using UnityEngine;
using UnityEngine.Rendering;

namespace MobileCore.Advertisements
{
    [DefaultExecutionOrder(10000)] // Run extremely late to override Cinemachine
    public class BannerAdViewportController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The camera to adjust. If empty, uses the Main Camera or camera on this object.")]
        [SerializeField] private Camera targetCamera;

        [Header("Background Settings")]
        [Tooltip("Color to fill the empty space (usually generic black)")]
        [SerializeField] private Color letterboxColor = Color.black;
        private Camera backgroundCamera;

        [Header("Banner Settings")]
        [Tooltip("Height of the banner in pixels (Standard is usually 50dp converted to pixels)")]
        [SerializeField] private float bannerHeightDp = 50f;
        
        [Tooltip("Position of the banner")]
        [SerializeField] private BannerPosition bannerPosition = BannerPosition.Bottom;

        [Header("Animation")]
        [SerializeField] private bool useAnimation = true;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Rect originalRect;
        private float originalOrthographicSize;
        private float originalFieldOfView;
        
        private bool isBannerVisible = false;
        private float currentHeightFactor = 1f;
        private Coroutine adjustmentCoroutine;

        // Static flag to persist state across scene reloads
        private static bool isBannerGlobalVisible = false;

        public enum BannerPosition { Top, Bottom }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            originalRect = targetCamera.rect;
            originalOrthographicSize = targetCamera.orthographicSize;
            originalFieldOfView = targetCamera.fieldOfView;
        }

        private void Start()
        {
            CreateBackgroundCamera();
        }

        private void CreateBackgroundCamera()
        {
            GameObject bgCamObj = new GameObject("BannerBackgroundCam");
            bgCamObj.transform.SetParent(this.transform);
            
            backgroundCamera = bgCamObj.AddComponent<Camera>();
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = letterboxColor;
            backgroundCamera.depth = targetCamera.depth - 1; 
            backgroundCamera.cullingMask = 0; 
            backgroundCamera.useOcclusionCulling = false;
            
            // Match projection type
            backgroundCamera.orthographic = targetCamera.orthographic;
        }

        private void OnEnable()
        {
            AdsManager.BannerShown += OnBannerShown;
            AdsManager.BannerHidden += OnBannerHidden;
            
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            
            // Sync with global state immediately
            if (isBannerGlobalVisible)
            {
                OnBannerShown();
            }
        }

        private void OnDisable()
        {
            AdsManager.BannerShown -= OnBannerShown;
            AdsManager.BannerHidden -= OnBannerHidden;
            
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            
            if (targetCamera != null)
            {
                targetCamera.rect = originalRect;
                targetCamera.ResetProjectionMatrix();
            }
        }

        private void OnBannerShown()
        {
            isBannerGlobalVisible = true;
            
            if (!isBannerVisible)
            {
                isBannerVisible = true;
                if (useAnimation && Application.isPlaying)
                {
                    if (adjustmentCoroutine != null) StopCoroutine(adjustmentCoroutine);
                    adjustmentCoroutine = StartCoroutine(AnimateViewport(true));
                }
                else
                {
                    currentHeightFactor = CalculateTargetHeightFactor();
                }
            }
        }

        private void OnBannerHidden()
        {
            isBannerGlobalVisible = false;
            
            if (isBannerVisible)
            {
                isBannerVisible = false;
                if (useAnimation && Application.isPlaying)
                {
                    if (adjustmentCoroutine != null) StopCoroutine(adjustmentCoroutine);
                    adjustmentCoroutine = StartCoroutine(AnimateViewport(false));
                }
                else
                {
                    currentHeightFactor = 1f;
                    if (targetCamera != null)
                    {
                        targetCamera.rect = originalRect;
                        targetCamera.ResetProjectionMatrix();
                    }
                }
            }
        }

        private void LateUpdate()
        {
            RefreshCameraSettings();
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam == targetCamera)
            {
                RefreshCameraSettings();
            }
        }

        private void RefreshCameraSettings()
        {
            if (targetCamera == null) return;
            
            if (!isBannerVisible && currentHeightFactor >= 0.999f) return;

            ApplyViewportRect();
            ApplyAntiDistortion();
        }

        private void ApplyViewportRect()
        {
            float screenRatio = currentHeightFactor; 
            Rect rect = originalRect;
            
            if (bannerPosition == BannerPosition.Bottom)
            {
                rect.y += (1f - screenRatio);
                rect.height *= screenRatio;
            }
            else
            {
                rect.height *= screenRatio;
            }
            
            targetCamera.rect = rect;
        }

        private void ApplyAntiDistortion()
        {
            float screenRatio = currentHeightFactor;
            if (screenRatio <= 0.001f) return;
            
            targetCamera.ResetProjectionMatrix();
            Matrix4x4 m = targetCamera.projectionMatrix;
            
            m[1, 1] *= (1f / screenRatio); 
            
            targetCamera.projectionMatrix = m;
        }

        private System.Collections.IEnumerator AnimateViewport(bool showing)
        {
            float elapsed = 0f;
            float startFactor = currentHeightFactor;
            float endFactor = showing ? CalculateTargetHeightFactor() : 1f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float curveValue = animationCurve.Evaluate(t);

                currentHeightFactor = Mathf.Lerp(startFactor, endFactor, curveValue);
                yield return null;
            }

            currentHeightFactor = endFactor;
            
            if (!showing)
            {
                targetCamera.rect = originalRect;
                targetCamera.ResetProjectionMatrix();
            }
        }

        private float CalculateTargetHeightFactor()
        {
             float fullBannerHeight = CalculatePixelHeight();
             return 1f - (fullBannerHeight / Screen.height);
        }

        private float CalculatePixelHeight()
        {
            float dpi = Screen.dpi;
            if (dpi == 0) dpi = 160; 
            return bannerHeightDp * (dpi / 160f);
        }
    }
}
