using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
	using UnityEditor;
#endif

namespace Lumeneo
{



    #if UNITY_EDITOR
	    [ExecuteInEditMode]
	    [RequireComponent(typeof(Camera))]
	    [AddComponentMenu("Rendering/AdaptiveGraphics", 90)]
	#endif


	//
	// Adaptive Graphics Manager Component
	//
	public class AdaptiveGraphicsManager : MonoBehaviour
    {


        const string
            CAMERA_OLD 		    = "AdaptiveGraphicsCamera",
            BILLBOARD_OLD 	    = "AdaptiveGraphicsBillboard",
            CAMERA_INSTANCED 	= "AdaptiveGraphicsCameraInstanced",
            BILLBOARD_INSTANCED = "AdaptiveGraphicsBillboardInstanced";

        const int
            BILLBOARD_LAYER = 29;

        public Action
            repaintInspectorAction;



        [SerializeField]
        DOWNSAMPLING_METHOD _method = DOWNSAMPLING_METHOD.AdaptativeDownsampling;

        public DOWNSAMPLING_METHOD method
        {
            get { return _method; }
            set
            {
                if (_method != value)
                {
                    _method = value;
                    ClearRTArray();
                    UpdateSettings();
                    switch (_method)
                    {
                        case DOWNSAMPLING_METHOD.AdaptativeDownsampling:
                            _downsampling = 0.25f;
                            _staticDownsampling = 0.25f;
                            break;
                        case DOWNSAMPLING_METHOD.HorizontalDownsampling:
                            _downsampling = 0.5f;
                            _staticDownsampling = 0.5f;
                            break;
                        case DOWNSAMPLING_METHOD.QuadDownsampling:
                            _downsampling = 0.5f;
                            _staticDownsampling = 0.5f;
                            break;
                        case DOWNSAMPLING_METHOD.Disabled:
                            CheckCamera();
                            break;
                    }
                    rtDownsampling = 0f;
                    CheckRT();
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        FILTERING_MODE
            _filtering = FILTERING_MODE.Bilinear;

        public FILTERING_MODE filtering
        {
            get { return _filtering; }
            set
            {
                if (_filtering != value)
                {
                    _filtering = value;
                    ClearRTArray();
                    UpdateSettings();
                    rtDownsampling = 0f;
                    CheckRT();
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        int _targetFPS = 30;

        public int targetFPS
        {
            get { return _targetFPS; }
            set
            {
                if (_targetFPS != value)
                {
                    _targetFPS = Mathf.Clamp(value, 10, 120);
                    if (_targetFPS > _niceFPS)
                        _niceFPS = _targetFPS;
                    UpdateSettings();
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        CameraClearFlags _cameraClearFlags = CameraClearFlags.SolidColor;


        public CameraClearFlags cameraClearFlags
        {
            get { return _cameraClearFlags; }
            set
            {
                if (_cameraClearFlags != value)
                {
                    _cameraClearFlags = value;
                    if (agCamera != null)
                        agCamera.clearFlags = _cameraClearFlags;
                    isDirty = true;
                }
            }
        }


        [SerializeField]
        bool
            _niceFPSEnabled = false;

        public bool niceFPSEnabled
        {
            get { return _niceFPSEnabled; }
            set
            {
                if (_niceFPSEnabled != value)
                {
                    _niceFPSEnabled = value;
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        bool
            _prewarm = false;

        public bool prewarm
        {
            get { return _prewarm; }
            set
            {
                if (_prewarm != value)
                {
                    _prewarm = value;
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        int
            _niceFPS = 55;

        public int niceFPS
        {
            get { return _niceFPS; }
            set
            {
                if (_niceFPS != value)
                {
                    _niceFPS = Mathf.Clamp(value, _targetFPS, 120);
                    UpdateSettings();
                    isDirty = true;
                }
            }
        }

        public bool niceFPSisActive { get { return avgFPSNice >= _niceFPS && _niceFPSEnabled; } }

        [SerializeField]
        float
            _downsampling = 0.25f;

        public float downsampling
        {
            get { return _downsampling; }
            set
            {
                if (_downsampling != value)
                {
                    _downsampling = rtClamp(value);
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        float
            _staticDownsampling = 0.3f;

        public float staticCameraDownsampling
        {
            get { return _staticDownsampling; }
            set
            {
                if (_staticDownsampling != value)
                {
                    _staticDownsampling = rtClamp(value);
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        float
            _fpsChangeSpeedUp = 0.02f;

        public float fpsChangeSpeedUp
        {
            get { return _fpsChangeSpeedUp; }
            set
            {
                if (_fpsChangeSpeedUp != value)
                {
                    _fpsChangeSpeedUp = Mathf.Clamp(value, 0.01f, 0.1f);
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        float
            _fpsChangeSpeedDown = 0.02f;

        public float fpsChangeSpeedDown
        {
            get { return _fpsChangeSpeedDown; }
            set
            {
                if (_fpsChangeSpeedDown != value)
                {
                    _fpsChangeSpeedDown = Mathf.Clamp(value, 0.01f, 0.1f);
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        bool
            _sharpen = false;

        public bool sharpen
        {
            get { return _sharpen; }
            set
            {
                if (_sharpen != value)
                {
                    _sharpen = value;
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        int
            _antialias = 1;

        public int antialias
        {
            get { return _antialias; }
            set
            {
                if (_antialias != value)
                {
                    _antialias = value;
                    if (mainCamera != null && mainCamera.targetTexture != null && mainCamera.targetTexture.antiAliasing != getAntialiasLevel())
                    {
                        ClearRTArray();
                        CheckRT();
                    }
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        bool
            _reducePixelLights = true;

        public bool reducePixelLights
        {
            get { return _reducePixelLights; }
            set
            {
                if (_reducePixelLights != value)
                {
                    _reducePixelLights = value;
                    if (!_reducePixelLights && oldPixelLightCount > 0)
                    {
                        QualitySettings.pixelLightCount = oldPixelLightCount;
                    }
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        bool
            _manageShadows = true;

        public bool manageShadows
        {
            get { return _manageShadows; }
            set
            {
                if (_manageShadows != value)
                {
                    _manageShadows = value;
                    if (!_manageShadows)
                        ResetShadows();
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        COMPOSITING_METHOD
            _compositingMethod = COMPOSITING_METHOD.Simple;

        public COMPOSITING_METHOD compositingMethod
        {
            get { return _compositingMethod; }
            set
            {
                if (_compositingMethod != value)
                {
                    _compositingMethod = value;
                    DestroyAdaptiveGraphicsCamera();
                    CheckCamera();
                    isDirty = true;
                }
            }
        }

        [NonSerialized]
        public GameObject
            adaptiveGraphicsCameraObj;
        [NonSerialized]
        public GameObject
            agBillboardObj;
        [NonSerialized]
        public bool
            isDirty;
        static AdaptiveGraphicsManager _adaptiveGraphics;

        public static AdaptiveGraphicsManager instance
        {
            get
            {
                if (_adaptiveGraphics == null)
                {
                    foreach (Camera camera in Camera.allCameras)
                    {
                        _adaptiveGraphics = camera.GetComponent<AdaptiveGraphicsManager>();
                        if (_adaptiveGraphics != null)
                            break;
                    }
                }
                return _adaptiveGraphics;
            }
        }

        public int currentFPS
        {
            get { return fps; }
        }

        public float appliedDownsampling
        {
            get { return rtDownsampling; }
        }

        public RenderTexture rt
        {
            get
            {
                if (mainCamera == null)
                    return null;
                else
                    return mainCamera.targetTexture;
            }
        }

        public float activeDownsampling
        {
            get
            {
                return cameraIsStatic ? _staticDownsampling : _downsampling;
            }
        }


        /* Internal fields */

        RenderTexture[] rtArray;
        int frameCount;
        float nextPeriod;
        int fps;
        const float FPS_UPDATE_RATE = 0.5f;
        Camera mainCamera, agCamera;
        AdaptiveGraphicsPostFrameBehavior agPost;
        float avgDownsampling, rtDownsampling;
        int oldVSyncCount;
        bool cameraIsStatic;
        Vector3 oldCameraPos, oldCameraRot;
        int oldPixelLightCount;
        List<Light> lights;
        float lastLightCheckTime;
        Dictionary<Light,LightShadows> oldLightShadows;
        int _screenWidth, _screenHeight;
        Material adaptiveGraphicsBillboardMat;
        GUITexture adaptiveGraphicsBillboardTex;
        float lastNiceTimeCheck;
        float avgFPSNice;
        float lastInspectorRefresh;
        int camMovDetectThreshold;

        int screenWidth
        {
            get
            {
                #if UNITY_EDITOR
                return _screenWidth;
                #else
				return Screen.width;
                #endif
            }
        }

        int screenHeight
        {
            get
            {
                #if UNITY_EDITOR
                return _screenHeight;
                #else
				return Screen.height;
                #endif
            }
        }



        #region Game loop events

        // Creates a private material used to the effect
        void OnEnable()
        {
            Init();
        }

        void OnReset()
        {
            Init();
        }

        void Init()
        {
            oldLightShadows = new Dictionary<Light, LightShadows>();
            oldVSyncCount = QualitySettings.vSyncCount;
            fps = _targetFPS;
            avgDownsampling = 1f;
            ClearRTArray();
            ClearOldInstancedCamera();
            CheckCamera();
            UpdateSettings();
            if (_prewarm)
                PrewarmRTs();
            CheckRT();
            UpdateUICanvases();
        }

        void ClearOldInstancedCamera()
        {
            for (int k = 0; k < 50; k++)
            {
                GameObject go = GameObject.Find(CAMERA_OLD);
                if (go != null)
                    DestroyImmediate(go);
            }
            for (int k = 0; k < 50; k++)
            {
                GameObject go = GameObject.Find(BILLBOARD_OLD);
                if (go != null)
                    DestroyImmediate(go);
            }
        }

        void PrewarmRTs()
        {
            if (_method == DOWNSAMPLING_METHOD.AdaptativeDownsampling)
            {
                for (int k = 1; k <= 10; k++)
                {
                    avgDownsampling = k / 10f;
                    CheckRT();
                }
            }
            else
            {
                cameraIsStatic = false;
                CheckRT();
                cameraIsStatic = true;
                CheckRT();
            }
        }

        void ClearRTArray()
        {
            if (rtArray != null)
            {
                for (int k = 0; k < rtArray.Length; k++)
                {
                    CheckAndReleaseRT(rtArray[k]);
                    rtArray[k] = null;
                }
            }
            else
            {
                rtArray = new RenderTexture[10];
            }
        }

        void OnDisable()
        {
            if (mainCamera != null)
            {
                mainCamera.targetTexture = null;
            }
            if (agCamera != null)
            {
                agCamera.enabled = false; //.SetActive (false);
            }
            if (agBillboardObj != null)
            {
                agBillboardObj.SetActive(false);
            }
            ResetShadows();
            RestoreUICanvases();
        }

        void OnDestroy()
        {
            DestroyAdaptiveGraphicsCamera();
            ClearRTArray();
        }

        void DestroyAdaptiveGraphicsCamera()
        {
            if (adaptiveGraphicsCameraObj != null)
            {
                DestroyImmediate(adaptiveGraphicsCameraObj);
                adaptiveGraphicsCameraObj = null;
            }
            if (agBillboardObj != null)
            {
                DestroyImmediate(agBillboardObj);
                agBillboardObj = null;
            }
            RestoreUICanvases();
        }

        void Start()
        {
            nextPeriod = Time.realtimeSinceStartup + FPS_UPDATE_RATE;
            oldPixelLightCount = QualitySettings.pixelLightCount;
        }

        void LateUpdate()
        {
            #if UNITY_EDITOR
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;

            if (repaintInspectorAction != null && Time.time - lastInspectorRefresh > 3f)
            {
                lastInspectorRefresh = Time.time;
                repaintInspectorAction();
            }
            #endif

            if (Application.isPlaying && _niceFPSEnabled)
            {
                float niceTimeInterval = mainCamera.targetTexture != null ? 1f : 5f;
                if (Time.time - lastNiceTimeCheck > niceTimeInterval)
                {
                    avgFPSNice = (avgFPSNice + fps) * 0.5f;
                    lastNiceTimeCheck = Time.time;
                    if (avgFPSNice >= _niceFPS && mainCamera.targetTexture != null)
                    {
                        // Clear AdaptiveGraphics elements
                        mainCamera.targetTexture = null;
                        if (agCamera != null)
                            agCamera.enabled = false;
//																								if (adaptiveGraphicsCameraObj != null)
//																												adaptiveGraphicsCameraObj.SetActive (false);
                        frameCount += _targetFPS;
                    }
                }
            }

            if (_compositingMethod != COMPOSITING_METHOD.Simple)
            {
                if (adaptiveGraphicsCameraObj == null || (_compositingMethod != COMPOSITING_METHOD.SecondCameraBlit && agBillboardObj == null))
                {
                    CheckCamera();
                }
//																if (adaptiveGraphicsCameraObj != null && !adaptiveGraphicsCameraObj.activeSelf) {
                if (agCamera != null && !agCamera.enabled)
                {
                    if (agPost != null)
                    {
                        agPost.adaptiveGraphicsManager = this;
                    }
//																				adaptiveGraphicsCameraObj.SetActive (true);
                    agCamera.enabled = true;
                }
                if (agBillboardObj != null && !agBillboardObj.activeSelf)
                {
                    agBillboardObj.SetActive(true);
                }
            }
            if (!Application.isPlaying)
            {
                rtDownsampling = 0;
                avgDownsampling = 1f;
                cameraIsStatic = true;
                CheckRT();
                return;
            }
            // Camera moved?
            if (camMovDetectThreshold++ > 5)
            {
                Vector3 camRot = mainCamera.transform.rotation.eulerAngles;
                if (mainCamera.transform.position != oldCameraPos || camRot != oldCameraRot)
                {
                    cameraIsStatic = false;
                    oldCameraPos = mainCamera.transform.position;
                    oldCameraRot = camRot;
                    camMovDetectThreshold = 0;
                }
                else
                {
                    cameraIsStatic = true;
                }
            }

            // Compute fps
            frameCount++;
            if (Time.realtimeSinceStartup > nextPeriod)
            {
                fps = (int)(frameCount / FPS_UPDATE_RATE);
                frameCount = 0;
                nextPeriod += FPS_UPDATE_RATE;
                if (_method == DOWNSAMPLING_METHOD.Disabled)
                {
                    avgDownsampling = 1f;
                }
                else
                {
                    if (fps >= targetFPS)
                    {
                        avgDownsampling += Mathf.Min(_fpsChangeSpeedUp * (fps - targetFPS), _fpsChangeSpeedUp);
                    }
                    else
                    {
                        avgDownsampling -= Mathf.Min(_fpsChangeSpeedDown * (targetFPS - fps), _fpsChangeSpeedDown);
                    }
                    avgDownsampling = Mathf.Clamp(avgDownsampling, activeDownsampling, 1f);
                }

                // Additional adjustments
                if (_reducePixelLights)
                {
                    int newPixelLightCount = avgDownsampling < 1f ? 1 : oldPixelLightCount;
                    QualitySettings.pixelLightCount = newPixelLightCount;
                }
                if (_manageShadows)
                {
                    ManageShadows();
                }
                CheckRT();
            }

        }

        void OnPreRender()
        {

            // Check nice FPS
            if (Application.isPlaying)
            {
                if (avgFPSNice >= _niceFPS && _niceFPSEnabled)
                    return;
            }

            if (agCamera != null)
            {
                if (agCamera.enabled && _method == DOWNSAMPLING_METHOD.Disabled)
                {
                    agCamera.enabled = false;
                }
                else if (!agCamera.enabled && method != DOWNSAMPLING_METHOD.Disabled)
                {
                    agCamera.enabled = true;
                }
            }

            if (mainCamera.targetTexture == null && _method != DOWNSAMPLING_METHOD.Disabled)
            {
                CheckRT();
            }

            if (mainCamera.targetTexture != null)
                mainCamera.targetTexture.DiscardContents();

            switch (_compositingMethod)
            {
                case COMPOSITING_METHOD.SecondCameraBillboardOverlay:
                    if (adaptiveGraphicsBillboardTex != null && adaptiveGraphicsBillboardTex.texture != rt)
                    {
                        adaptiveGraphicsBillboardTex.texture = rt;
                    }
                    break;
                case COMPOSITING_METHOD.SecondCameraBillboardWorldSpace:
                    if (adaptiveGraphicsBillboardMat != null && adaptiveGraphicsBillboardMat.mainTexture != rt)
                    {
                        adaptiveGraphicsBillboardMat.mainTexture = rt;
                    }
                    break;
            }

        }

        void OnGUI()
        {

            if (Event.current.type != EventType.layout && Event.current.type != EventType.repaint)
                return;

            if (_compositingMethod == COMPOSITING_METHOD.Simple && _method != DOWNSAMPLING_METHOD.Disabled && rt != null && mainCamera.targetTexture != null)
            {
                RenderTexture.active = null;	// Android needs this
                GUI.depth = 2500;
                GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), rt, ScaleMode.StretchToFill, false);
            }

        }



        #endregion

        #region Camera setup stuff

        void UpdateSettings()
        {

            switch (_method)
            {
                case DOWNSAMPLING_METHOD.AdaptativeDownsampling:
                    Application.targetFrameRate = 300;
                    QualitySettings.vSyncCount = 0;
                    break;
                case DOWNSAMPLING_METHOD.HorizontalDownsampling:
                    Application.targetFrameRate = 300;
                    QualitySettings.vSyncCount = 0;
                    break;
                case DOWNSAMPLING_METHOD.QuadDownsampling:
                    Application.targetFrameRate = 300;
                    QualitySettings.vSyncCount = 0;
                    break;
                case DOWNSAMPLING_METHOD.Disabled:
                    Application.targetFrameRate = -1;
                    QualitySettings.vSyncCount = oldVSyncCount;
                    break;
            }
        }

        void CheckCamera()
        {
            mainCamera = GetComponent<Camera>();

            if (adaptiveGraphicsCameraObj == null)
            {
                adaptiveGraphicsCameraObj = FindGameObject(CAMERA_INSTANCED);
                agCamera = null;
            }
            if (agBillboardObj == null)
            {
                agBillboardObj = FindGameObject(BILLBOARD_INSTANCED);
            }

            if ((_compositingMethod == COMPOSITING_METHOD.Simple || _method == DOWNSAMPLING_METHOD.Disabled) && adaptiveGraphicsCameraObj != null)
            {
                DestroyAdaptiveGraphicsCamera();
            }

            if (_method == DOWNSAMPLING_METHOD.Disabled)
                return;

            if (_compositingMethod != COMPOSITING_METHOD.Simple)
            {
                if (adaptiveGraphicsCameraObj == null)
                {
                    adaptiveGraphicsCameraObj = new GameObject(CAMERA_INSTANCED);
                    agCamera = null;
                }
                if (agCamera == null)
                {
                    agCamera = adaptiveGraphicsCameraObj.GetComponent<Camera>();
                    if (agCamera == null)
                        agCamera = adaptiveGraphicsCameraObj.AddComponent<Camera>();
                }
                if (adaptiveGraphicsCameraObj.GetComponent<FlareLayer>() == null)
                    adaptiveGraphicsCameraObj.AddComponent<FlareLayer>();
                switch (_compositingMethod)
                {
                    case COMPOSITING_METHOD.SecondCameraBlit:
                        SetupSecondCameraBlitMode();
                        break;
                    case COMPOSITING_METHOD.SecondCameraBillboardOverlay:
                        SetupSecondCameraBillboardOverlayMode();
                        break;
                    case COMPOSITING_METHOD.SecondCameraBillboardWorldSpace:
                        SetupSecondCameraBillboardWorldSpaceMode();
                        break;
                }
            }
            isDirty = true;
        }

        GameObject FindGameObject(string name)
        {
            GameObject[] gos = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int k = 0; k < gos.Length; k++)
            {
                GameObject go = gos[k];
                if (go != null && go.name.Equals(name))
                {
                    return go;
                }
            }
            return null;
        }


        void SetupSecondCameraBlitMode()
        {
            if (mainCamera != null)
            {
                agCamera.CopyFrom(mainCamera);
                agCamera.depth--;
            }
            agCamera.renderingPath = RenderingPath.Forward;
            agCamera.transform.position = new Vector3(0, 0, 10000);
            agCamera.transform.rotation = Quaternion.Euler(0, 0, 0);
            agCamera.targetTexture = null;
            agCamera.clearFlags = _cameraClearFlags;
            agCamera.depthTextureMode = DepthTextureMode.None;
            agCamera.farClipPlane = 1f;
            agCamera.cullingMask = 0;
            agCamera.allowMSAA = false;
            agCamera.useOcclusionCulling = false;
            agPost = adaptiveGraphicsCameraObj.GetComponent<AdaptiveGraphicsPostFrameBehavior>() ?? adaptiveGraphicsCameraObj.gameObject.AddComponent<AdaptiveGraphicsPostFrameBehavior>();
            agPost.adaptiveGraphicsManager = this;

            GUILayer residualGUILayer = adaptiveGraphicsCameraObj.GetComponent<GUILayer>();
            if (residualGUILayer != null)
                DestroyImmediate(residualGUILayer);
        }

        void SetupSecondCameraBillboardOverlayMode()
        {
            if (mainCamera != null)
            {
                agCamera.CopyFrom(mainCamera);
                agCamera.depth--;
            }
            agCamera.renderingPath = RenderingPath.Forward;
            agCamera.transform.position = new Vector3(0, 0, 10000);
            agCamera.transform.rotation = Quaternion.Euler(0, 0, 0);
            agCamera.targetTexture = null;
            agCamera.clearFlags = _cameraClearFlags;
            agCamera.depthTextureMode = DepthTextureMode.None;
            agCamera.farClipPlane = 1f;
            agCamera.useOcclusionCulling = false;
            agCamera.cullingMask = 1 << BILLBOARD_LAYER;
            agCamera.allowMSAA = false;
            agPost = adaptiveGraphicsCameraObj.GetComponent<AdaptiveGraphicsPostFrameBehavior>();
            if (agPost != null)
            {
                DestroyImmediate(agPost);
                agPost = null;
            }
            if (adaptiveGraphicsCameraObj.GetComponent<GUILayer>() == null)
            {
                adaptiveGraphicsCameraObj.AddComponent<GUILayer>();
            }
            if (agBillboardObj == null)
            {
                agBillboardObj = new GameObject(BILLBOARD_INSTANCED);
                agBillboardObj.layer = BILLBOARD_LAYER;
            }
            agBillboardObj.transform.SetParent(agCamera.transform);
            agBillboardObj.transform.position = new Vector2(0.5f, 0.5f);
            adaptiveGraphicsBillboardTex = agBillboardObj.GetComponent<GUITexture>();
            if (adaptiveGraphicsBillboardTex == null)
                adaptiveGraphicsBillboardTex = agBillboardObj.AddComponent<GUITexture>();
            adaptiveGraphicsBillboardTex.pixelInset = new Rect(0, 0, 0, 0);
            adaptiveGraphicsBillboardTex.border = new RectOffset(0, 0, 0, 0);
            adaptiveGraphicsBillboardTex.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        void SetupSecondCameraBillboardWorldSpaceMode()
        {
            if (mainCamera != null)
            {
                agCamera.CopyFrom(mainCamera);
                agCamera.depth--;
            }
            agCamera.renderingPath = RenderingPath.Forward;
            agCamera.transform.position = new Vector3(0, 1000, 1000);
            agCamera.transform.rotation = Quaternion.Euler(0, 0, 0);
            agCamera.targetTexture = null;
            agCamera.clearFlags = _cameraClearFlags;
            agCamera.depthTextureMode = DepthTextureMode.None;
            agCamera.farClipPlane = 1f;
            agCamera.cullingMask = 1 << BILLBOARD_LAYER;
            agCamera.useOcclusionCulling = false;
            agCamera.orthographic = true;
            agCamera.orthographicSize = 0.5f;
            agCamera.allowMSAA = false;

            agPost = adaptiveGraphicsCameraObj.GetComponent<AdaptiveGraphicsPostFrameBehavior>();
            if (agPost != null)
            {
                DestroyImmediate(agPost);
                agPost = null;
            }
            GUILayer residualGUILayer = adaptiveGraphicsCameraObj.GetComponent<GUILayer>();
            if (residualGUILayer != null)
                DestroyImmediate(residualGUILayer);
            if (agBillboardObj == null)
            {
                agBillboardObj = Instantiate(Resources.Load<GameObject>("Prefabs/AdaptiveGraphicsBillboard"));
                agBillboardObj.name = BILLBOARD_INSTANCED;
                agBillboardObj.layer = BILLBOARD_LAYER;
            }
            agBillboardObj.transform.SetParent(agCamera.transform);
            agBillboardObj.transform.rotation = Quaternion.Euler(0, 0, 0);
            agBillboardObj.transform.localPosition = new Vector3(0, 0, 0.5f);
            agBillboardObj.transform.localScale = new Vector3(agCamera.aspect, 1f, 1f);
            adaptiveGraphicsBillboardMat = agBillboardObj.GetComponent<Renderer>().sharedMaterial;
        }


        #endregion

        #region Downsampling stuff

        float rtClamp(float d)
        {
            return Mathf.Clamp(d, 0.1f, 1f);
        }

        int getAntialiasLevel()
        {
            return (int)(Mathf.Pow(2, _antialias - 1));
        }

        RenderTexture FetchRT(int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.isPowerOfTwo = false;
            rt.hideFlags = HideFlags.DontSave;
            rt.filterMode = _filtering == FILTERING_MODE.Bilinear ? FilterMode.Bilinear : FilterMode.Point;
            rt.wrapMode = TextureWrapMode.Clamp;
            if (_antialias > 1)
                rt.antiAliasing = getAntialiasLevel();
            rt.Create();
            return rt;
        }

        RenderTexture PrepareAdaptativeRenderTexture()
        {
            int index = Mathf.RoundToInt(avgDownsampling * 10f);
            if (rtArray == null || rtArray.Length < index)
                return null;
            RenderTexture rt = rtArray[index - 1];
            float f = index * 0.1f;
            int width = Mathf.RoundToInt(screenWidth * f);
            int height = Mathf.RoundToInt(screenHeight * f);
            if (width <= 0 || (rt != null && rt.width == width && rt.height == height && rt.IsCreated()))
                return rt;
            CheckAndReleaseRT(rt);
            rt = FetchRT(width, height);
            rtArray[index - 1] = rt;
            return rt;
        }

        RenderTexture PrepareVerticalRenderTexture()
        {
            if (rtArray == null || rtArray.Length == 0)
                return null;
            int index = cameraIsStatic ? 0 : 1;
            RenderTexture rt = rtArray[index];
            int width = Mathf.RoundToInt(screenWidth * activeDownsampling);
            int height = screenHeight;
            if (width <= 0 || (rt != null && rt.width == width && rt.height == height && rt.IsCreated()))
                return rt;
            CheckAndReleaseRT(rt);
            rt = FetchRT(width, height);
            rtArray[index] = rt;
            return rt;
        }

        RenderTexture PrepareQuadRenderTexture()
        {
            if (rtArray == null || rtArray.Length == 0)
                return null;
            int index = cameraIsStatic ? 0 : 1;
            RenderTexture rt = rtArray[index];
            int width = Mathf.RoundToInt(screenWidth * activeDownsampling);
            int height = Mathf.RoundToInt(screenHeight * activeDownsampling);
            if (width <= 0 || (rt != null && rt.width == width && rt.height == height && rt.IsCreated()))
                return rt;
            CheckAndReleaseRT(rt);
            rt = FetchRT(width, height);
            rtArray[index] = rt;
            return rt;
        }

        void CheckAndReleaseRT(RenderTexture rt)
        {
            if (rt == null)
                return;
            if (mainCamera.targetTexture == rt)
            {
                RenderTexture.active = null;
                mainCamera.targetTexture = null;
                rt.Release();
            }
        }

        void CheckRT()
        {

            if (mainCamera == null)
                return;

            if (_niceFPSEnabled && avgFPSNice >= _niceFPS)
                return;

            RenderTexture rt = null;
            switch (_method)
            {
                case DOWNSAMPLING_METHOD.AdaptativeDownsampling:
                    if (mainCamera.targetTexture != null && Mathf.Abs(avgDownsampling - rtDownsampling) < 0.05f)
                        return;
                    rtDownsampling = avgDownsampling;
                    mainCamera.ResetAspect();
                    rt = PrepareAdaptativeRenderTexture();
                    break;
                case DOWNSAMPLING_METHOD.HorizontalDownsampling:
                    rt = PrepareVerticalRenderTexture();
                    if (rt != null)
                    {
                        rtDownsampling = activeDownsampling;
                        float aspectRatio = (float)rt.width / (rt.height * rtDownsampling);
                        if (aspectRatio != mainCamera.aspect)
                        {
                            mainCamera.aspect = aspectRatio;
                        }
                    }
                    break;
                case DOWNSAMPLING_METHOD.QuadDownsampling:
                    rt = PrepareQuadRenderTexture();
                    if (rt != null)
                    {
                        rtDownsampling = activeDownsampling;
                        float aspectRatio = (float)rt.width / rt.height;
                        if (aspectRatio != mainCamera.aspect)
                        {
                            mainCamera.aspect = aspectRatio;
                        }
                    }
                    break;
            }
            if (mainCamera.targetTexture != rt)
            {
                mainCamera.targetTexture = rt;
                UpdateUICanvases();
            }
        }

        void UpdateUICanvases()
        {
            // Make sure UI Canvas is not Screen Overlay or it won't work
            if (_compositingMethod != COMPOSITING_METHOD.Simple)
                return;
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            for (int k = 0; k < canvases.Length; k++)
            {
                Canvas canvas = canvases[k];
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = mainCamera;
                }
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == mainCamera)
                {
                    canvas.scaleFactor = rtDownsampling;
                }
            }
        }


        void RestoreUICanvases()
        {
            if (_compositingMethod != COMPOSITING_METHOD.Simple)
                return;
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            for (int k = 0; k < canvases.Length; k++)
            {
                Canvas canvas = canvases[k];
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == mainCamera)
                {
                    canvas.scaleFactor = 1f;
                }
            }
        }

        #endregion

        #region Quality settings stuff

        void ResetShadows()
        {
            if (lights == null)
                return;
            int lightsCount = lights.Count;
            for (int k = 0; k < lightsCount; k++)
            {
                Light light = lights[k];
                if (light == null)
                    continue;
                if (oldLightShadows.ContainsKey(light))
                {
                    LightShadows lightShadow = oldLightShadows[light];
                    light.shadows = lightShadow;
                }
            }
        }

        void ManageShadows()
        {
            if (lights == null || Time.time - lastLightCheckTime > 10f)
            {
                lastLightCheckTime = Time.time;

                Light[] newLights = FindObjectsOfType(typeof(Light)) as Light[];
                if (lights == null)
                {
                    lights = new List<Light>(newLights);
                }
                else
                {
                    for (int k = 0; k < newLights.Length; k++)
                    {
                        Light light = newLights[k];
                        if (light == null)
                            continue;
                        if (!lights.Contains(light))
                            lights.Add(light);
                    }
                }
            }
            // Annotate lights
            int lightsCount = lights.Count;
            for (int k = 0; k < lightsCount; k++)
            {
                Light light = lights[k];
                if (light == null)
                    continue;
                LightShadows oldLightShadow;
                if (oldLightShadows.ContainsKey(light))
                {
                    oldLightShadow = oldLightShadows[light];
                }
                else
                {
                    oldLightShadow = light.shadows;
                    oldLightShadows.Add(light, oldLightShadow);
                }
            }
            // Check for good shadows
            if (avgDownsampling >= 1f)
            {
                for (int k = 0; k < lightsCount; k++)
                {
                    Light light = lights[k];
                    if (light == null)
                        continue;
                    LightShadows oldLightShadow = oldLightShadows[light];
                    if (light.shadows != oldLightShadow)
                    {
                        light.shadows = oldLightShadow;
                    }
                }
            }
            else
            {
                // Reduce shadow quality
                // Look for soft shadows first
                bool pendingHardShadows = true;
                if (avgDownsampling > 0.5f)
                {
                    for (int k = 0; k < lightsCount; k++)
                    {
                        Light light = lights[k];
                        if (light == null)
                            continue;
                        if (light.shadows == LightShadows.Soft)
                        {
                            light.shadows = LightShadows.Hard;
                            pendingHardShadows = false;
                        }
                    }
                }
                if (pendingHardShadows)
                {
                    for (int k = 0; k < lightsCount; k++)
                    {
                        Light light = lights[k];
                        if (light == null)
                            continue;
                        if (light.shadows != LightShadows.None)
                        {
                            light.shadows = LightShadows.None;
                        }
                    }
                }
            }
        }


        #endregion

    }

}