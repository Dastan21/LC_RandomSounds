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
    public struct ClipWeight(AudioClip clip, int weight)
    {
        public AudioClip clip = clip;
        public int weight = weight;
    }
}
