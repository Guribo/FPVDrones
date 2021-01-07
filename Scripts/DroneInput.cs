using System;
using UdonSharp;
using UnityEngine;

namespace Guribo.FPVDrones.Scripts
{
    public class DroneInput : UdonSharpBehaviour
    {
        [NonSerialized] public bool InUse;
        
        public string enter = "Oculus_CrossPlatform_Button4";
        public string exit = "Oculus_CrossPlatform_Button2";
        public string reset = "Oculus_CrossPlatform_PrimaryThumbstick";
        public string toggleFpv = "Oculus_CrossPlatform_SecondaryThumbstick";

        public string throttleMin = "Vertical";
        public string throttleMax = "Vertical";
        public string pitchMin = "Oculus_CrossPlatform_SecondaryThumbstickVertical";
        public string pitchMax = "Oculus_CrossPlatform_SecondaryThumbstickVertical";
        public string rollMin = "Oculus_CrossPlatform_SecondaryThumbstickHorizontal";
        public string rollMax = "Oculus_CrossPlatform_SecondaryThumbstickHorizontal";
        public string yawMin = "Horizontal";
        public string yawMax = "Horizontal";

        public KeyCode enterFallback = KeyCode.Return;
        public KeyCode exitFallback = KeyCode.Backspace;
        public KeyCode resetFallback = KeyCode.R;
        public KeyCode toggleFpvFallback = KeyCode.C;
        public KeyCode throttleUpFallback = KeyCode.LeftShift;
        public KeyCode throttleDownFallback = KeyCode.None;
        public KeyCode pitchUpFallback = KeyCode.S;
        public KeyCode pitchDownFallback = KeyCode.W;
        public KeyCode rollLeftFallback = KeyCode.A;
        public KeyCode rollRightFallback = KeyCode.D;
        public KeyCode yawLeftFallback = KeyCode.Q;
        public KeyCode yawRightFallback = KeyCode.E;

        public bool yawMinInverted = false;
        public bool yawMaxInverted = false;

        public bool pitchMinInverted = false;
        public bool pitchMaxInverted = false;

        public bool rollMinInverted = false;
        public bool rollMaxInverted = false;

        public bool throttleMinInverted = false;
        public bool throttleMaxInverted = false;

        public float yawExpo = 0.25f;
        public float pitchExpo = 0.25f;
        public float rollExpo = 0.25f;
        public float throttleExpo = 0.25f;

        public float yawRate = 1f;
        public float pitchRate = 1f;
        public float rollRate = 1f;
        public float throttleRate = 1f;

        public Vector2 yawInputCalibration = Vector2.one;
        public Vector2 pitchInputCalibration = Vector2.one;
        public Vector2 rollInputCalibration = Vector2.one;
        public Vector2 throttleInputCalibration = Vector2.one;

        private void Start()
        {
            Debug.LogError("Data behaviours should not be enabled in the scene");
            gameObject.SetActive(false);
        }

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
            var raw = GetInputAxis(yawMin, yawMax,
                yawLeftFallback,
                yawRightFallback,
                yawMinInverted,
                yawMaxInverted,
                yawInputCalibration);
            raw = ApplyExpo(raw, yawExpo);
            raw *= yawRate;
            return raw;
        }

        public float GetPitch()
        {
            var raw = GetInputAxis(pitchMin, pitchMax,
                pitchDownFallback,
                pitchUpFallback,
                pitchMinInverted,
                pitchMaxInverted,
                pitchInputCalibration);
            raw = ApplyExpo(raw, pitchExpo);
            raw *= pitchRate;
            return raw;
        }

        public float GetRoll()
        {
            var raw = GetInputAxis(rollMin, rollMax,
                rollLeftFallback,
                rollRightFallback,
                rollMinInverted,
                rollMaxInverted,
                rollInputCalibration);
            raw = ApplyExpo(raw, rollExpo);
            raw *= rollRate;
            return raw;
        }

        public float GetThrottle()
        {
            var raw = GetInputAxis(throttleMin, throttleMax,
                throttleDownFallback,
                throttleUpFallback,
                throttleMinInverted,
                throttleMaxInverted,
                throttleInputCalibration);
            raw = ApplyExpo(raw, throttleExpo);
            raw *= throttleRate;
            return raw;
        }

        private float GetAxisInput(string minAxisName,
            string maxAxisName,
            bool minInverted,
            bool maxInverted,
            Vector2 axisCalibration)
        {
            var axisValue = 0f;
            if (!string.IsNullOrWhiteSpace(minAxisName))
            {
                var min = (minInverted ? -1f : 1f) * Input.GetAxisRaw(minAxisName);
                var invertedInputIsNegative = min < 0f && minInverted;
                var normalInputIsPositive = min > 0f && !minInverted;

                if (!(invertedInputIsNegative || normalInputIsPositive))
                {
                    axisValue += (min / (axisCalibration.x > 0f ? axisCalibration.x : 1f));
                }
            }

            if (!string.IsNullOrWhiteSpace(maxAxisName))
            {
                var max = (maxInverted ? -1f : 1f) * Input.GetAxisRaw(maxAxisName);
                var invertedInputIsPositive = max > 0f && maxInverted;
                var normalInputIsNegative = max < 0f && !maxInverted;
                if (!(invertedInputIsPositive || normalInputIsNegative))
                {
                    axisValue += (max / (axisCalibration.y > 0f ? axisCalibration.y : 1f));
                }
            }

            return axisValue;
        }

        private float GetInputAxis(string minAxisName, string maxAxisName, KeyCode fallbackMin, KeyCode fallbackMax,
            bool minInverted, bool maxInverted, Vector2 axisCalibration)
        {
            var min = Input.GetKey(fallbackMin) ? -1f : 0f;
            var max = Input.GetKey(fallbackMax) ? 1f : 0f;
            var combinedInput = min + max + GetAxisInput((string) minAxisName, (string) maxAxisName, minInverted,
                maxInverted, axisCalibration);
            return Mathf.Clamp(combinedInput, -1, 1);
        }
    }
}