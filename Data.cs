using System;
using UnityEngine;

namespace RandomSounds
{
    [Serializable]
    public struct SoundWeight
    {
        public string sound;
        public int weight;
    }

    [Serializable]
    public struct ClipWeight
    {
        public AudioClip clip;
        public int weight;

        public ClipWeight(AudioClip clip, int weight)
        {
            this.clip = clip;
            this.weight = weight;
        }
    }
}
