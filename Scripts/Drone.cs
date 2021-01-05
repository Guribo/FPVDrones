using System;
using Guribo.UdonBetterAudio.Scripts;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.FPVDrone.Scripts
{
    public class Drone : UdonSharpBehaviour
    {
        [HideInInspector] public int noPilot = -1;
        [UdonSynced] public int pilotId = -1;
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
        [SerializeField] private Transform spawn;
        [SerializeField] private Camera fpvCamera;
        [SerializeField] private Camera viewOverrideCamera;

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
        }


        public void UpdateInput()
        {
            if (pilotId == noPilot)
            {
                fpvCamera.gameObject.SetActive(false);
                viewOverrideCamera.gameObject.SetActive(false);
                rpm = 0f;
                PlayMotorSound(false);
                return;
            }
            else
            {
                PlayMotorSound(true);
            }


            _isLocallyControlled = false;
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            if (pilotId != localPlayer.playerId)
            {
                fpvCamera.gameObject.SetActive(true);
                viewOverrideCamera.gameObject.SetActive(false);
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
                (!string.IsNullOrEmpty(droneInput.reset) && Input.GetButtonDown(droneInput.reset)))
            {
                transform.SetPositionAndRotation(spawn.position, spawn.rotation);
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            if (Input.GetKeyDown(droneInput.toggleFpvFallback) ||
                (!string.IsNullOrEmpty(droneInput.toggleFpv) && Input.GetButtonDown(droneInput.toggleFpv))
                || !(fpvCamera.gameObject.activeSelf || viewOverrideCamera.gameObject.activeSelf))
            {
                var fpvEnabled = fpvCamera.gameObject.activeSelf;
                fpvCamera.gameObject.SetActive(!fpvEnabled);
                fpvCamera.enabled = !fpvEnabled;
                viewOverrideCamera.gameObject.SetActive(fpvEnabled);
                viewOverrideCamera.enabled = fpvEnabled;
            }

            _isLocallyControlled = true;

            rpm = Mathf.Clamp01(_throttle + (0.5f * (Mathf.Abs(_pitch) + Mathf.Abs(_roll) + Mathf.Abs(_yaw))));
        }

        private void PlayMotorSound(bool play)
        {
            if (play)
            {
                var volume = Remap(0, 1, minRpm, 1f, rpm);
                _motorProxy.volume = volume;
                _motorProxy.pitch = volume;
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
            if (!_isLocallyControlled || !droneInput)
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
    }
}