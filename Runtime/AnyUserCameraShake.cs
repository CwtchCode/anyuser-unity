using UnityEngine;

namespace AnyUser
{
    /// <summary>
    /// AnyUser Behavioral Interceptor: Camera Shake Dampener.
    /// Intercepts camera trauma and clamps vibration to user's motion sensitivity preference.
    /// </summary>
    [AddComponentMenu("AnyUser/AnyUser Camera Shake")]
    public class AnyUserCameraShake : MonoBehaviour
    {
        [Header("Trauma Settings")]
        [Tooltip("Maximum camera positional offset at 1.0 trauma in units.")]
        [SerializeField] private Vector3 maxTranslation = new Vector3(0.5f, 0.5f, 0.2f);

        [Tooltip("Maximum camera rotational shake at 1.0 trauma in degrees.")]
        [SerializeField] private Vector3 maxRotation = new Vector3(3.0f, 3.0f, 2.0f);

        [Tooltip("Speed at which trauma dissipates per second.")]
        [SerializeField] private float traumaDecay = 1.5f;

        [Range(0f, 1f)]
        [SerializeField] private float trauma = 0f;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float noiseSeed;

        private void Start()
        {
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
            noiseSeed = Random.value * 100f;
        }

        /// <summary>
        /// Adds trauma to the camera shake accumulator (0.0 to 1.0).
        /// </summary>
        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        private void LateUpdate()
        {
            if (trauma > 0f)
            {
                trauma = Mathf.Max(0f, trauma - traumaDecay * Time.deltaTime);

                // Fetch real-time screen shake multiplier from AnyUser standard (0.0 = static safe)
                float shakeMultiplier = AnyUserParser.Instance != null ? AnyUserParser.Instance.ScreenShake : 1.0f;

                if (shakeMultiplier > 0.001f)
                {
                    float shake = trauma * trauma * shakeMultiplier;
                    float time = Time.time * 25f;

                    // Perlin noise calculation for smooth multi-axis shudder
                    float offsetX = (Mathf.PerlinNoise(noiseSeed, time) - 0.5f) * 2f * maxTranslation.x * shake;
                    float offsetY = (Mathf.PerlinNoise(noiseSeed + 1f, time) - 0.5f) * 2f * maxTranslation.y * shake;
                    float offsetZ = (Mathf.PerlinNoise(noiseSeed + 2f, time) - 0.5f) * 2f * maxTranslation.z * shake;

                    float rotX = (Mathf.PerlinNoise(noiseSeed + 3f, time) - 0.5f) * 2f * maxRotation.x * shake;
                    float rotY = (Mathf.PerlinNoise(noiseSeed + 4f, time) - 0.5f) * 2f * maxRotation.y * shake;
                    float rotZ = (Mathf.PerlinNoise(noiseSeed + 5f, time) - 0.5f) * 2f * maxRotation.z * shake;

                    transform.localPosition = initialPosition + new Vector3(offsetX, offsetY, offsetZ);
                    transform.localRotation = initialRotation * Quaternion.Euler(rotX, rotY, rotZ);
                }
                else
                {
                    // Locked static frame for vestibular and motion sickness protection
                    transform.localPosition = initialPosition;
                    transform.localRotation = initialRotation;
                }
            }
            else
            {
                transform.localPosition = initialPosition;
                transform.localRotation = initialRotation;
            }
        }
    }
}
