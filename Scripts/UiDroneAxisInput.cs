using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Guribo.FPVDrone.Scripts
{
    public class UiDroneAxisInput : UdonSharpBehaviour
    {
        public DroneInput customDroneInput;
        public string customDroneInputExpo = "yawExpo";
        public string customDroneInputRates = "yawRate";
        public string customDroneInputInvertedMin = "yawMinInverted";
        public string customDroneInputInvertedMax = "yawMaxInverted";
        public string customDroneInputCalibration = "yawInputCalibration";
        public string customDroneInputMinAxisName = "yawMin";
        public string customDroneInputMaxAxisName = "yawMax";


        [SerializeField] private Slider expoSlider;
        [SerializeField] private Slider ratesSlider;

        // preview image using the input preview shader Guribo/FPVDrones/UI/ExpoGraph
        [SerializeField] private Image previewImage;

        [SerializeField] private GameObject minAxisSelectionHint;
        [SerializeField] private GameObject maxAxisSelectionHint;

        [SerializeField] private GameObject minInvertedHint;
        [SerializeField] private GameObject maxInvertedHint;

        [SerializeField] private Text minAxis;
        [SerializeField] private Text maxAxis;

        [SerializeField] private Text minCalibration;
        [SerializeField] private Text maxCalibration;

        private const string AxisInputMaterialProperty = "_AxisInput";
        private const string ExpoMaterialProperty = "_Expo";
        private const string RatesMaterialProperty = "_MaxRate";

        // material used for previewing axis value, rate and smoothing (expo)
        private Material _previewMaterial;

        // threshold which triggers selecting the axis which exceeds this value
        private const float SelectionThreshold = 0.25f;

        // caches whether the preview material has the required property (optimization)
        private bool _materialHasAxisInputProperty;

        // caches whether the preview material has the required property (optimization)
        private bool _materialHasExpoProperty;

        // caches whether the preview material has the required property (optimization)
        private bool _materialHasRatesProperty;

        // whether the min axis can currently be assigned
        private bool _selectingMin;

        // whether the max axis can currently be assigned
        private bool _selectingMax;

        // whether the selected min axis provides positive axis values
        private bool _minInverted;

        // whether the selected max axis provides negative axis values
        private bool _maxInverted;

        // whether a custom minimum axis was ever selected
        private bool _customMin;

        // whether a custom maximum axis was ever selected
        private bool _customMax;

        // delay that prevent accidentally selecting the trigger e.g. in VR when it is used to press the UI button
        private const float SelectionActivationDelay = 0.5f;

        // time after which axis values values are evaluated for axis selection
        private float _selectionActivationTime;

        private float _minAxisCalibration;
        private float _maxAxisCalibration;


        private float[] _inputAxisValues;
        private float[] _absPreviousInputAxisValues;

#if UNITY_EDITOR
        private readonly string[] _inputAxisNames = new[]
        {
            "Horizontal",
            "Vertical"
        };
#else
        private readonly string[] _inputAxisNames = new[]
        {
            "Joy1 Axis 1",
            "Joy1 Axis 2",
            "Joy1 Axis 3",
            "Joy1 Axis 4",
            "Joy1 Axis 5",
            "Joy1 Axis 6",
            "Joy1 Axis 7",
            "Joy1 Axis 8",
            "Joy1 Axis 9",
            "Joy1 Axis 10",
            "Joy2 Axis 1",
            "Joy2 Axis 2",
            "Joy2 Axis 3",
            "Joy2 Axis 4",
            "Joy2 Axis 5",
            "Joy2 Axis 6",
            "Joy2 Axis 7",
            "Joy2 Axis 8",
            "Joy2 Axis 9",
            "Joy2 Axis 10",
            "Oculus_CrossPlatform_PrimaryIndexTrigger",
            "Oculus_CrossPlatform_SecondaryIndexTrigger",
            "Oculus_CrossPlatform_PrimaryHandTrigger",
            "Oculus_CrossPlatform_SecondaryHandTrigger",
            "Oculus_CrossPlatform_PrimaryThumbstickHorizontal",
            "Oculus_CrossPlatform_PrimaryThumbstickVertical",
            "Oculus_CrossPlatform_SecondaryThumbstickHorizontal",
            "Oculus_CrossPlatform_SecondaryThumbstickVertical",
            "Oculus_GearVR_LThumbstickX",
            "Oculus_GearVR_LThumbstickY",
            "Oculus_GearVR_RThumbstickX",
            "Oculus_GearVR_RThumbstickY",
            "Oculus_GearVR_DpadX",
            "Oculus_GearVR_DpadY",
            "Oculus_GearVR_LIndexTrigger",
            "Oculus_GearVR_RIndexTrigger"
        };
#endif


        private void Start()
        {
            _inputAxisValues = new float[_inputAxisNames.Length];
            _absPreviousInputAxisValues = new float[_inputAxisNames.Length];

            if (!previewImage)
            {
                return;
            }

            _previewMaterial = previewImage.material;

            if (!_previewMaterial) return;
            _materialHasAxisInputProperty = _previewMaterial.HasProperty(AxisInputMaterialProperty);
            _materialHasRatesProperty = _previewMaterial.HasProperty(RatesMaterialProperty);
            _materialHasExpoProperty = _previewMaterial.HasProperty(ExpoMaterialProperty);
        }

        public void MaxAxisButtonClicked()
        {
            _selectingMin = false;
            _selectingMax = !_selectingMax;
            if (_selectingMax)
            {
                _maxAxisCalibration = 1f;
            }

            SetSelectionActivationTime(_selectingMax);
            ClearCalibrationRecordings();
            RefreshSelectionAxisButtons();
        }

        public void MinAxisButtonClicked()
        {
            _selectingMax = false;
            _selectingMin = !_selectingMin;
            if (_selectingMin)
            {
                _minAxisCalibration = 1f;
            }

            SetSelectionActivationTime(_selectingMin);
            ClearCalibrationRecordings();
            RefreshSelectionAxisButtons();
        }

        private void ClearCalibrationRecordings()
        {
            for (var i = 0; i < _inputAxisValues.Length; i++)
            {
                _inputAxisValues[i] = 0f;
                _absPreviousInputAxisValues[i] = 0f;
            }
        }

        public void ExpoChanged()
        {
            if (!expoSlider)
            {
                return;
            }

            if (!_previewMaterial)
            {
                return;
            }

            if (!_materialHasExpoProperty)
            {
                return;
            }

            _previewMaterial.SetFloat(ExpoMaterialProperty, expoSlider.value);


            if (customDroneInput)
            {
                customDroneInput.InUse = true;
                customDroneInput.SetProgramVariable(customDroneInputExpo, expoSlider.value);
            }
        }

        public void RatesChanged()
        {
            if (!ratesSlider)
            {
                return;
            }

            if (!_previewMaterial)
            {
                return;
            }

            if (!_materialHasRatesProperty)
            {
                return;
            }

            _previewMaterial.SetFloat(RatesMaterialProperty, ratesSlider.value);

            if (customDroneInput)
            {
                customDroneInput.InUse = true;
                customDroneInput.SetProgramVariable(customDroneInputRates, ratesSlider.value);
            }
        }

        public void ResetPressed()
        {
        }

        public void LateUpdate()
        {
            SelectMinAxis();
            SelectMaxAxis();
            PreviewAxisInput();
        }

        private void PreviewAxisInput()
        {
            if (!_previewMaterial)
            {
                return;
            }

            if (!_materialHasAxisInputProperty)
            {
                return;
            }

            var axisValue = 0f;
            if (_customMin)
            {
                var axis = (_minInverted ? -1f : 1f) * Input.GetAxisRaw(minAxis.text);
                var invertedInputIsNegative = axis < 0f && _minInverted;
                var normalInputIsPositive = axis > 0f && !_minInverted;

                if (!(invertedInputIsNegative || normalInputIsPositive))
                {
                    axisValue += axis / (_minAxisCalibration > 0f ? _minAxisCalibration : 1f);
                }
            }

            if (_customMax)
            {
                var axis = (_maxInverted ? -1f : 1f) * Input.GetAxisRaw(maxAxis.text);
                var invertedInputIsPositive = axis > 0f && _maxInverted;
                var normalInputIsNegative = axis < 0f && !_maxInverted;
                if (!(invertedInputIsPositive || normalInputIsNegative))
                {
                    axisValue += axis / (_maxAxisCalibration > 0f ? _minAxisCalibration : 1f);
                }
            }

            _previewMaterial.SetFloat(AxisInputMaterialProperty, axisValue);
        }

        private void SelectMaxAxis()
        {
            if (_selectingMax && SelectingDelayElapsed())
            {
                for (var i = 0; i < _inputAxisNames.Length; i++)
                {
                    _absPreviousInputAxisValues[i] = Mathf.Abs(_inputAxisValues[i]);
                    _inputAxisValues[i] = Input.GetAxisRaw(_inputAxisNames[i]);
                    var absAxisValue = Mathf.Abs(_inputAxisValues[i]);
                    var axisSelected = absAxisValue > SelectionThreshold;
                    if (axisSelected)
                    {
                        if (maxAxis) maxAxis.text = _inputAxisNames[i];
                        _maxInverted = _inputAxisValues[i] > 0f;
                        var calibrated = absAxisValue - _absPreviousInputAxisValues[i] <= 0f;
                        _maxAxisCalibration = Mathf.Max(absAxisValue, _absPreviousInputAxisValues[i]);
                        _selectingMax = !calibrated;
                        _customMax = calibrated;

                        if (maxCalibration) maxCalibration.text = _maxAxisCalibration.ToString("F2");
                        if (maxInvertedHint) maxInvertedHint.SetActive(_maxInverted);
                        if (maxAxisSelectionHint) maxAxisSelectionHint.SetActive(_selectingMax);

                        if (calibrated && customDroneInput)
                        {
                            customDroneInput.InUse = true;
                            customDroneInput.SetProgramVariable(customDroneInputInvertedMax, _maxInverted);
                            customDroneInput.SetProgramVariable(customDroneInputMaxAxisName, _inputAxisNames[i]);
                            var calibration =
                                (Vector2) customDroneInput.GetProgramVariable(customDroneInputCalibration);
                            calibration.y = _maxAxisCalibration;
                            customDroneInput.SetProgramVariable(customDroneInputCalibration, calibration);
                        }

                        break;
                    }
                }
            }
        }

        private void SelectMinAxis()
        {
            if (_selectingMin && SelectingDelayElapsed())
            {
                for (var i = 0; i < _inputAxisNames.Length; i++)
                {
                    _absPreviousInputAxisValues[i] = Mathf.Abs(_inputAxisValues[i]);
                    _inputAxisValues[i] = Input.GetAxisRaw(_inputAxisNames[i]);
                    var absAxisValue = Mathf.Abs(_inputAxisValues[i]);
                    var axisSelected = absAxisValue > SelectionThreshold;
                    if (axisSelected)
                    {
                        if (minAxis) minAxis.text = _inputAxisNames[i];
                        _minInverted = _inputAxisValues[i] > 0f;
                        var calibrated = absAxisValue - _absPreviousInputAxisValues[i] <= 0f;
                        _minAxisCalibration = Mathf.Max(absAxisValue, _absPreviousInputAxisValues[i]);
                        _selectingMin = !calibrated;
                        _customMin = calibrated;

                        if (minCalibration) minCalibration.text = _minAxisCalibration.ToString("F2");
                        if (minInvertedHint) minInvertedHint.SetActive(_minInverted);
                        if (minAxisSelectionHint) minAxisSelectionHint.SetActive(_selectingMin);

                        if (calibrated && customDroneInput)
                        {
                            customDroneInput.InUse = true;
                            customDroneInput.SetProgramVariable(customDroneInputInvertedMin, _minInverted);
                            customDroneInput.SetProgramVariable(customDroneInputMinAxisName, _inputAxisNames[i]);
                            var calibration =
                                (Vector2) customDroneInput.GetProgramVariable(customDroneInputCalibration);
                            calibration.x = _maxAxisCalibration;
                            customDroneInput.SetProgramVariable(customDroneInputCalibration, calibration);
                        }

                        break;
                    }
                }
            }
        }

        private void SetSelectionActivationTime(bool set)
        {
            if (!set) return;
            _selectionActivationTime = Time.time + SelectionActivationDelay;
        }

        private void RefreshSelectionAxisButtons()
        {
            if (maxAxisSelectionHint) maxAxisSelectionHint.SetActive(_selectingMax);
            if (minAxisSelectionHint) minAxisSelectionHint.SetActive(_selectingMin);
        }

        private bool SelectingDelayElapsed()
        {
            return Time.time > _selectionActivationTime;
        }
    }
}