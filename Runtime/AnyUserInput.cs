using System.Collections.Generic;
using UnityEngine;

namespace AnyUser
{
    /// <summary>
    /// AnyUser Behavioral Interceptor: Input Dexterity & Motor Mitigations.
    /// Provides hold-vs-toggle latching, tremor debounce absorption, and aim assist magnetism.
    /// </summary>
    public static class AnyUserInput
    {
        private static readonly Dictionary<string, bool> toggleStates = new Dictionary<string, bool>();
        private static readonly Dictionary<string, float> lastActionTimes = new Dictionary<string, float>();

        /// <summary>
        /// Evaluates whether an action is active, automatically swapping hold requirements for toggles.
        /// </summary>
        public static bool IsActionActive(string actionName, bool isPhysicalHold, bool isJustPressed)
        {
            bool useToggle = AnyUserParser.Instance != null && AnyUserParser.Instance.ToggleInsteadOfHold;

            if (useToggle)
            {
                if (isJustPressed)
                {
                    if (!toggleStates.ContainsKey(actionName))
                        toggleStates[actionName] = false;

                    toggleStates[actionName] = !toggleStates[actionName];
                }

                return toggleStates.TryGetValue(actionName, out bool active) && active;
            }

            return isPhysicalHold;
        }

        /// <summary>
        /// Filters accidental duplicate keypresses caused by motor tremors or keystroke bounce.
        /// </summary>
        public static bool IsActionJustPressedDebounced(string actionName, bool isJustPressed)
        {
            if (!isJustPressed) return false;

            int delayMs = AnyUserParser.Instance != null ? AnyUserParser.Instance.InputRepeatDelayMs : 0;
            if (delayMs > 0)
            {
                float now = Time.unscaledTime;
                if (lastActionTimes.TryGetValue(actionName, out float lastTime))
                {
                    if ((now - lastTime) * 1000f < delayMs)
                    {
                        // Duplicate tremor keystroke absorbed
                        return false;
                    }
                }

                lastActionTimes[actionName] = now;
            }

            return true;
        }

        /// <summary>
        /// Resets all latched toggle states (useful when restarting a level or opening pause menu).
        /// </summary>
        public static void ResetToggles()
        {
            toggleStates.Clear();
        }

        /// <summary>
        /// Applies aim assist magnetism to bend a projectile or raycast vector toward nearest enemies.
        /// </summary>
        public static Vector3 CalculateAimAssist(
            Vector3 origin,
            Vector3 rawAimDirection,
            Vector3[] candidateTargets,
            float maxDistance = 20f,
            float maxAngleDeg = 35f)
        {
            float assistStrength = AnyUserParser.Instance != null ? AnyUserParser.Instance.AimAssistStrength : 0f;
            if (assistStrength <= 0f || candidateTargets == null || candidateTargets.Length == 0)
            {
                return rawAimDirection.normalized;
            }

            Vector3 normalizedAim = rawAimDirection.normalized;
            float bestDist = maxDistance;
            Vector3 bestTargetDir = normalizedAim;
            bool foundTarget = false;

            for (int i = 0; i < candidateTargets.Length; i++)
            {
                Vector3 toTarget = candidateTargets[i] - origin;
                float dist = toTarget.magnitude;

                if (dist < bestDist)
                {
                    Vector3 targetDir = toTarget.normalized;
                    float angle = Vector3.Angle(normalizedAim, targetDir);

                    if (angle <= maxAngleDeg)
                    {
                        bestDist = dist;
                        bestTargetDir = targetDir;
                        foundTarget = true;
                    }
                }
            }

            if (foundTarget)
            {
                // Smoothly blend aim towards the closest target based on aim_assist_strength
                float blendFactor = Mathf.Clamp01(assistStrength) * 0.75f;
                return Vector3.Slerp(normalizedAim, bestTargetDir, blendFactor);
            }

            return normalizedAim;
        }
    }
}
