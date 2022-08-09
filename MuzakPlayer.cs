using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxul.Utilities;

namespace Muzak
{
    public class MuzakPlayer : MonoBehaviour
    {
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
                source.clip = channel.Clip;
                source.playOnAwake = false;
                sourceChannelMapping[channel] = source;
            }
            var timer = 0f;
            foreach (var sourceChannel in sourceChannelMapping)
            {
                foreach (var sequence in sourceChannel.Key.Sequences)
                {
                    sourceChannel.Value.PlayScheduled(AudioSettings.dspTime + sequence.StartTime);
                }
            }
            while (timer < track.Duration)
            {
                foreach (var sourceChannel in sourceChannelMapping)
                {
                    var channel = sourceChannel.Key;
                    var sequence = channel.GetSequenceAtTime(timer);
                    var sequenceTime = timer - (float)sequence.StartTime;
                    var source = sourceChannel.Value;
                    source.volume = channel.Volume * sequence.VolumeCurve.Evaluate(sequenceTime / sequence.Duration);
                }
                yield return null;
                timer += Time.deltaTime;
            }
            foreach (var sourceChannel in sourceChannelMapping)
            {
                sourceChannel.Value.gameObject.SafeDestroy();
            }
        }
    }
}