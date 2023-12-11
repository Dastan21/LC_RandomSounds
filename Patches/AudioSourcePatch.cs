using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace RandomSounds.Patches
{
    [HarmonyPatch(typeof(AudioSource))]
    internal class AudioSourcePatch
    {
        [HarmonyPatch(nameof(AudioSource.PlayOneShotHelper), new[] { typeof(AudioSource), typeof(AudioClip), typeof(float) })]
        [HarmonyPrefix]
        public static void PlayOneShotHelper_Patch(AudioSource source, ref AudioClip clip, float volumeScale)
        {
            clip = ReplaceClipWithNew(clip);
        }

        private static AudioClip ReplaceClipWithNew(AudioClip original)
        {
            if (original == null) return null;

            string clipName = original.GetName();

            if (!RandomSounds.ReplacedClips.ContainsKey(clipName)) return original;

            AudioClip[] clips = RandomSounds.ReplacedClips[clipName].ToArray();
            int index = RandomSounds.random.Next(0, clips.Length + 1);
            if (index == clips.Length) return original;

            RandomSounds.Instance.logger.LogInfo($"Playing custom sound {clips[index].GetName()} instead of {clipName}");
            return clips[index];
        }
    }
}
