#pragma warning disable 0649
#pragma warning disable 0414

using UnityEngine;

namespace MobileCore.SystemModule
{
    [System.Serializable]
    public class ScreenSettings
    {
        [Header("Frame Rate")]
        [InspectorName("Auto Detect Frame Rate")]
        [Tooltip("Automatically set frame rate based on device refresh rate.")]
        [SerializeField] private bool setFrameRateAutomatically = false;

        [Space]
        [InspectorName("Default FPS")]
        [Tooltip("Target frame rate for normal operation.")]
        [SerializeField] private AllowedFrameRates defaultFrameRate = AllowedFrameRates.Rate60;

        [InspectorName("Battery Save FPS")]
        [Tooltip("Frame rate when device is in low power mode (iOS only).")]
        [SerializeField] private AllowedFrameRates batterySaveFrameRate = AllowedFrameRates.Rate30;

        [Header("Sleep")]
        [InspectorName("Sleep Timeout")]
        [Tooltip("-1 = never sleep; -2 = system setting; 0 = system default with custom timeout.")]
        [SerializeField] private int sleepTimeout = -1;

        [InspectorName("Custom Timeout")]
        [Tooltip("Custom sleep timeout in seconds (used with System Default).")]
        [SerializeField] private int customSleepTimeout = 300; // Default 5 menit

        public void Initialize()
        {
            // Jika sleepTimeout = 0 (System Default), gunakan customSleepTimeout
            int actualTimeout = sleepTimeout == 0 ? customSleepTimeout : sleepTimeout;
            Screen.sleepTimeout = actualTimeout;

            if (setFrameRateAutomatically)
            {
                uint numerator = Screen.currentResolution.refreshRateRatio.numerator;
                uint denominator = Screen.currentResolution.refreshRateRatio.denominator;

                if (numerator != 0 && denominator != 0)
                {
                    Application.targetFrameRate = Mathf.RoundToInt(numerator / denominator);
                }
                else
                {
                    Application.targetFrameRate = (int)defaultFrameRate;
                }
            }
            else
            {
#if UNITY_IOS
                if(UnityEngine.iOS.Device.lowPowerModeEnabled)
                {
                    Application.targetFrameRate = (int)batterySaveFrameRate;
                }
                else
                {
                    Application.targetFrameRate = (int)defaultFrameRate;
                }    
#else
                Application.targetFrameRate = (int)defaultFrameRate;
#endif
            }
        }

        private enum AllowedFrameRates
        {
            Rate30 = 30,
            Rate60 = 60,
            Rate90 = 90,
            Rate120 = 120,
        }
    }
}