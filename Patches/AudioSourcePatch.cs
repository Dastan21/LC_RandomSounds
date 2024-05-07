using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace RandomSounds.Patches
{
    [HarmonyPatch(typeof(AudioSource))]
    internal class AudioSourcePatch
    {
        [HarmonyPatch(nameof(AudioSource.Play), [])]
        [HarmonyPrefix]
        public static void Play_Patch(AudioSource __instance)
        {
            __instance.clip = ReplaceClipWithNew(__instance.clip);
        }

        [HarmonyPatch(nameof(AudioSource.Play), [typeof(ulong)])]
        [HarmonyPrefix]
        public static void Play_UlongPatch(AudioSource __instance)
        {
            __instance.clip = ReplaceClipWithNew(__instance.clip);
        }

        [HarmonyPatch(nameof(AudioSource.Play), [typeof(double)])]
        [HarmonyPrefix]
        public static void Play_DoublePatch(AudioSource __instance)
        {
            __instance.clip = ReplaceClipWithNew(__instance.clip);
        }

        [HarmonyPatch(nameof(AudioSource.PlayDelayed), [typeof(float)])]
        [HarmonyPrefix]
        public static void PlayDelayed_Patch(AudioSource __instance)
        {
            __instance.clip = ReplaceClipWithNew(__instance.clip);
        }

        [HarmonyPatch(nameof(AudioSource.PlayOneShotHelper), [typeof(AudioSource), typeof(AudioClip), typeof(float)])]
        [HarmonyPrefix]
        public static void PlayOneShotHelper_Patch(AudioSource source, ref AudioClip clip, float volumeScale)
        {
            clip = ReplaceClipWithNew(clip);
        }

        private static AudioClip ReplaceClipWithNew(AudioClip original)
        {
            if (original == null) return null;

            string clipName = original.GetName();

            RandomSounds.Instance.logger.LogInfo($"ReplaceClipWithNew: {clipName}");
            if (!RandomSounds.ReplacedClips.ContainsKey(clipName)) return original;

            ClipWeight[] clips = [.. RandomSounds.ReplacedClips[clipName]];
            int totalWeight = clips.Aggregate(0, (t, sw) => t + sw.weight);

            AudioClip clip = null;
            int rng = RandomSounds.random.Next(0, totalWeight);
            RandomSounds.SeedOffset++;
            for (int i = 0; i < clips.Length + 1; i++)
            {
                clip = clips[i].clip;
                if (rng < clips[i].weight) break;
                rng -= clips[i].weight;
            }
            if (clip == null) return original;

            RandomSounds.Instance.logger.LogInfo($"Playing custom sound {clip.GetName()} instead of {clipName}");
            return clip;
        }
    }
}
