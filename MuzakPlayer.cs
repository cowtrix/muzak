using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxul.Utilities;

namespace Muzak
{
    public class MuzakPlayer : MonoBehaviour
    {
        public MuzakTrack Track;
        [Range(0, 1)]
        public float Strength = 1;

        private void Start()
        {
            if (Track)
            {
                PlayTrack(Track);
            }
        }

        public void PlayTrack(MuzakTrack track)
        {
            StartCoroutine(PlayTrackAsync(track));
        }

        IEnumerator PlayTrackAsync(MuzakTrack track)
        {
            var sourceChannelMapping = new Dictionary<MuzakChannel, AudioSource>();
            foreach (var channel in track.Channels)
            {
                var source = new GameObject($"MuzakTrackPlayer_{channel.Clip.name}")
                    .AddComponent<AudioSource>();
                source.transform.SetParent(transform);
                source.clip = channel.Clip;
                source.playOnAwake = false;
                sourceChannelMapping[channel] = source;
            }

            var loopStartTime = AudioSettings.dspTime;
            do
            {
                const double lookAhead = .1;
                foreach (var sourceChannel in sourceChannelMapping)
                {
                    foreach (var sequence in sourceChannel.Key.Sequences)
                    {
                        var nextPlayTime = loopStartTime + sequence.StartTime;
                        sourceChannel.Value.time = (float)sequence.Offset;
                        sourceChannel.Value.PlayScheduled(nextPlayTime);
                        sourceChannel.Value.SetScheduledEndTime(nextPlayTime + sequence.Duration);
                    }
                }
                while (AudioSettings.dspTime < (loopStartTime + Track.Duration) - lookAhead)
                {
                    var t = AudioSettings.dspTime - loopStartTime;
                    foreach (var sourceChannel in sourceChannelMapping)
                    {
                        // Adjust volume
                        var channel = sourceChannel.Key;
                        var sequence = channel.GetSequenceAtTime(t);
                        if(sequence == null)
                        {
                            continue;
                        }
                        var sequenceTime = t - (float)sequence.StartTime;
                        var source = sourceChannel.Value;
                        source.volume = channel.Volume * 
                            sequence.StrengthCurve.Evaluate(Strength) *
                            sequence.VolumeCurve.Evaluate((float)sequenceTime / (float)sequence.Duration);
                    }
                    yield return null;
                }
                loopStartTime = AudioSettings.dspTime + lookAhead;
            }
            while (track.Loop);
            foreach (var sourceChannel in sourceChannelMapping)
            {
                sourceChannel.Value.gameObject.SafeDestroy();
            }
        }
    }
}