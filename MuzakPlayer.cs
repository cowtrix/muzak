using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Muzak
{
    public class MuzakPlayer : MonoBehaviour
    {
        public enum ePlayState
        {
            Stopped,
            Stopping,
            Paused,
            Playing,
        }

        public ePlayState PlayState { get; private set; }

        public MuzakTrack Track;
        [Range(0, 1)]
        public float Strength = 1;

        public float FadeInTime = 1;
        public AnimationCurve FadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float FadeOutTime = 1;
        public AnimationCurve FadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private void Start()
        {
            StartCoroutine(PlayTrackAsync(Track));
        }

        [ContextMenu("Play")]
        public void Play()
        {
            PlayState = ePlayState.Playing;
        }

        public void Pause()
        {
            PlayState = ePlayState.Paused;
        }

        public void Stop()
        {
            PlayState = ePlayState.Stopping;
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

            while (PlayState != ePlayState.Playing)
            {
                yield return null;
            }
            var loopStartTime = AudioSettings.dspTime;
            var startTime = loopStartTime;
            var playingness = 0f;

            do
            {
                const double lookAhead = .1;
                foreach (var sourceChannel in sourceChannelMapping)
                {
                    foreach (var sequence in sourceChannel.Key.Sequences)
                    {
                        var roll = Random.value;
                        if (roll > sequence.Probability)
                        {
                            continue;
                        }
                        var nextPlayTime = loopStartTime + sequence.StartTime;
                        sourceChannel.Value.time = (float)sequence.Offset;
                        sourceChannel.Value.PlayScheduled(nextPlayTime);
                        sourceChannel.Value.SetScheduledEndTime(nextPlayTime + sequence.Duration);
                    }
                }
                while (PlayState != ePlayState.Stopped && AudioSettings.dspTime < (loopStartTime + Track.Duration) - lookAhead)
                {
                    var t = AudioSettings.dspTime - startTime;
                    var loopT = AudioSettings.dspTime - loopStartTime;

                    switch (PlayState)
                    {
                        case ePlayState.Playing:
                            playingness = Mathf.Clamp(playingness + Time.deltaTime, 0, FadeInTime);
                            if (playingness < FadeInTime)
                            {
                                Strength = FadeInCurve.Evaluate(playingness / FadeInTime);
                            }
                            else
                            {
                                Strength = 1;
                            }
                            break;
                        case ePlayState.Stopping:
                            playingness = Mathf.Clamp(playingness - Time.deltaTime, 0, FadeOutTime);
                            if (playingness > 0)
                            {
                                Strength = FadeOutCurve.Evaluate(playingness / FadeInTime);
                            }
                            else
                            {
                                Strength = 0;
                            }
                            break;
                    }


                    foreach (var sourceChannel in sourceChannelMapping)
                    {
                        // Adjust volume
                        var channel = sourceChannel.Key;
                        var sequence = channel.GetSequenceAtTime(loopT);
                        if (sequence == null)
                        {
                            continue;
                        }
                        var sequenceTime = loopT - (float)sequence.StartTime;
                        var source = sourceChannel.Value;
                        source.volume = channel.Volume *
                            sequence.StrengthCurve.Evaluate(Strength) *
                            sequence.VolumeCurve.Evaluate((float)sequenceTime / (float)sequence.Duration);
                    }
                    yield return null;
                }
                loopStartTime = AudioSettings.dspTime + lookAhead;
            }
            while (PlayState != ePlayState.Stopped && track.Loop);
            foreach (var sourceChannel in sourceChannelMapping)
            {
                if (sourceChannel.Value)
                {
                    Destroy(sourceChannel.Value.gameObject);
                }
            }
        }
    }
}