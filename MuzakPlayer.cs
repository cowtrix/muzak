using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Muzak
{
    public class MuzakPlayerEvent : UnityEvent<MuzakPlayerEvent.MuzakEventInfo>
    {
        public enum eEventType
        {
            PlayerPlay,
            PlayerPause,
            PlayerStop,
            TrackLoopStarted,
            TrackLoopEnded,
            SequenceStarted,
            SequenceSkipped,
            SequenceEnded,
        }

        public struct MuzakEventInfo
        {
            public eEventType EventType;
            public MuzakTrack Track;
            public MuzakPlayer Player;
            public int Channel;
            public int Sequence;
        }
    }

    public class MuzakPlayer : MonoBehaviour
    {
        public enum ePlayState
        {
            Stopped,
            Stopping,
            Paused,
            Playing,
        }

        public MuzakPlayerEvent EventListener { get; private set; } = new MuzakPlayerEvent();
        public ePlayState PlayState { get; private set; }
        public double CurrentLoopTime { get; set; }

        public MuzakTrack Track;
        [Range(0, 1)]
        public float Strength = 1;

        public float FadeInTime = 1;
        public AnimationCurve FadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float FadeOutTime = 1;
        public AnimationCurve FadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public AudioSource AudioSourcePrototype;

        private Coroutine m_currentCoroutine;
        private Dictionary<MuzakSequence, (MuzakChannel, AudioSource)> m_sequenceSourceMappings = new Dictionary<MuzakSequence, (MuzakChannel, AudioSource)>();

        public void Play()
        {
            PlayState = ePlayState.Playing;
            if (m_currentCoroutine == null)
            {
                m_currentCoroutine = StartCoroutine(PlayTrackAsync(Track));
            }
            EventListener.Invoke(new MuzakPlayerEvent.MuzakEventInfo
            {
                EventType = MuzakPlayerEvent.eEventType.PlayerPlay,
                Player = this,
                Track = Track,
                Channel = -1,
                Sequence = -1,
            });
        }

        public void Pause()
        {
            PlayState = ePlayState.Paused;
            EventListener.Invoke(new MuzakPlayerEvent.MuzakEventInfo
            {
                EventType = MuzakPlayerEvent.eEventType.PlayerPause,
                Player = this,
                Track = Track,
                Channel = -1,
                Sequence = -1,
            });
        }

        public void Stop()
        {
            PlayState = ePlayState.Stopping;
        }

        private AudioSource GetAudioSource(MuzakChannel channel, MuzakSequence sequence)
        {
            var channelIndex = Track.Channels.IndexOf(channel);
            var sequenceIndex = Track.Channels[channelIndex].Sequences.IndexOf(sequence);
            var roll = Random.value;
            if (roll > sequence.Probability)
            {
                StartCoroutine(DelayedEvent(new MuzakPlayerEvent.MuzakEventInfo
                {
                    Channel = channelIndex,
                    Sequence = sequenceIndex,
                    Track = Track,
                    EventType = MuzakPlayerEvent.eEventType.SequenceSkipped,
                    Player = this,
                }, (float)sequence.StartTime));
                return null;
            }
            AudioSource source;
            if (AudioSourcePrototype)
            {
                source = Instantiate(AudioSourcePrototype.gameObject)
                    .GetComponent<AudioSource>();
            }
            else
            {
                source = new GameObject($"MuzakTrackPlayer_{channel.Clip.name}")
                    .AddComponent<AudioSource>();
            }
            source.transform.SetParent(transform);
            source.clip = channel.Clip;
            source.outputAudioMixerGroup = Track.Mixer;
            source.playOnAwake = false;
            source.gameObject.hideFlags = HideFlags.HideAndDontSave;

            
            return source;
        }

        private void StartSequence(MuzakChannel channel, MuzakSequence sequence, AudioSource source)
        {
            var channelIndex = Track.Channels.IndexOf(channel);
            var sequenceIndex = Track.Channels[channelIndex].Sequences.IndexOf(sequence);
            source.time = (float)sequence.Offset;
            source.PlayScheduled(AudioSettings.dspTime + sequence.StartTime);
            source.SetScheduledEndTime(AudioSettings.dspTime + sequence.StartTime + sequence.Duration);

            StartCoroutine(DelayedEvent(new MuzakPlayerEvent.MuzakEventInfo
            {
                Channel = channelIndex,
                Sequence = sequenceIndex,
                Track = Track,
                EventType = MuzakPlayerEvent.eEventType.SequenceStarted,
                Player = this,
            }, (float)sequence.StartTime));
            StartCoroutine(DelayedEvent(new MuzakPlayerEvent.MuzakEventInfo
            {
                Channel = channelIndex,
                Sequence = sequenceIndex,
                Track = Track,
                EventType = MuzakPlayerEvent.eEventType.SequenceEnded,
                Player = this,
            }, (float)(sequence.StartTime + sequence.Duration)));
        }

        private IEnumerator DelayedEvent(MuzakPlayerEvent.MuzakEventInfo eventInfo, float delay)
        {
            yield return new WaitForSeconds(delay);
            EventListener.Invoke(eventInfo);
        }

        IEnumerator PlayTrackAsync(MuzakTrack track)
        {
            while (PlayState != ePlayState.Playing)
            {
                yield return null;
            }

            var loopStartTime = AudioSettings.dspTime;
            var playingness = 0f;
            do
            {
                EventListener.Invoke(new MuzakPlayerEvent.MuzakEventInfo
                {
                    EventType = MuzakPlayerEvent.eEventType.TrackLoopStarted,
                    Player = this,
                    Track = Track,
                    Channel = -1,
                    Sequence = -1,
                });

                const double lookAhead = .0;
                foreach (var channel in track.Channels)
                {
                    foreach (var sequence in channel.Sequences)
                    {
                        AudioSource audioSource;
                        if (m_sequenceSourceMappings.TryGetValue(sequence, out var source) && source.Item2)
                        {
                            audioSource = source.Item2;
                        }
                        else
                        {
                            audioSource = GetAudioSource(channel, sequence);
                        }
                        if (audioSource == null)
                        {
                            continue;
                        }
                        m_sequenceSourceMappings[sequence] = (channel, audioSource);
                        StartSequence(channel, sequence, audioSource);
                    }
                }

                while (PlayState != ePlayState.Stopped && AudioSettings.dspTime < (loopStartTime + Track.Duration) - lookAhead)
                {
                    var loopT = AudioSettings.dspTime - loopStartTime;
                    CurrentLoopTime = loopT;

                    // Depending on if we're starting or stopping change the transition amount
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


                    foreach (var sequenceSourceMapping in m_sequenceSourceMappings)
                    {
                        var sequence = sequenceSourceMapping.Key;
                        var channel = sequenceSourceMapping.Value.Item1;
                        var source = sequenceSourceMapping.Value.Item2;

                        // Adjust volume
                        if (loopT < sequence.StartTime || loopT > sequence.StartTime + sequence.Duration)
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
                EventListener.Invoke(new MuzakPlayerEvent.MuzakEventInfo
                {
                    EventType = MuzakPlayerEvent.eEventType.TrackLoopEnded,
                    Player = this,
                    Track = Track,
                    Channel = -1,
                    Sequence = -1,
                });
            }
            while (PlayState != ePlayState.Stopped && track.Loop);

            EventListener.Invoke(new MuzakPlayerEvent.MuzakEventInfo
            {
                EventType = MuzakPlayerEvent.eEventType.PlayerStop,
                Player = this,
                Track = Track,
                Channel = -1,
                Sequence = -1,
            });
            m_currentCoroutine = null;
        }
    }
}