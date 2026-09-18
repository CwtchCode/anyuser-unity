using System;
using System.IO;
using UnityEngine;

namespace AnyUser
{
    /// <summary>
    /// AnyUser Accessibility Standard - Unity Reference Parser and Ingestion Engine.
    /// Ingests, validates, and exposes .anyuser profiles before Frame 1.
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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

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

        /// <summary>
        /// Attempts to discover an .anyuser profile in persistentDataPath or StreamingAssets.
        /// </summary>
        public void AutoDiscoverAndLoadProfile()
        {
            string persistentPath = Path.Combine(Application.persistentDataPath, customProfileFileName);
            string streamingPath = Path.Combine(Application.streamingAssetsPath, customProfileFileName);

            if (File.Exists(persistentPath))
            {
                LoadProfileFromFile(persistentPath);
            }
            else if (File.Exists(streamingPath))
            {
                LoadProfileFromFile(streamingPath);
            }
            else
            {
                // Fall back to default Draft-07 baseline
                currentProfile = GetDefaultProfile();
                OnProfileLoaded?.Invoke(currentProfile);
                OnProfileChanged?.Invoke(currentProfile);
                Debug.Log("[AnyUser] Initialized with canonical Draft-07 accessibility baseline.");
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
                    Debug.Log($"[AnyUser] Profile loaded successfully. UI Scale: {UiScale} | Screen Shake: {ScreenShake}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnyUser] Failed to parse profile JSON: {ex.Message}");
            }
            return false;
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

        // Typed Quick Accessors
        public float UiScale => currentProfile?.vision?.ui_scale ?? 1.0f;
        public float ScreenShake => currentProfile?.vision?.screen_shake ?? 1.0f;
        public string ColorblindFilter => currentProfile?.vision?.colorblind_filter ?? "none";
        public bool HighContrastMode => currentProfile?.vision?.high_contrast_mode ?? false;
        public bool ToggleInsteadOfHold => currentProfile?.motor?.toggle_instead_of_hold ?? false;
        public int InputRepeatDelayMs => currentProfile?.motor?.input_repeat_delay_ms ?? 0;
        public float AimAssistStrength => currentProfile?.motor?.aim_assist_strength ?? 0.0f;
        public bool TinnitusFrequencyCut => currentProfile?.audio?.tinnitus_frequency_cut ?? false;
        public bool DialogueBoost => currentProfile?.audio?.dialogue_boost ?? false;
        public bool ReadingLevelSimplified => currentProfile?.cognitive?.reading_level_simplified ?? false;
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
