using System;
using UdonSharp;
using UnityEngine;

namespace Guribo.FPVDrones.Scripts
{
    public class DroneInput : UdonSharpBehaviour
    {
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
    }
}