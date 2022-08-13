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
        public double CurrentLoopTime { get; set; }

        public MuzakTrack Track;
        [Range(0, 1)]
        public float Strength = 1;

        public float FadeInTime = 1;
        public AnimationCurve FadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float FadeOutTime = 1;
        public AnimationCurve FadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine m_currentCoroutine;

        [ContextMenu("Play")]
        public void Play()
        {
            PlayState = ePlayState.Playing;
            if (m_currentCoroutine == null)
            {
                m_currentCoroutine = StartCoroutine(PlayTrackAsync(Track));
            }
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
            var sourceChannelMapping = new List<(MuzakChannel, MuzakSequence, AudioSource)>();
            foreach (var channel in track.Channels)
            {
                for (int i = 0; i < channel.Sequences.Count; i++)
                {
                    var sequence = channel.Sequences[i];
                    var source = new GameObject($"MuzakTrackPlayer_{channel.Clip.name}_{i}")
                    .AddComponent<AudioSource>();
                    source.transform.SetParent(transform);
                    source.clip = channel.Clip;
                    source.playOnAwake = false;
                    sourceChannelMapping.Add((channel, sequence, source));
                }
            }

            while (PlayState != ePlayState.Playing)
            {
                yield return null;
            }
            var loopStartTime = AudioSettings.dspTime;
            var playingness = 0f;

            do
            {
                const double lookAhead = .0;
                foreach (var sequenceSource in sourceChannelMapping)
                {
                    var channel = sequenceSource.Item1;
                    var sequence = sequenceSource.Item2;
                    var source = sequenceSource.Item3;

                    var roll = Random.value;
                    if (roll > sequence.Probability)
                    {
                        continue;
                    }
                    var nextPlayTime = loopStartTime + sequence.StartTime;
                    source.time = (float)sequence.Offset;
                    source.PlayScheduled(nextPlayTime);
                    source.SetScheduledEndTime(nextPlayTime + sequence.Duration);
                }
                while (PlayState != ePlayState.Stopped && AudioSettings.dspTime < (loopStartTime + Track.Duration) - lookAhead)
                {
                    var loopT = AudioSettings.dspTime - loopStartTime;
                    CurrentLoopTime = loopT;
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
                                PlayState = ePlayState.Stopped;
                            }
                            break;
                    }


                    foreach (var sequenceSource in sourceChannelMapping)
                    {
                        // Adjust volume
                        var channel = sequenceSource.Item1;
                        var sequence = sequenceSource.Item2;
                        var source = sequenceSource.Item3;

                        if(loopT < sequence.StartTime || loopT > sequence.StartTime + sequence.Duration)
                        {
                            source.volume = 0;
                            continue;
                        }

                        var sequenceTime = loopT - (float)sequence.StartTime;
                        source.volume = channel.Volume *
                            sequence.StrengthCurve.Evaluate(Strength) *
                            sequence.VolumeCurve.Evaluate((float)sequenceTime / (float)sequence.Duration);
                    }
                    yield return null;
                }
                loopStartTime = AudioSettings.dspTime + lookAhead;
            }
            while (PlayState != ePlayState.Stopped && track.Loop);
            foreach (var sequenceSource in sourceChannelMapping)
            {
                var source = sequenceSource.Item3;
                if (source)
                {
                    Destroy(source.gameObject);
                }
            }
            m_currentCoroutine = null;
        }
    }
}