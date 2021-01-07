using Guribo.FPVDrone.Scripts;
using Guribo.UdonBetterAudio.Scripts;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace Guribo.FPVDrones.Scripts
{
    public class Drone : UdonSharpBehaviour
    {
        [HideInInspector] public int noPilot = -1;
        public int pilotId = -1;
        [UdonSynced(UdonSyncMode.Linear)] public float rpm;

        private float _throttle;
        private float _yaw;
        private float _pitch;
        private float _roll;

        [SerializeField] private BetterAudioSource startupSound;
        [SerializeField] private BetterAudioSource motorSound;
        private float _startUpTime = -1f;

        [SerializeField] private float maxEngineThrust = 3.5f;

        [SerializeField] private Rigidbody rigidbody;
        [SerializeField] private Camera fpvCamera;
        [SerializeField] private Camera viewOverrideCamera;

        public Transform screen;

        [SerializeField] private Transform motorFrontLeft;
        [SerializeField] private Transform motorFrontRight;
        [SerializeField] private Transform motorRearLeft;
        [SerializeField] private Transform motorRearRight;

        public DroneInput customDroneInput;
        public DroneInput[] droneInputs;


        public float minRpm = 0.2f;

        private bool _isVr;
        private bool _isLocallyControlled;
        private AudioSource _motorProxy;

        private DroneInput droneInput;
        public DroneUserController droneUserController;
        public VRCPickup vrcPickup;

        private bool _startPilotingPressed;
        private bool _stopPilotingPressed;
        private bool _toggleFpvPressed;
        private float _lastLocallyControlled;

        private bool _isPiloting;

        public void Start()
        {
            rigidbody.centerOfMass = Vector3.zero;
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                _isVr = localPlayer.IsUserInVR();
            }

            viewOverrideCamera.stereoTargetEye = _isVr ? StereoTargetEyeMask.Both : StereoTargetEyeMask.None;

            _motorProxy = motorSound.GetAudioSourceProxy();
        }


        public void Update()
        {
            UpdateInput();
            ControlPiloting();
            PlayMotorSound(true);
        }


        public void UpdateInput()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            if (_startUpTime < 0f || Time.time < _startUpTime)
            {
                if (!startupSound.IsPlaying() && _startUpTime < 0f)
                {
                    _startUpTime = Time.time + startupSound.GetAudioClip().length;
                    startupSound.Play(false);
                }

                return;
            }

            if (pilotId != localPlayer.playerId)
            {
                fpvCamera.gameObject.SetActive(true);
                viewOverrideCamera.gameObject.SetActive(false);
                return;
            }

            _isLocallyControlled = true;


            var vrcInputMethod = (int) InputManager.GetLastUsedInputMethod();

            if (droneInputs == null || vrcInputMethod >= droneInputs.Length)
            {
                return;
            }

            droneInput = customDroneInput.InUse ? customDroneInput : droneInputs[vrcInputMethod];
            if (!droneInput)
            {
                return;
            }

            if (Input.GetKeyDown(droneInput.resetFallback) ||
                (!string.IsNullOrEmpty(droneInput.reset)
                 && Input.GetButtonDown(droneInput.reset)))
            {
                SpawnDroneForPlayer(this, localPlayer);
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                return;
            }


            _stopPilotingPressed = Input.GetKeyDown(droneInput.exitFallback) ||
                                   (!string.IsNullOrEmpty(droneInput.exit)
                                    && Input.GetButtonDown(droneInput.exit));
            _startPilotingPressed = Input.GetKeyDown(droneInput.enterFallback) ||
                                    (!string.IsNullOrEmpty(droneInput.enter)
                                     && Input.GetButtonDown(droneInput.enter));

            if (_isPiloting)
            {
                _toggleFpvPressed = Input.GetKeyDown(droneInput.toggleFpvFallback) ||
                                    (!string.IsNullOrEmpty(droneInput.toggleFpv)
                                     && Input.GetButtonDown(droneInput.toggleFpv));


                rpm = Mathf.Clamp(_throttle + (0.5f * (Mathf.Abs(_pitch) + Mathf.Abs(_roll) + Mathf.Abs(_yaw))), minRpm,
                    1f);
            }
            else
            {
                _toggleFpvPressed = false;
                rpm = 0f;
            }
        }

        private void PlayMotorSound(bool play)
        {
            if (play)
            {
                _motorProxy.volume = rpm;
                _motorProxy.pitch = rpm;
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

        public void FixedUpdate()
        {
            if (vrcPickup.IsHeld)
            {
                var owner = Networking.GetOwner(gameObject);
                if (owner != null)
                {
                    owner.PlayHapticEventInHand(vrcPickup.currentHand, Time.fixedDeltaTime, rpm, rpm * 10f);
                }
            }


            if (!_isLocallyControlled || !droneInput || !_isPiloting)
            {
                return;
            }

            _yaw = droneInput.GetYaw();
            _pitch = droneInput.GetPitch();
            _roll = droneInput.GetRoll();
            _throttle = droneInput.GetThrottle();

            rigidbody.AddForceAtPosition(
                motorFrontLeft.up * (Mathf.Clamp01(_pitch + _roll + _throttle) * maxEngineThrust),
                motorFrontLeft.position);
            rigidbody.AddForceAtPosition(
                motorFrontRight.up * (Mathf.Clamp01(_pitch - _roll + _throttle) * maxEngineThrust),
                motorFrontRight.position);
            rigidbody.AddForceAtPosition(
                motorRearLeft.up * (Mathf.Clamp01(-_pitch + _roll + _throttle) * maxEngineThrust),
                motorRearLeft.position);
            rigidbody.AddForceAtPosition(
                motorRearRight.up * (Mathf.Clamp01(-_pitch - _roll + _throttle) * maxEngineThrust),
                motorRearRight.position);

            rigidbody.AddTorque(transform.up * _yaw);
        }


        private float Remap(float iMin, float iMax, float oMin, float oMax, float value)
        {
            var t = InverseLerp(iMin, iMax, value);
            return Lerp(oMin, oMax, t);
        }

        private float InverseLerp(float a, float b, float value) => (value - a) / (b - a);
        private float Lerp(float a, float b, float t) => (1f - t) * a + t * b;


        private void ControlPiloting()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return;
            if (pilotId == localPlayer.playerId)
            {
                if (_startPilotingPressed)
                {
                    localPlayer.Immobilize(true);

                    SpawnScreenForPlayer(localPlayer);
                    _isPiloting = true;
                }

                if (_toggleFpvPressed)
                {
                    var fpvEnabled = fpvCamera.gameObject.activeSelf;
                    fpvCamera.gameObject.SetActive(!fpvEnabled);
                    fpvCamera.enabled = !fpvEnabled;
                    viewOverrideCamera.gameObject.SetActive(fpvEnabled);
                    viewOverrideCamera.enabled = fpvEnabled;
                }

                if (_stopPilotingPressed)
                {
                    localPlayer.Immobilize(false);
                    _isPiloting = false;
                    fpvCamera.gameObject.SetActive(true);
                    fpvCamera.enabled = true;
                    viewOverrideCamera.gameObject.SetActive(false);
                    viewOverrideCamera.enabled = false;
                }

                _lastLocallyControlled = Time.time;
            }


            if (Time.time - _lastLocallyControlled > 0.5f)
            {
                if (Networking.IsOwner(gameObject))
                {
                    localPlayer.Immobilize(false);
                    _isPiloting = false;
                }

                fpvCamera.gameObject.SetActive(true);
                fpvCamera.enabled = true;
                viewOverrideCamera.gameObject.SetActive(false);
                viewOverrideCamera.enabled = false;
            }
        }

        public override void OnSpawn()
        {
            var vrcPlayerApi = Networking.LocalPlayer;
            if (vrcPlayerApi == null) return;
            vrcPlayerApi.Immobilize(false);
            _isPiloting = false;

            fpvCamera.gameObject.SetActive(true);
            fpvCamera.enabled = true;
            viewOverrideCamera.gameObject.SetActive(false);
            viewOverrideCamera.enabled = false;
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
            var playerRotation = player.GetRotation();
            screen.SetPositionAndRotation(
                player.GetPosition() + playerRotation * Vector3.forward, playerRotation);
        }
    }
}