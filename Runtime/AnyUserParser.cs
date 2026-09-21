using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnyUser
{
    /// <summary>
    /// AnyUser Accessibility Standard - Unity Reference Parser and Ingestion Engine.
    /// Ingests, validates, and exposes .anyuser profiles before Frame 1 with Zero-Code Drop-In Automation.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class AnyUserParser : MonoBehaviour
    {
        public static AnyUserParser Instance { get; private set; }

        public event Action<AnyUserProfile> OnProfileLoaded;
        public event Action<AnyUserProfile> OnProfileChanged;

        [Header("Runtime State")]
        [SerializeField] private AnyUserProfile currentProfile = new AnyUserProfile();
        public AnyUserProfile CurrentProfile => currentProfile;

        [Header("Configuration")]
        [SerializeField] private bool loadOnAwake = true;
        [SerializeField] private string customProfileFileName = "profile.anyuser";

        private Canvas overlayCanvas;
        private Image overlayImage;

        /// <summary>
        /// True Zero-Touch Initialization: Automatically bootstraps AnyUser before any scene loads
        /// even if the developer never added a prefab or GameObject to the scene hierarchy.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrapOnLoad()
        {
            if (Instance == null)
            {
                var existing = UnityEngine.Object.FindObjectOfType<AnyUserParser>();
                if (existing == null)
                {
                    GameObject go = new GameObject("AnyUser_RuntimeEngine");
                    Instance = go.AddComponent<AnyUserParser>();
                    DontDestroyOnLoad(go);
                    Instance.AutoDiscoverAndLoadProfile();
                }
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                SceneManager.sceneLoaded += OnSceneLoaded;

                if (loadOnAwake)
                {
                    AutoDiscoverAndLoadProfile();
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyDropInInterceptors();
        }

        /// <summary>
        /// Attempts to discover an .anyuser profile across common developer/user locations.
        /// </summary>
        public void AutoDiscoverAndLoadProfile()
        {
            string[] searchPaths = new string[]
            {
                Path.Combine(Application.persistentDataPath, customProfileFileName),
                Path.Combine(Application.streamingAssetsPath, customProfileFileName),
                Path.Combine(Application.dataPath, customProfileFileName),
                Path.Combine(Directory.GetCurrentDirectory(), customProfileFileName),
                Path.Combine(Application.dataPath, "..", customProfileFileName)
            };

            bool loaded = false;
            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    Debug.Log($"[AnyUser] Discovered profile at: {path}");
                    loaded = LoadProfileFromFile(path);
                    if (loaded) break;
                }
            }

            if (!loaded)
            {
                // Fall back to default Draft-07 baseline
                currentProfile = GetDefaultProfile();
                OnProfileLoaded?.Invoke(currentProfile);
                OnProfileChanged?.Invoke(currentProfile);
                Debug.Log("[AnyUser] Operating on canonical Draft-07 accessibility baseline.");
                ApplyDropInInterceptors();
            }
        }

        /// <summary>
        /// Loads and parses an .anyuser profile from a file on disk.
        /// </summary>
        public bool LoadProfileFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[AnyUser] Profile file not found at: {filePath}");
                    return false;
                }

                string json = File.ReadAllText(filePath);
                return LoadProfileFromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnyUser] Failed to read profile from {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parses an .anyuser JSON string and populates the active profile.
        /// </summary>
        public bool LoadProfileFromJson(string jsonString)
        {
            try
            {
                var profile = JsonUtility.FromJson<AnyUserProfile>(jsonString);
                if (profile != null)
                {
                    currentProfile = profile;
                    OnProfileLoaded?.Invoke(currentProfile);
                    OnProfileChanged?.Invoke(currentProfile);
                    Debug.Log($"[AnyUser] Profile loaded successfully. UI Scale: {UiScale:F2} | Screen Shake: {ScreenShake:F2} | Aim Assist: {AimAssistStrength:F2}");
                    ApplyDropInInterceptors();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnyUser] Failed to parse profile JSON: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Applies all automated drop-in interceptors without requiring changes to game code.
        /// </summary>
        public void ApplyDropInInterceptors()
        {
            ApplyGlobalUIScaling();
            ApplyColorblindOverlay();
            ApplyAudioAccessibility();
        }

        /// <summary>
        /// Automatically adjusts CanvasScaler scale factors across active UI scenes.
        /// </summary>
        private void ApplyGlobalUIScaling()
        {
            float scale = UiScale;
            if (scale <= 0f) return;

            var scalers = UnityEngine.Object.FindObjectsOfType<CanvasScaler>();
            foreach (var scaler in scalers)
            {
                scaler.scaleFactor = scale;
            }

            if (scalers.Length > 0)
            {
                Debug.Log($"[AnyUser] Drop-In Interceptor: Applied UI scale {scale:F2} across {scalers.Length} CanvasScaler(s).");
            }
        }

        /// <summary>
        /// Automatically spawns and manages a top-layer screen-space overlay with colorblind compensation.
        /// </summary>
        private void ApplyColorblindOverlay()
        {
            string filter = ColorblindFilter.ToLowerInvariant();
            int filterMode = 0;
            switch (filter)
            {
                case "protanopia": filterMode = 1; break;
                case "deuteranopia": filterMode = 2; break;
                case "tritanopia": filterMode = 3; break;
                case "achromatopsia":
                case "monochromacy":
                case "greyscale":
                case "grayscale": filterMode = 4; break;
            }

            if (filterMode > 0)
            {
                if (overlayCanvas == null)
                {
                    GameObject canvasObj = new GameObject("AnyUser_ColorblindOverlayCanvas");
                    overlayCanvas = canvasObj.AddComponent<Canvas>();
                    overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    overlayCanvas.sortingOrder = 32767; // Render in front of everything

                    canvasObj.AddComponent<CanvasScaler>();
                    var raycaster = canvasObj.AddComponent<GraphicRaycaster>();
                    raycaster.enabled = false; // Do not block UI clicks

                    DontDestroyOnLoad(canvasObj);

                    GameObject imgObj = new GameObject("ColorblindImage");
                    imgObj.transform.SetParent(canvasObj.transform, false);
                    overlayImage = imgObj.AddComponent<Image>();
                    overlayImage.raycastTarget = false;

                    RectTransform rect = imgObj.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    Shader shader = Shader.Find("AnyUser/ColorblindCompensation");
                    if (shader != null)
                    {
                        Material mat = new Material(shader);
                        overlayImage.material = mat;
                    }
                }

                if (overlayImage != null && overlayImage.material != null)
                {
                    overlayImage.material.SetInt("_FilterMode", filterMode);
                }

                if (overlayCanvas != null)
                {
                    overlayCanvas.gameObject.SetActive(true);
                }
                Debug.Log($"[AnyUser] Drop-In Interceptor: Fullscreen Colorblind compensation active (Filter: {filter} [{filterMode}]).");
            }
            else if (overlayCanvas != null)
            {
                overlayCanvas.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Automatically adjusts global audio listener settings for mono downmixing and tinnitus high-cut.
        /// </summary>
        private void ApplyAudioAccessibility()
        {
            try
            {
                if (currentProfile?.audio?.mono_audio == true)
                {
                    UnityEngine.AudioSettings.speakerMode = AudioSpeakerMode.Mono;
                    Debug.Log("[AnyUser] Drop-In Interceptor: Audio speakerMode set to Mono.");
                }

                if (TinnitusFrequencyCut)
                {
                    var listener = UnityEngine.Object.FindObjectOfType<AudioListener>();
                    if (listener != null)
                    {
                        var filter = listener.GetComponent<AudioLowPassFilter>();
                        if (filter == null)
                        {
                            filter = listener.gameObject.AddComponent<AudioLowPassFilter>();
                        }
                        filter.cutoffFrequency = 4000f;
                        filter.enabled = true;
                        Debug.Log("[AnyUser] Drop-In Interceptor: AudioLowPassFilter applied to AudioListener (4000Hz cut).");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AnyUser] Audio interceptor notice: {ex.Message}");
            }
        }

        public static AnyUserProfile GetDefaultProfile()
        {
            return new AnyUserProfile
            {
                version = "1.0",
                vision = new VisionSettings
                {
                    colorblind_filter = "none",
                    high_contrast_mode = false,
                    ui_scale = 1.0f,
                    font_preference = "default",
                    screen_shake = 1.0f,
                    flashing_effects = true,
                    subtitles = new SubtitleSettings
                    {
                        enabled = false,
                        size = "medium",
                        speaker_names = true,
                        background_opacity = 0.75f
                    }
                },
                motor = new MotorSettings
                {
                    toggle_instead_of_hold = false,
                    qte_auto_complete = false,
                    input_repeat_delay_ms = 0,
                    aim_assist_strength = 0.0f
                },
                audio = new AudioSettings
                {
                    dialogue_boost = false,
                    mono_audio = false,
                    background_music_ducking = 0.0f,
                    tinnitus_frequency_cut = false
                },
                cognitive = new CognitiveSettings
                {
                    reading_level_simplified = false,
                    disable_time_limits = false,
                    tutorial_reminders = "medium"
                }
            };
        }

        // Typed Quick Accessors (Vision)
        public float UiScale => currentProfile?.vision?.ui_scale ?? 1.0f;
        public float ScreenShake => currentProfile?.vision?.screen_shake ?? 1.0f;
        public string ColorblindFilter => currentProfile?.vision?.colorblind_filter ?? "none";
        public bool HighContrastMode => currentProfile?.vision?.high_contrast_mode ?? false;
        public string FontPreference => currentProfile?.vision?.font_preference ?? "default";
        public bool FlashingEffects => currentProfile?.vision?.flashing_effects ?? true;
        public bool SubtitlesEnabled => currentProfile?.vision?.subtitles?.enabled ?? false;
        public string SubtitleSize => currentProfile?.vision?.subtitles?.size ?? "medium";
        public bool SubtitleSpeakerNames => currentProfile?.vision?.subtitles?.speaker_names ?? true;
        public float SubtitleBackgroundOpacity => currentProfile?.vision?.subtitles?.background_opacity ?? 0.75f;

        // Typed Quick Accessors (Motor)
        public bool ToggleInsteadOfHold => currentProfile?.motor?.toggle_instead_of_hold ?? false;
        public bool QteAutoComplete => currentProfile?.motor?.qte_auto_complete ?? false;
        public int InputRepeatDelayMs => currentProfile?.motor?.input_repeat_delay_ms ?? 0;
        public float AimAssistStrength => currentProfile?.motor?.aim_assist_strength ?? 0.0f;

        // Typed Quick Accessors (Audio)
        public bool DialogueBoost => currentProfile?.audio?.dialogue_boost ?? false;
        public bool MonoAudio => currentProfile?.audio?.mono_audio ?? false;
        public float BackgroundMusicDucking => currentProfile?.audio?.background_music_ducking ?? 0.0f;
        public bool TinnitusFrequencyCut => currentProfile?.audio?.tinnitus_frequency_cut ?? false;

        // Typed Quick Accessors (Cognitive)
        public bool ReadingLevelSimplified => currentProfile?.cognitive?.reading_level_simplified ?? false;
        public bool DisableTimeLimits => currentProfile?.cognitive?.disable_time_limits ?? false;
        public string TutorialReminders => currentProfile?.cognitive?.tutorial_reminders ?? "medium";
    }

    [Serializable]
    public class AnyUserProfile
    {
        public string version = "1.0";
        public VisionSettings vision = new VisionSettings();
        public MotorSettings motor = new MotorSettings();
        public AudioSettings audio = new AudioSettings();
        public CognitiveSettings cognitive = new CognitiveSettings();
    }

    [Serializable]
    public class VisionSettings
    {
        public string colorblind_filter = "none";
        public bool high_contrast_mode = false;
        public float ui_scale = 1.0f;
        public string font_preference = "default";
        public float screen_shake = 1.0f;
        public bool flashing_effects = true;
        public SubtitleSettings subtitles = new SubtitleSettings();
    }

    [Serializable]
    public class SubtitleSettings
    {
        public bool enabled = false;
        public string size = "medium";
        public bool speaker_names = true;
        public float background_opacity = 0.75f;
    }

    [Serializable]
    public class MotorSettings
    {
        public bool toggle_instead_of_hold = false;
        public bool qte_auto_complete = false;
        public int input_repeat_delay_ms = 0;
        public float aim_assist_strength = 0.0f;
    }

    [Serializable]
    public class AudioSettings
    {
        public bool dialogue_boost = false;
        public bool mono_audio = false;
        public float background_music_ducking = 0.0f;
        public bool tinnitus_frequency_cut = false;
    }

    [Serializable]
    public class CognitiveSettings
    {
        public bool reading_level_simplified = false;
        public bool disable_time_limits = false;
        public string tutorial_reminders = "medium";
    }
}
