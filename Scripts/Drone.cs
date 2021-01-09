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

        private const int DroneStateOff = 0;
        private const int DroneStateNotBooted = 1;
        private const int DroneStateBooting = 2;
        private const int DroneStateReady = 3;
        private const int DroneStatePiloted = 4;
        private const int DroneStateCrashed = 5;
        private const int DroneStateGrabbed = 6;

        private bool _initialized;
        private int _currentState;
        private int _previousState;
        private bool _isOwner;
        private VRCPlayerApi _owner;
        private bool _isPilot;
        private float _bootUpCompletedTime;

        private bool IsInState(int targetState)
        {
            return syncedDroneState == targetState;
        }

        private bool WasInState(int targetState)
        {
            return _previousState == targetState;
        }

        #endregion

        #region Networking

        /// <summary>
        /// DroneStateOff = 0;
        /// DroneStateNotBooted = 1;
        /// DroneStateBooting = 2;
        /// DroneStateReady = 3;
        /// DroneStatePiloted = 4;
        /// DroneStateCrashed = 5;
        /// DroneStateGrabbed = 6;
        /// </summary>
        [UdonSynced] public int syncedDroneState = DroneStateOff;

        [UdonSynced] public int syncedPilotId = -1;

        [UdonSynced(UdonSyncMode.Linear)] public float syncedMotorRpm;

        public override void OnPreSerialization()
        {
            _isOwner = Networking.IsOwner(gameObject);
            _isPilot = syncedPilotId == _localPlayerId;

            if (_isOwner)
            {
                _owner = _localPlayer;
                syncedDroneState = _currentState;
            }
        }

        public override void OnDeserialization()
        {
            _isOwner = Networking.IsOwner(gameObject);
            _isPilot = syncedPilotId == _localPlayerId;

            if (!_isOwner)
            {
                _owner = Networking.GetOwner(gameObject);
                UpdateLocalState(syncedDroneState);
            }
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
        private bool _toggleFpvPressed;

        #endregion

        #region Audio

        [Header("Audio")] [SerializeField] private BetterAudioSource startupSound;
        [SerializeField] private BetterAudioSource motorSound;
        private AudioSource _motorSoundProxy;

        public float minRpm = 0.2f;

        #endregion

        #region Physics

        [Header("Physics")] [SerializeField] private Rigidbody droneRigidbody;

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
            if (!CacheLocalPlayer()
                || !CacheLocalMotorPositions()
                || !InitPhysics()
                || !InitCameras()
                || !InitAudio())
            {
                return;
            }

            _isOwner = Networking.IsOwner(gameObject);
            _isPilot = syncedPilotId == _localPlayerId;

            _initialized = true;
            Debug.Log($"Drone {name} initialized successfully");
        }

        public void FixedUpdate()
        {
            if (!_initialized) return;

            if (_isOwner && IsInState(DroneStateGrabbed))
            {
                _localPlayer.PlayHapticEventInHand(vrcPickup.currentHand,
                    Time.fixedDeltaTime,
                    syncedMotorRpm * 10f,
                    syncedMotorRpm * 10f);
            }

            if (_isOwner && _isPilot && IsInState(DroneStatePiloted))
            {
                _yaw = GetYaw();
                _pitch = GetPitch();
                _roll = GetRoll();
                _throttle = GetThrottle();

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
                    droneRigidbody.AddForceAtPosition(force, position);
                }

                droneRigidbody.AddTorque(transformUp * _yaw);
            }
        }


        public void Update()
        {
            UpdateInput();
            PlayMotorSound(true);
        }

        #endregion

        private bool InitAudio()
        {
            if (!motorSound)
            {
                return false;
            }

            _motorSoundProxy = motorSound.GetAudioSourceProxy();
            return true;
        }

        private bool InitCameras()
        {
            if (!viewOverrideCamera)
            {
                return false;
            }

            viewOverrideCamera.stereoTargetEye = _isVr ? StereoTargetEyeMask.Both : StereoTargetEyeMask.None;
            return true;
        }

        private bool InitPhysics()
        {
            if (!droneRigidbody)
            {
                return false;
            }

            droneRigidbody.centerOfMass = Vector3.zero;
            droneRigidbody.angularDrag = GetAngularDrag(syncedDroneState);
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

        public void UpdateInput()
        {
            if (!_initialized)
            {
                return;
            }

            // update the synced state
            if (vrcPickup && (vrcPickup.IsHeld || vrcPickup.currentPlayer != null))
            {
                UpdateLocalState(DroneStateGrabbed);
            }

            // update manually if network sync events are not firing
            if (VRCPlayerApi.GetPlayerCount() == 1)
            {
                _isPilot = _localPlayerId == syncedPilotId;
                _isOwner = Networking.IsOwner(gameObject);
            }

            var time = Time.time;

            if (debugText)
            {
                var debugTextText =
                    $"{transform.parent.gameObject.name}: current state: {_currentState} previous state {_previousState} pilot ID {syncedPilotId} isPilot {_isPilot} is Owner {_isOwner} is VR {_isVr}";
                debugText.text = debugTextText;
                Debug.Log(debugTextText);
            }


            switch (_currentState)
            {
                case DroneStateOff:
                {
                    // switch (_previousState)
                    // {
                    //     case DroneStateOff:
                    //         break;
                    //     case DroneStateNotBooted:
                    //         break;
                    //     case DroneStateBooting:
                    //         break;
                    //     case DroneStateReady:
                    //         break;
                    //     case DroneStatePiloted:
                    //         break;
                    //     case DroneStateCrashed:
                    //         break;
                    //     case DroneStateGrabbed:
                    //         break;
                    //     default:
                    //         break;
                    // }

                    syncedMotorRpm = 0f;

                    if (!_isPilot)
                    {
                        return;
                    }

                    if (!UpdateInputMethod())
                    {
                        return;
                    }

                    if (!_isOwner)
                    {
                        return;
                    }

                    _startPilotingPressed = Input.GetKeyDown(_enterFallback) ||
                                            (!string.IsNullOrEmpty(_enter)
                                             && Input.GetButtonDown(_enter));

                    if (_startPilotingPressed)
                    {
                        UpdateLocalState(DroneStateNotBooted);
                        _bootUpCompletedTime = 0f;
                        _localPlayer.Immobilize(true);
                        SpawnDroneForPlayer(this, _localPlayer);
                        SpawnScreenForPlayer(_localPlayer);
                        screen.gameObject.SetActive(true);
                    }
                }
                    break;
                case DroneStateNotBooted:
                {
                    // switch (_previousState)
                    // {
                    //     case DroneStateOff:
                    //         break;
                    //     case DroneStateNotBooted:
                    //         break;
                    //     case DroneStateBooting:
                    //         break;
                    //     case DroneStateReady:
                    //         break;
                    //     case DroneStatePiloted:
                    //         break;
                    //     case DroneStateCrashed:
                    //         break;
                    //     case DroneStateGrabbed:
                    //         break;
                    //     default:
                    //         break;
                    // }

                    StartDroneBoot(time);
                    syncedMotorRpm = 0f;

                    if (!_isPilot)
                    {
                        return;
                    }

                    if (!UpdateInputMethod())
                    {
                        return;
                    }

                    if (_isOwner)
                    {
                        RespawnDrone();

                        _stopPilotingPressed = Input.GetKeyDown(_exitFallback) ||
                                               (!string.IsNullOrEmpty(_exit)
                                                && Input.GetButtonDown(_exit));

                        if (_stopPilotingPressed)
                        {
                            StopPiloting();
                        }
                    }
                }
                    break;
                case DroneStateBooting:
                {
                    switch (_previousState)
                    {
                        case DroneStateOff:
                            StartDroneBoot(time);
                            break;
                        case DroneStateNotBooted:
                            FinishDroneBoot(time);
                            break;
                        case DroneStateBooting:
                            StartDroneBoot(time);
                            break;
                        case DroneStateReady:
                            StartDroneBoot(time);
                            break;
                        case DroneStatePiloted:
                            StartDroneBoot(time);
                            break;
                        case DroneStateCrashed:
                            StartDroneBoot(time);
                            break;
                        case DroneStateGrabbed:
                            StartDroneBoot(time);
                            break;
                        default:
                            StartDroneBoot(time);
                            break;
                    }

                    syncedMotorRpm = 0f;
                    if (!_isPilot)
                    {
                        return;
                    }

                    if (!UpdateInputMethod())
                    {
                        return;
                    }

                    if (_isOwner)
                    {
                        RespawnDrone();

                        _stopPilotingPressed = Input.GetKeyDown(_exitFallback) ||
                                               (!string.IsNullOrEmpty(_exit)
                                                && Input.GetButtonDown(_exit));

                        if (_stopPilotingPressed)
                        {
                            StopPiloting();
                        }
                    }
                }
                    break;
                case DroneStateReady:
                {
                    syncedMotorRpm = minRpm;

                    // switch (_previousState)
                    // {
                    //     case DroneStateOff:
                    //         break;
                    //     case DroneStateNotBooted:
                    //         break;
                    //     case DroneStateBooting:
                    //         break;
                    //     case DroneStateReady:
                    //         break;
                    //     case DroneStatePiloted:
                    //         break;
                    //     case DroneStateCrashed:
                    //         break;
                    //     case DroneStateGrabbed:
                    //         break;
                    //     default:
                    //         break;
                    // }

                    if (!_isPilot)
                    {
                        return;
                    }

                    if (!UpdateInputMethod())
                    {
                        return;
                    }

                    fpvCamera.gameObject.SetActive(true);
                    fpvCamera.enabled = true;
                    viewOverrideCamera.gameObject.SetActive(false);
                    viewOverrideCamera.enabled = false;

                    UpdateLocalState(DroneStatePiloted);

                    _toggleFpvPressed = Input.GetKeyDown(_toggleFpvFallback) ||
                                        (!string.IsNullOrEmpty(_toggleFpv)
                                         && Input.GetButtonDown(_toggleFpv));
                    if (_toggleFpvPressed)
                    {
                        UpdateFPV();
                    }

                    if (_isOwner)
                    {
                        RespawnDrone();

                        _stopPilotingPressed = Input.GetKeyDown(_exitFallback) ||
                                               (!string.IsNullOrEmpty(_exit)
                                                && Input.GetButtonDown(_exit));
                        if (_stopPilotingPressed)
                        {
                            StopPiloting();
                        }
                    }
                }
                    break;
                case DroneStatePiloted:
                {
                    // switch (_previousState)
                    // {
                    //     case DroneStateOff:
                    //         break;
                    //     case DroneStateNotBooted:
                    //         break;
                    //     case DroneStateBooting:
                    //         break;
                    //     case DroneStateReady:
                    //         break;
                    //     case DroneStatePiloted:
                    //         break;
                    //     case DroneStateCrashed:
                    //         break;
                    //     case DroneStateGrabbed:
                    //         break;
                    //     default:
                    //         break;
                    // }

                    if (!_isPilot)
                    {
                        return;
                    }

                    if (!UpdateInputMethod())
                    {
                        return;
                    }

                    _toggleFpvPressed = Input.GetKeyDown(_toggleFpvFallback) ||
                                        (!string.IsNullOrEmpty(_toggleFpv)
                                         && Input.GetButtonDown(_toggleFpv));
                    if (_toggleFpvPressed)
                    {
                        UpdateFPV();
                    }

                    if (_isOwner)
                    {
                        RespawnDrone();

                        _stopPilotingPressed = Input.GetKeyDown(_exitFallback) ||
                                               (!string.IsNullOrEmpty(_exit)
                                                && Input.GetButtonDown(_exit));
                        if (_stopPilotingPressed)
                        {
                            StopPiloting();
                        }
                    }
                }
                    break;
                case DroneStateCrashed:
                {
                    syncedMotorRpm = 0;

                    // switch (_previousState)
                    // {
                    //     case DroneStateOff:
                    //         break;
                    //     case DroneStateNotBooted:
                    //         break;
                    //     case DroneStateBooting:
                    //         break;
                    //     case DroneStateReady:
                    //         break;
                    //     case DroneStatePiloted:
                    //         break;
                    //     case DroneStateCrashed:
                    //         break;
                    //     case DroneStateGrabbed:
                    //         break;
                    //     default:
                    //         break;
                    // }

                    if (!_isPilot)
                    {
                        return;
                    }

                    if (!UpdateInputMethod())
                    {
                        return;
                    }

                    _toggleFpvPressed = Input.GetKeyDown(_toggleFpvFallback) ||
                                        (!string.IsNullOrEmpty(_toggleFpv)
                                         && Input.GetButtonDown(_toggleFpv));
                    if (_toggleFpvPressed)
                    {
                        UpdateFPV();
                    }

                    if (_isOwner)
                    {
                        RespawnDrone();

                        _stopPilotingPressed = Input.GetKeyDown(_exitFallback) ||
                                               (!string.IsNullOrEmpty(_exit)
                                                && Input.GetButtonDown(_exit));
                        if (_stopPilotingPressed)
                        {
                            StopPiloting();
                        }
                    }
                }
                    break;
                case DroneStateGrabbed:
                {
                    // switch (_previousState)
                    // {
                    //     case DroneStateOff:
                    //         break;
                    //     case DroneStateNotBooted:
                    //         break;
                    //     case DroneStateBooting:
                    //         break;
                    //     case DroneStateReady:
                    //         break;
                    //     case DroneStatePiloted:
                    //         break;
                    //     case DroneStateCrashed:
                    //         break;
                    //     case DroneStateGrabbed:
                    //         break;
                    //     default:
                    //         break;
                    // }

                    if (_isOwner)
                    {
                        if (vrcPickup.currentPlayer == null && !vrcPickup.IsHeld)
                        {
                            UpdateLocalState(_previousState);
                        }
                    }

                    if (!_isPilot)
                    {
                        return;
                    }

                    if (!UpdateInputMethod())
                    {
                        return;
                    }

                    syncedMotorRpm = Mathf.Clamp(
                        _throttle + (0.5f * (Mathf.Abs(_pitch) + Mathf.Abs(_roll) + Mathf.Abs(_yaw))), minRpm,
                        1f);

                    _toggleFpvPressed = Input.GetKeyDown(_toggleFpvFallback) ||
                                        (!string.IsNullOrEmpty(_toggleFpv)
                                         && Input.GetButtonDown(_toggleFpv));
                    if (_toggleFpvPressed)
                    {
                        UpdateFPV();
                    }
                }
                    break;
                default:
                {
                    Debug.LogError("Unknown drone state");
                }
                    break;
            }
        }

        private void StopPiloting()
        {
            _localPlayer.Immobilize(false);
            fpvCamera.gameObject.SetActive(false);
            fpvCamera.enabled = false;
            viewOverrideCamera.gameObject.SetActive(false);
            viewOverrideCamera.enabled = false;
            screen.gameObject.SetActive(false);

            startupSound.Stop();
            motorSound.Stop();
            UpdateLocalState(DroneStateOff);
        }

        private void RespawnDrone()
        {
            if (Input.GetKeyDown(_resetFallback) ||
                (!string.IsNullOrEmpty(_reset)
                 && Input.GetButtonDown(_reset)))
            {
                SpawnDroneForPlayer(this, _localPlayer);
                droneRigidbody.velocity = Vector3.zero;
                droneRigidbody.angularVelocity = Vector3.zero;

                UpdateLocalState(DroneStateReady);
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

        private void FinishDroneBoot(float time)
        {
            if (time > _bootUpCompletedTime)
            {
                UpdateLocalState(DroneStateReady);
                droneRigidbody.angularDrag = GetAngularDrag(_currentState);
            }
        }

        private void StartDroneBoot(float time)
        {
            if (!startupSound.IsPlaying())
            {
                _bootUpCompletedTime = time + startupSound.GetAudioClip().length;
                startupSound.Play(false);

                UpdateLocalState(DroneStateBooting);
                droneRigidbody.angularDrag = GetAngularDrag(_currentState);
            }
        }


        private void PlayMotorSound(bool play)
        {
            return; // TODO ENABLE ONCE AUDIO PERFORMANCE IS FIXED
            if (play)
            {
                _motorSoundProxy.volume = syncedMotorRpm;
                _motorSoundProxy.pitch = syncedMotorRpm;
                if (!motorSound.IsPlaying())
                {
                    motorSound.Play(true);
                }
            }
            else
            {
                if (motorSound.IsPlaying())
                {
                    motorSound.Stop();
                }
            }
        }

        private float i;


        private float Remap(float iMin, float iMax, float oMin, float oMax, float value)
        {
            var t = InverseLerp(iMin, iMax, value);
            return Lerp(oMin, oMax, t);
        }

        private float InverseLerp(float a, float b, float value) => (value - a) / (b - a);
        private float Lerp(float a, float b, float t) => (1f - t) * a + t * b;


        private void UpdateFPV()
        {
            var fpvEnabled = fpvCamera.gameObject.activeSelf;
            fpvCamera.gameObject.SetActive(!fpvEnabled);
            fpvCamera.enabled = !fpvEnabled;
            viewOverrideCamera.gameObject.SetActive(fpvEnabled);
            viewOverrideCamera.enabled = fpvEnabled;

            screen.gameObject.SetActive(!fpvEnabled);
        }

        public override void OnSpawn()
        {
            if (_isPilot)
            {
                StopPiloting();
            }
        }

        private void SpawnDroneForPlayer(Drone drone, VRCPlayerApi player)
        {
            var playerRotation = player.GetRotation();
            drone.gameObject.transform.SetPositionAndRotation(
                player.GetPosition() + playerRotation * Vector3.forward, playerRotation);
            SpawnScreenForPlayer(player);
        }

        private void SpawnScreenForPlayer(VRCPlayerApi player)
        {
            if (!screen) return;
            screen.gameObject.SetActive(true);
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

        #endregion
    }
}