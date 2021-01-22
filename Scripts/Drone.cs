using System;
using Guribo.UdonBetterAudio.Scripts;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;


namespace Guribo.FPVDrones.Scripts
{
    public class Drone : UdonSharpBehaviour
    {
        public Text debugText;

        #region DroneState

        [HideInInspector] public int noPilot = -1;

        private const int DroneStateTurningOff = 0;
        private const int DroneStateOff = 1;
        private const int DroneStateNotBooted = 2;
        private const int DroneStateBooting = 3;
        private const int DroneStateReady = 4;
        private const int DroneStatePiloted = 5;
        private const int DroneStateCrashed = 6;

        private float _motorRpm;
        private bool _initialized;
        private bool _grabbed;
        private int _previousState;
        private int _currentState;
        private int _pendingState;
        private bool _isOwner;
        private VRCPlayerApi _owner;
        private bool _isPilot;
        private float _bootUpCompletedTime;

        private bool IsInState(int targetState)
        {
            return _currentState == targetState;
        }

        private bool WasInState(int targetState)
        {
            return _previousState == targetState;
        }

        private int GetNextState(int currentState, int targetState)
        {
            if (targetState == DroneStateTurningOff)
            {
                return DroneStateTurningOff;
            }

            switch (currentState)
            {
                case DroneStateTurningOff:
                    if (targetState > DroneStateTurningOff)
                    {
                        return DroneStateOff;
                    }

                    return DroneStateTurningOff;
                case DroneStateOff:
                    if (targetState > DroneStateOff)
                    {
                        return DroneStateNotBooted;
                    }

                    return DroneStateOff;
                case DroneStateNotBooted:
                    if (targetState > DroneStateNotBooted)
                    {
                        return DroneStateBooting;
                    }
                    else if (targetState < DroneStateNotBooted)
                    {
                        return DroneStateOff;
                    }

                    return DroneStateNotBooted;
                case DroneStateBooting:
                    if (targetState > DroneStateBooting)
                    {
                        return DroneStateReady;
                    }
                    else if (targetState < DroneStateBooting)
                    {
                        return DroneStateNotBooted;
                    }

                    return DroneStateBooting;
                case DroneStateReady:
                    if (targetState > DroneStateReady)
                    {
                        return DroneStatePiloted;
                    }
                    else if (targetState < DroneStateReady)
                    {
                        return DroneStateNotBooted;
                    }

                    return DroneStateReady;
                case DroneStatePiloted:
                    if (targetState > DroneStatePiloted)
                    {
                        return DroneStateCrashed;
                    }
                    else if (targetState < DroneStatePiloted)
                    {
                        return DroneStateOff;
                    }

                    return DroneStatePiloted;
                case DroneStateCrashed:
                    if (targetState < DroneStateCrashed)
                    {
                        return DroneStatePiloted;
                    }

                    return DroneStateCrashed;
                default:
                    return DroneStateOff;
            }
        }

        #endregion

        #region Networking

        /// <summary>
        /// DroneStateTurningOff = 0;
        /// DroneStateOff = 1;
        /// DroneStateNotBooted = 2;
        /// DroneStateBooting = 3;
        /// DroneStateReady = 4;
        /// DroneStatePiloted = 5;
        /// DroneStateCrashed = 6;
        /// </summary>
        [UdonSynced] public int syncedTargetDroneState = DroneStateOff;

        [UdonSynced] public int syncedPilotId = -1;

        [UdonSynced(UdonSyncMode.Linear)] public float syncedMotorRpm;

        public override void OnPreSerialization()
        {
            TryRefreshOwnerState();
        }

        public override void OnDeserialization()
        {
            TryRefreshRemoteState();
        }


        public override void OnOwnershipTransferred()
        {
            OnDeserialization();
        }

        #endregion

        #region Input

        [Header("Input")] public bool customDroneInputChanged;
        public DroneInput customDroneInput;
        public DroneInput[] droneInputs;

        private float _throttle;
        private float _yaw;
        private float _pitch;
        private float _roll;

        private bool _startPilotingPressed;
        private bool _stopPilotingPressed;

        #endregion

        #region Audio

        [Header("Audio")] [SerializeField] private BetterAudioSource startupSound;
        [SerializeField] private BetterAudioSource motorSound;
        private AudioSource _motorSoundProxy;

        public float minRpm = 0.2f;

        #endregion

        #region Physics

        [Header("Physics")] [SerializeField] private Rigidbody droneRigidbody;
        [SerializeField] private Transform centerOfGravity;

        [Tooltip("Only works for drones that have no negative thrust, potentially better better performance")]
        public bool useSingleForce = false;

        public float angularDragActive = 20;
        public float angularDragNotActive = 1;

        [SerializeField] private float maxEngineThrust = 3.5f;

        #endregion

        [SerializeField] private Camera fpvCamera;
        [SerializeField] private Camera viewOverrideCamera;
        public Transform screen;

        #region Drone

        [Header("Drone")] [SerializeField] private Transform motorFrontLeft;
        [SerializeField] private Transform motorFrontRight;
        [SerializeField] private Transform motorRearLeft;
        [SerializeField] private Transform motorRearRight;

        #endregion


        public DroneUserController droneUserController;
        public VRCPickup vrcPickup;


        #region Caching

        #region localPlayer

        private VRCPlayerApi _localPlayer;
        private int _localPlayerId;
        private bool _isVr;

        private bool CacheLocalPlayer()
        {
            _localPlayer = Networking.LocalPlayer;
            if (_localPlayer == null)
            {
                _localPlayerId = -1;
                Debug.LogWarning("Invalid local player");
                return false;
            }

            _localPlayerId = _localPlayer.playerId;
            _isVr = _localPlayer.IsUserInVR();
            return true;
        }

        #endregion

        #region Drone

        private Vector3 _localMotorPositionFl;
        private Vector3 _localMotorPositionFr;
        private Vector3 _localMotorPositionBL;
        private Vector3 _localMotorPositionBR;

        private bool CacheLocalMotorPositions()
        {
            if (!motorFrontLeft
                || !motorFrontRight
                || !motorRearLeft
                || !motorRearRight)
            {
                Debug.LogWarning("Invalid motors");
                return false;
            }

            _localMotorPositionFl = motorFrontLeft.localPosition;
            _localMotorPositionFr = motorFrontRight.localPosition;
            _localMotorPositionBL = motorRearLeft.localPosition;
            _localMotorPositionBR = motorRearRight.localPosition;
            return true;
        }

        #endregion


        #region localInputCopy

        private string _enter;
        private string _exit;
        private string _reset;
        private string _toggleFpv;

        private string _throttleMin;
        private string _throttleMax;
        private string _pitchMin;
        private string _pitchMax;
        private string _rollMin;
        private string _rollMax;
        private string _yawMin;
        private string _yawMax;

        private KeyCode _enterFallback;
        private KeyCode _exitFallback;
        private KeyCode _resetFallback;
        private KeyCode _toggleFpvFallback;
        private KeyCode _throttleUpFallback;
        private KeyCode _throttleDownFallback;
        private KeyCode _pitchUpFallback;
        private KeyCode _pitchDownFallback;
        private KeyCode _rollLeftFallback;
        private KeyCode _rollRightFallback;
        private KeyCode _yawLeftFallback;
        private KeyCode _yawRightFallback;

        private bool _yawMinInverted;
        private bool _yawMaxInverted;

        private bool _pitchMinInverted;
        private bool _pitchMaxInverted;

        private bool _rollMinInverted;
        private bool _rollMaxInverted;

        private bool _throttleMinInverted;
        private bool _throttleMaxInverted;

        private float _yawExpo;
        private float _pitchExpo;
        private float _rollExpo;
        private float _throttleExpo;

        private float _yawRate;
        private float _pitchRate;
        private float _rollRate;
        private float _throttleRate;

        private Vector2 _yawInputCalibration;
        private Vector2 _pitchInputCalibration;
        private Vector2 _rollInputCalibration;
        private Vector2 _throttleInputCalibration;

        #endregion

        #endregion

        #region MonoBehaviour Methods

        public void Start()
        {
            if (!(CacheLocalPlayer()
                  && CacheLocalMotorPositions()
                  && InitPhysics()
                  && InitCameras()
                  && InitAudio()))
            {
                Debug.LogError($"Drone {name} initialization failed");
                return;
            }

            UpdateSinglePlayerState();
            _initialized = true;

            Debug.Log($"Drone {name} initialized successfully", this);
        }

        public void FixedUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            PilotControlDrone();
        }

        public void Update()
        {
            if (!_initialized)
            {
                return;
            }

            var time = Time.time;
            var deltaTime = Time.deltaTime;

            UpdateSinglePlayerState();
            UpdateGrabState();
            UpdateGrabbingForceFeedback(deltaTime);
            UpdateDrone(time);
            // Debug.Log($"Pending state {_pendingState}");
            UpdateLocalState(GetNextState(_currentState, _pendingState));

            if (debugText)
            {
                var debugTextText = GetDebugText();
                debugText.text = debugTextText;
                // Debug.Log(debugTextText);
            }
        }

        #endregion

        private void UpdateGrabbingForceFeedback(float deltaTime)
        {
            if (IsOwnerGrabbing())
            {
                _localPlayer.PlayHapticEventInHand(vrcPickup.currentHand,
                    deltaTime,
                    _motorRpm,
                    _motorRpm * 10f);
            }
        }

        private void PilotControlDrone()
        {
            if (!(_isOwner
                  && _isPilot
                  && IsInState(DroneStatePiloted)))
            {
                return;
            }

            _yaw = GetYaw();
            _pitch = GetPitch();
            _roll = GetRoll();
            _throttle = GetThrottle();

            _motorRpm = _throttle;


            if (useSingleForce)
            {
                var frontLeft = (Mathf.Clamp01(_pitch + _roll + _throttle));
                var frontRight = (Mathf.Clamp01(_pitch - _roll + _throttle));
                var backLeft = (Mathf.Clamp01(_roll - _pitch + _throttle));
                var backRight = (Mathf.Clamp01(_throttle - _pitch - _roll));
                var forceSum = (frontLeft + frontRight + backLeft + backRight);

                var transformUp = transform.up;
                if (forceSum > 0.001f)
                {
                    var localForcePosition = (frontLeft / forceSum * _localMotorPositionFl)
                                             + (frontRight / forceSum * _localMotorPositionFr)
                                             + (backLeft / forceSum * _localMotorPositionBL)
                                             + (backRight / forceSum * _localMotorPositionBR);
                    var force = forceSum * maxEngineThrust * transformUp;
                    var position = transform.TransformPoint(localForcePosition);
                    droneRigidbody.AddForceAtPosition(force, position, ForceMode.Force);
                }

                droneRigidbody.AddTorque(transformUp * _yaw);
            }
            else
            {
                droneRigidbody.AddForceAtPosition(
                    motorFrontLeft.up * (Mathf.Clamp01(_pitch + _roll + _throttle) * maxEngineThrust),
                    motorFrontLeft.position);
                droneRigidbody.AddForceAtPosition(
                    motorFrontRight.up * (Mathf.Clamp01(_pitch - _roll + _throttle) * maxEngineThrust),
                    motorFrontRight.position);
                droneRigidbody.AddForceAtPosition(
                    motorRearLeft.up * (Mathf.Clamp01(-_pitch + _roll + _throttle) * maxEngineThrust),
                    motorRearLeft.position);
                droneRigidbody.AddForceAtPosition(
                    motorRearRight.up * (Mathf.Clamp01(-_pitch - _roll + _throttle) * maxEngineThrust),
                    motorRearRight.position);

                droneRigidbody.AddTorque(transform.up * _yaw);
            }
        }

        private bool IsOwnerGrabbing()
        {
            Assert(vrcPickup, "vrcPickup is valid");
            return _isOwner
                   && _grabbed
                   && vrcPickup.currentHand != VRC_Pickup.PickupHand.None;
        }

        private void UpdateSinglePlayerState()
        {
            if (VRCPlayerApi.GetPlayerCount() == 1)
            {
                // update manually because the network sync events are not firing
                var manualPlayerUpdatePerformed = TryRefreshOwnerState() || TryRefreshRemoteState();
                Debug.Assert(manualPlayerUpdatePerformed, "Manual update performed");
            }
        }

        private void UpdateDrone(float time)
        {
            switch (_currentState)
            {
                case DroneStateTurningOff:
                    UpdateDroneTurningOff();
                    break;
                case DroneStateOff:
                    UpdateDroneOff(time);
                    break;
                case DroneStateNotBooted:
                    UpdateDroneNotBooted(time);
                    break;
                case DroneStateBooting:
                    UpdateDroneBooting(time);
                    break;
                case DroneStateReady:
                    UpdateDroneReady(time);
                    break;
                case DroneStatePiloted:
                    UpdateDronePiloted(time);
                    break;
                case DroneStateCrashed:
                    UpdateDroneCrashed(time);
                    break;
                default:
                {
                    Debug.LogError("Unknown drone state");
                }
                    break;
            }
        }

        private void UpdateDroneCrashed(float time)
        {
            droneRigidbody.angularDrag = angularDragNotActive;

            _motorRpm = 0;
            _motorSoundProxy.volume = _motorRpm;
            _motorSoundProxy.pitch = _motorRpm;

            if (_isOwner)
            {
                if (_isPilot)
                {
                    // owner and pilot
                    if (!UpdateOwnerPilotRespawnAndOffAndFpv())
                    {
                        return;
                    }
                }
                else
                {
                    // owner and not pilot
                }
            }
            else
            {
                if (_isPilot)
                {
                    // pilot but not owner
                    if (!UpdateNonOwnerPilotRespawnAndOffAndFpv())
                    {
                        return;
                    }
                }
                else
                {
                    // not owner and not pilot
                }
            }
        }

        private void UpdateDronePiloted(float time)
        {
            if (_isOwner)
            {
                if (_isPilot)
                {
                    // owner and pilot
                    if (!UpdateOwnerPilotRespawnAndOffAndFpv())
                    {
                        return;
                    }
                }
                else
                {
                    // owner and not pilot
                }
            }
            else
            {
                if (_isPilot)
                {
                    // pilot but not owner
                    if (!UpdateNonOwnerPilotRespawnAndOffAndFpv())
                    {
                        return;
                    }
                }
                else
                {
                    // not owner and not pilot
                }
            }

            _motorSoundProxy.volume = _motorRpm;
            _motorSoundProxy.pitch = _motorRpm;
        }

        private void UpdateDroneReady(float time)
        {
            _motorRpm = minRpm;
            _motorSoundProxy.volume = _motorRpm;
            _motorSoundProxy.pitch = _motorRpm;
            motorSound.Play(true);
            droneRigidbody.angularDrag = angularDragActive;

            _pendingState = DroneStatePiloted;

            if (_isOwner)
            {
                if (_isPilot)
                {
                    // owner and pilot
                    SpawnScreenForPlayer(_localPlayer, true);
                    fpvCamera.gameObject.SetActive(true);
                    viewOverrideCamera.gameObject.SetActive(false);
                    _localPlayer.Immobilize(true);

                    if (!UpdateOwnerPilotRespawnAndOffAndFpv())
                    {
                        return;
                    }
                }
                else
                {
                    // owner and not pilot
                }
            }
            else
            {
                if (_isPilot)
                {
                    // pilot but not owner
                    SpawnScreenForPlayer(_localPlayer, true);
                    fpvCamera.gameObject.SetActive(true);
                    viewOverrideCamera.gameObject.SetActive(false);
                    _localPlayer.Immobilize(true);

                    if (!UpdateNonOwnerPilotRespawnAndOffAndFpv())
                    {
                        return;
                    }
                }
                else
                {
                    // not owner and not pilot
                }
            }
        }

        private bool UpdateOwnerPilotRespawnAndOffAndFpv()
        {
            if (!UpdateOwnerPilotRespawnAndOff())
            {
                return false;
            }

            var toggleFpvPressed = Input.GetKeyDown(_toggleFpvFallback) ||
                                   (!string.IsNullOrEmpty(_toggleFpv)
                                    && Input.GetButtonDown(_toggleFpv));
            if (toggleFpvPressed)
            {
                SwitchFpvCamera();
            }

            return true;
        }

        private bool UpdateNonOwnerPilotRespawnAndOffAndFpv()
        {
            if (!UpdateNonOwnerPilotRespawnAndOff())
            {
                return false;
            }

            var toggleFpvPressed = Input.GetKeyDown(_toggleFpvFallback) ||
                                   (!string.IsNullOrEmpty(_toggleFpv) && Input.GetButtonDown(_toggleFpv));
            if (toggleFpvPressed)
            {
                SwitchFpvCamera();
            }

            return true;
        }

        private void UpdateDroneBooting(float time)
        {
            if (time > _bootUpCompletedTime)
            {
                _pendingState = DroneStateReady;
            }


            if (_isOwner)
            {
                if (_isPilot)
                {
                    // owner and pilot
                    if (!UpdateOwnerPilotRespawnAndOff())
                    {
                        return;
                    }
                }
                else
                {
                    // owner and not pilot
                }
            }
            else
            {
                if (_isPilot)
                {
                    // pilot but not owner
                    if (!UpdateNonOwnerPilotRespawnAndOff())
                    {
                        return;
                    }
                }
                else
                {
                    // not owner and not pilot
                }
            }
        }

        private void UpdateDroneNotBooted(float time)
        {
            StartDroneBoot(time);
            _pendingState = DroneStateBooting;

            if (_isOwner)
            {
                if (_isPilot)
                {
                    // owner and pilot
                    if (!UpdateOwnerPilotRespawnAndOff())
                    {
                        return;
                    }
                }
                else
                {
                    // owner and not pilot
                }
            }
            else
            {
                if (_isPilot)
                {
                    // pilot but not owner
                    if (!UpdateNonOwnerPilotRespawnAndOff())
                    {
                        return;
                    }
                }
                else
                {
                    // not owner and not pilot
                }
            }
        }

        private bool UpdateNonOwnerPilotRespawnAndOff()
        {
            if (!UpdatePilotBasicInput())
            {
                return false;
            }

            var respawnDronePressed = Input.GetKeyDown(_resetFallback)
                                      || !string.IsNullOrEmpty(_reset) && Input.GetButtonDown(_reset);
            if (respawnDronePressed)
            {
                SpawnScreenForPlayer(_localPlayer, false);
                if (vrcPickup.IsHeld
                    && vrcPickup.currentHand != VRC_Pickup.PickupHand.None
                    && vrcPickup.currentPlayer != null)
                {
                    if (vrcPickup.DisallowTheft)
                    {
                        return false;
                    }

                    vrcPickup.Drop(_localPlayer);
                }

                if (TrySpawnDroneInPlayerHand())
                {
                    return false;
                }
            }

            return true;
        }

        private bool UpdateOwnerPilotRespawnAndOff()
        {
            if (!UpdatePilotBasicInput())
            {
                return false;
            }

            var respawnDronePressed = Input.GetKeyDown(_resetFallback)
                                      || !string.IsNullOrEmpty(_reset) && Input.GetButtonDown(_reset);
            if (respawnDronePressed && TrySpawnDroneInPlayerHand())
            {
                SpawnScreenForPlayer(_localPlayer, false);
                return false;
            }

            return true;
        }

        private bool UpdatePilotBasicInput()
        {
            if (!UpdateInputMethod())
            {
                return false;
            }

            _stopPilotingPressed = Input.GetKeyDown(_exitFallback)
                                   || !string.IsNullOrEmpty(_exit) && Input.GetButtonDown(_exit);

            if (_stopPilotingPressed)
            {
                _pendingState = DroneStateTurningOff;
                return false;
            }

            return true;
        }

        /// <summary>
        /// respawns the drone in the player's right hand (if it is empty)
        /// </summary>
        /// <returns>true if respawning succeeded</returns>
        private bool TrySpawnDroneInPlayerHand()
        {
            var rightPlayerHandIsEmpty = !_localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            if (rightPlayerHandIsEmpty)
            {
                var localPlayerRotation = _localPlayer.GetRotation();
                transform.SetPositionAndRotation(_localPlayer.GetPosition() +
                                                 localPlayerRotation * (Vector3.forward + Vector3.up),
                    localPlayerRotation);
                droneRigidbody.velocity = Vector3.zero;
                droneRigidbody.angularVelocity = Vector3.zero;
                // FIXME once we are allowed in UDON
                // _localPlayer.SetPickupInHand(vrcPickup, VRC_Pickup.PickupHand.Right);
                return true;
            }

            return false;
        }

        /// <summary>
        /// off means not controlled, screens off, pilot can run around etc.
        /// </summary>
        /// <param name="time"></param>
        private void UpdateDroneOff(float time)
        {
            if (_isOwner)
            {
                if (_isPilot)
                {
                    // owner and pilot
                    UpdatePilotDroneOffState();
                }
                else
                {
                    // owner and not pilot
                }
            }
            else
            {
                if (_isPilot)
                {
                    // pilot but not owner
                    UpdatePilotDroneOffState();
                }
                else
                {
                    // not owner and not pilot
                }
            }
        }

        private void UpdatePilotDroneOffState()
        {
            Assert(_isPilot, "_isPilot in UpdatePilotDroneOffState");
            if (!UpdateInputMethod())
            {
                return;
            }

            _startPilotingPressed = Input.GetKeyDown(_enterFallback)
                                    || !string.IsNullOrEmpty(_enter) && Input.GetButtonDown(_enter);

            if (_startPilotingPressed)
            {
                _bootUpCompletedTime = 0f;
                _pendingState = DroneStateNotBooted;
            }
        }

        private void UpdateGrabState()
        {
            Debug.Assert(vrcPickup, "vrcPickup is valid");
            if ((int) Time.time % 2 == 0)
            {
                _grabbed = vrcPickup && (vrcPickup.IsHeld && vrcPickup.currentPlayer != null);
            }
        }

        public string GetDebugText()
        {
            return
                $"Drone {name} state: {_currentState}; is owner: {_isOwner}; is pilot: {_isPilot} ({syncedPilotId}); is VR {_isVr}";
        }


        private bool InitAudio()
        {
            if (!motorSound)
            {
                Debug.LogWarning("Invalid motorSound");
                return false;
            }

            _motorSoundProxy = motorSound.GetAudioSourceProxy();
            return true;
        }

        private bool InitCameras()
        {
            if (!viewOverrideCamera)
            {
                Debug.LogWarning("Invalid viewOverrideCamera");
                return false;
            }

            viewOverrideCamera.stereoTargetEye = _isVr ? StereoTargetEyeMask.Both : StereoTargetEyeMask.None;
            return true;
        }

        private bool InitPhysics()
        {
            if (!(droneRigidbody
                  && centerOfGravity))
            {
                Debug.LogWarning("Invalid droneRigidBody/centerOfGravity");
                return false;
            }

            droneRigidbody.centerOfMass = centerOfGravity.localPosition;
            return true;
        }

        private float GetAngularDrag(int syncedDroneState1)
        {
            if (IsInState(DroneStatePiloted)
                || IsInState(DroneStateReady))
            {
                return angularDragActive;
            }

            return angularDragNotActive;
        }

        private void UpdateDroneTurningOff()
        {
            _motorRpm = 0;
            startupSound.Stop();
            motorSound.Stop();
            _pendingState = DroneStateOff;
            droneRigidbody.angularDrag = angularDragNotActive;

            if (_isOwner)
            {
                if (_isPilot)
                {
                    // owner and pilot
                    _localPlayer.Immobilize(false);
                    fpvCamera.gameObject.SetActive(false);
                    viewOverrideCamera.gameObject.SetActive(false);
                    screen.gameObject.SetActive(false);
                }
                else
                {
                    // owner and not pilot
                }
            }
            else
            {
                if (_isPilot)
                {
                    // pilot but not owner
                    _localPlayer.Immobilize(false);
                    fpvCamera.gameObject.SetActive(false);
                    viewOverrideCamera.gameObject.SetActive(false);
                    screen.gameObject.SetActive(false);
                }
                else
                {
                    // not owner and not pilot
                }
            }
        }

        private void UpdateLocalState(int newState)
        {
            var initialState = _currentState;
            _currentState = newState;
            // store the last different state
            _previousState = initialState != _currentState ? initialState : _previousState;
        }

        private bool UpdateInputMethod()
        {
            var vrcInputMethod = (int) InputManager.GetLastUsedInputMethod();
            if (droneInputs == null
                || vrcInputMethod >= droneInputs.Length)
            {
                Debug.LogError("No input");
                return false;
            }

            var inputMethodChanged = vrcInputMethod != _currentInputMethod;
            if (inputMethodChanged
                || customDroneInputChanged)
            {
                _previousInputMethod = _currentInputMethod;
                _currentInputMethod = vrcInputMethod;
                _customInputInUse |= customDroneInputChanged;
                customDroneInputChanged = false;
                var droneInput = _customInputInUse ? customDroneInput : droneInputs[vrcInputMethod];
                if (!droneInput)
                {
                    Debug.LogError("Retrieved invalid input");
                    return false;
                }

                CreateLocalCopyOfInputs(droneInput);
            }

            return true;
        }

        private void StartDroneBoot(float time)
        {
            if (!startupSound.IsPlaying())
            {
                _bootUpCompletedTime = time + startupSound.GetAudioClip().length;
                startupSound.Play(false);
            }
        }

        private float Remap(float iMin, float iMax, float oMin, float oMax, float value)
        {
            var t = InverseLerp(iMin, iMax, value);
            return Lerp(oMin, oMax, t);
        }

        private float InverseLerp(float a, float b, float value) => (value - a) / (b - a);
        private float Lerp(float a, float b, float t) => (1f - t) * a + t * b;


        private void SwitchFpvCamera()
        {
            Debug.Log($"{Time.frameCount} : toggle FPV triggered");

            var fpvEnabled = fpvCamera.gameObject.activeSelf;
            fpvCamera.gameObject.SetActive(!fpvEnabled);
            screen.gameObject.SetActive(!fpvEnabled);
            viewOverrideCamera.gameObject.SetActive(fpvEnabled);
        }

        public override void OnSpawn()
        {
            if (_isPilot)
            {
                UpdateDroneTurningOff();
            }
        }

        private bool TryRefreshOwnerState()
        {
            _isOwner = Networking.IsOwner(gameObject);
            _isPilot = syncedPilotId == _localPlayerId;

            if (!_isOwner)
            {
                return false;
            }

            _owner = _localPlayer;
            syncedTargetDroneState = _currentState;
            syncedMotorRpm = _motorRpm;
            return true;
        }


        private bool TryRefreshRemoteState()
        {
            _isOwner = Networking.IsOwner(gameObject);
            _isPilot = syncedPilotId == _localPlayerId;

            if (_isOwner)
            {
                return false;
            }

            _owner = Networking.GetOwner(gameObject);
            _motorRpm = syncedMotorRpm;
            _pendingState = syncedTargetDroneState;
            return true;
        }

        private void SpawnScreenForPlayer(VRCPlayerApi player, bool enableScreen)
        {
            if (!screen) return;
            if (enableScreen)
            {
                screen.gameObject.SetActive(true);
            }

            var playerRotation = player.GetRotation();
            screen.SetPositionAndRotation(
                player.GetPosition() + playerRotation * Vector3.forward, playerRotation);
        }

        #region Input

        private bool _customInputInUse;
        private int _previousInputMethod;
        private int _currentInputMethod;


        /// <summary>
        /// input should be between -1 to 1
        /// </summary>
        /// <param name="input"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        private float ApplyExpo(float input, float factor)
        {
            return ((1f - factor) * (input * input * input)) + (factor * input);
        }

        public float GetYaw()
        {
            var raw = GetInputAxis(_yawMin, _yawMax,
                _yawLeftFallback,
                _yawRightFallback,
                _yawMinInverted,
                _yawMaxInverted,
                _yawInputCalibration);

            raw = ApplyExpo(raw, _yawExpo);
            raw *= _yawRate;
            return raw;
        }

        public float GetPitch()
        {
            var raw = GetInputAxis(_pitchMin, _pitchMax,
                _pitchDownFallback,
                _pitchUpFallback,
                _pitchMinInverted,
                _pitchMaxInverted,
                _pitchInputCalibration);

            raw = ApplyExpo(raw, _pitchExpo);
            raw *= _pitchRate;
            return raw;
        }

        public float GetRoll()
        {
            var raw = GetInputAxis(_rollMin, _rollMax,
                _rollLeftFallback,
                _rollRightFallback,
                _rollMinInverted,
                _rollMaxInverted,
                _rollInputCalibration);

            raw = ApplyExpo(raw, _rollExpo);
            raw *= _rollRate;
            return raw;
        }

        public float GetThrottle()
        {
            var raw = GetInputAxis(_throttleMin, _throttleMax,
                _throttleDownFallback,
                _throttleUpFallback,
                _throttleMinInverted,
                _throttleMaxInverted,
                _throttleInputCalibration);

            raw = ApplyExpo(raw, _throttleExpo);
            raw *= _throttleRate;
            return raw;
        }

        private float GetAxisInput(string minAxisName,
            string maxAxisName,
            bool minInverted,
            bool maxInverted,
            Vector2 axisCalibration)
        {
            var axisValue = 0f;
            if (!string.IsNullOrEmpty(minAxisName))
            {
                var min = (minInverted ? -1f : 1f) * Input.GetAxisRaw(minAxisName);
                var invertedInputIsNegative = min < 0f && minInverted;
                var normalInputIsPositive = min > 0f && !minInverted;

                if (!(invertedInputIsNegative || normalInputIsPositive))
                {
                    axisValue += (min / (axisCalibration.x > 0f ? axisCalibration.x : 1f));
                }
            }

            if (string.IsNullOrEmpty(maxAxisName))
            {
                return axisValue;
            }

            var max = (maxInverted ? -1f : 1f) * Input.GetAxisRaw(maxAxisName);
            var invertedInputIsPositive = max > 0f && maxInverted;

            var normalInputIsNegative = max < 0f && !maxInverted;
            if (!(invertedInputIsPositive || normalInputIsNegative))
            {
                axisValue += (max / (axisCalibration.y > 0f ? axisCalibration.y : 1f));
            }

            return axisValue;
        }

        private float GetInputAxis(string minAxisName, string maxAxisName, KeyCode fallbackMin, KeyCode fallbackMax,
            bool minInverted, bool maxInverted, Vector2 axisCalibration)
        {
            var min = Input.GetKey(fallbackMin) ? -1f : 0f;
            var max = Input.GetKey(fallbackMax) ? 1f : 0f;

            var combinedInput = min + max + GetAxisInput(minAxisName, maxAxisName, minInverted,
                maxInverted, axisCalibration);
            return Mathf.Clamp(combinedInput, -1, 1);
        }


        private void CreateLocalCopyOfInputs(DroneInput droneInput)
        {
            if (!droneInput) return;
            _enter = droneInput.enter;
            _exit = droneInput.exit;
            _reset = droneInput.reset;
            _toggleFpv = droneInput.toggleFpv;
            _throttleMin = droneInput.throttleMin;
            _throttleMax = droneInput.throttleMax;
            _pitchMin = droneInput.pitchMin;
            _pitchMax = droneInput.pitchMax;
            _rollMin = droneInput.rollMin;
            _rollMax = droneInput.rollMax;
            _yawMin = droneInput.yawMin;
            _yawMax = droneInput.yawMax;
            _enterFallback = droneInput.enterFallback;
            _exitFallback = droneInput.exitFallback;
            _resetFallback = droneInput.resetFallback;
            _toggleFpvFallback = droneInput.toggleFpvFallback;
            _throttleUpFallback = droneInput.throttleUpFallback;
            _throttleDownFallback = droneInput.throttleDownFallback;
            _pitchUpFallback = droneInput.pitchUpFallback;
            _pitchDownFallback = droneInput.pitchDownFallback;
            _rollLeftFallback = droneInput.rollLeftFallback;
            _rollRightFallback = droneInput.rollRightFallback;
            _yawLeftFallback = droneInput.yawLeftFallback;
            _yawRightFallback = droneInput.yawRightFallback;
            _yawMinInverted = droneInput.yawMinInverted;
            _yawMaxInverted = droneInput.yawMaxInverted;
            _pitchMinInverted = droneInput.pitchMinInverted;
            _pitchMaxInverted = droneInput.pitchMaxInverted;
            _rollMinInverted = droneInput.rollMinInverted;
            _rollMaxInverted = droneInput.rollMaxInverted;
            _throttleMinInverted = droneInput.throttleMinInverted;
            _throttleMaxInverted = droneInput.throttleMaxInverted;
            _yawExpo = droneInput.yawExpo;
            _pitchExpo = droneInput.pitchExpo;
            _rollExpo = droneInput.rollExpo;
            _throttleExpo = droneInput.throttleExpo;
            _yawRate = droneInput.yawRate;
            _pitchRate = droneInput.pitchRate;
            _rollRate = droneInput.rollRate;
            _throttleRate = droneInput.throttleRate;
            _yawInputCalibration = droneInput.yawInputCalibration;
            _pitchInputCalibration = droneInput.pitchInputCalibration;
            _rollInputCalibration = droneInput.rollInputCalibration;
            _throttleInputCalibration = droneInput.throttleInputCalibration;
        }

        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"Assertion failed : '{GetType()} : {message}'", this);
            }
        }

        #endregion
    }
}