using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Muzak
{
    [Serializable]
    public class MuzakChannel
    {
        [Range(0, 1)]
        public float Volume;
        public AudioClip Clip;
        public List<MuzakSequence> Sequences = new List<MuzakSequence>();

        public MuzakSequence GetSequenceAtTime(float time) => Sequences.OrderBy(s => s.StartTime).FirstOrDefault(s => s.StartTime <= time);
    }


    [Serializable]
    public class MuzakSequence
    {
        [Range(0, 1)]
        public float StartThreshold;
        [Range(0, 1)]
        public float EndThreshold;
        public float Duration;
        public AnimationCurve VolumeCurve;
        public double StartTime;
        public float Offset;
    }

    [CreateAssetMenu(menuName = "Muzak/Track")]
    public class MuzakTrack : ScriptableObject
    {
        public float Strength { get; set; } = 1;

        public AnimationCurve StrengthCurve;
        public AudioMixer Mixer;
        public List<MuzakChannel> Channels = new List<MuzakChannel>();
        public float Duration;
    }
}