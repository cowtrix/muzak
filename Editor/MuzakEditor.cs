using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Muzak
{
    public class MuzakEditor : EditorWindow
    {
        [MenuItem("Tools/Muzak/Editor")]
        public static void OpenWindow()
        {
            var w = GetWindow<MuzakEditor>();
            w.titleContent = new GUIContent("Muzak Editor");
        }

        public const int TIME_LEAD = 15;
        public const float SECOND_WIDTH = 59f;

        public MuzakTrack Track { get; private set; }
        public Vector2 TrackScroll { get; private set; }
        public bool DragDown { get; private set; }
        public bool SnapEnabled { get; private set; } = true;
        public float SnapThreshold { get; private set; } = .25f;
        public Texture2D GridTexture
        {
            get
            {
                if (!m_gridTexture)
                {
                    m_gridTexture = new Texture2D(64, 64);
                    for (var x = 0; x < 64; x++)
                    {
                        for (var y = 0; y < 64; y++)
                        {
                            m_gridTexture.SetPixel(x, y, x == 0 ? new Color(1, 1, 1, .5f) : Color.clear);
                        }
                    }
                    m_gridTexture.Apply();
                    m_gridTexture.hideFlags = HideFlags.DontSave;
                }
                return m_gridTexture;
            }
        }
        private Texture2D m_gridTexture;

        private MuzakPlayer m_focusedPlayer;
        private Dictionary<MuzakSequence, Color> m_colorMap = new Dictionary<MuzakSequence, Color>();

        public void Update()
        {
            RefreshSelectedPlayer();
            // This is necessary to make the framerate normal for the editor window.
            Repaint();
        }

        private void OnSelectionChange()
        {
            RefreshSelectedPlayer();
        }

        private void RefreshSelectedPlayer()
        {
            var player = Selection.activeGameObject?.GetComponent<MuzakPlayer>();
            if (player && (player != m_focusedPlayer || m_focusedPlayer.Equals(null)))
            {
                if (m_focusedPlayer && !m_focusedPlayer.Equals(null))
                {
                    m_focusedPlayer.EventListener.RemoveAllListeners();
                }
                m_focusedPlayer = player;
                m_focusedPlayer.EventListener.AddListener(OnPlayerEvent);
            }
        }

        private void OnPlayerEvent(MuzakPlayerEvent.MuzakEventInfo ev)
        {
            switch (ev.EventType)
            {
                case MuzakPlayerEvent.eEventType.TrackLoopStarted:
                case MuzakPlayerEvent.eEventType.TrackLoopEnded:
                case MuzakPlayerEvent.eEventType.PlayerStop:
                    m_colorMap.Clear();
                    return;
                case MuzakPlayerEvent.eEventType.SequenceStarted:
                    m_colorMap[Track.Channels[ev.Channel].Sequences[ev.Sequence]] = Color.blue;
                    break;
                case MuzakPlayerEvent.eEventType.SequenceEnded:
                    m_colorMap.Remove(Track.Channels[ev.Channel].Sequences[ev.Sequence]);
                    break;
            }
        }

        private void OnGUI()
        {
            GUI.skin = Resources.Load<GUISkin>("MuzakSkin");
            RefreshSelectedPlayer();

            // Toolbar
            EditorGUILayout.BeginHorizontal("Window", GUILayout.ExpandWidth(true), GUILayout.Height(20));
            GUILayout.Label(EditorGUIUtility.IconContent("Open"), GUILayout.Width(25));
            Track = (MuzakTrack)EditorGUILayout.ObjectField("", Track, typeof(MuzakTrack), true, GUILayout.Width(200));
            if (Track)
            {
                GUILayout.Label(EditorGUIUtility.IconContent("MainStageView"), GUILayout.Width(20));
                SnapEnabled = EditorGUILayout.Toggle(SnapEnabled, GUILayout.Width(20));
                GUI.enabled = SnapEnabled;
                SnapThreshold = EditorGUILayout.FloatField(SnapThreshold, GUILayout.Width(30));
                GUI.enabled = true;

                GUILayout.Label(EditorGUIUtility.IconContent("d_preAudioLoopOff"), GUILayout.Width(20));
                Track.Loop = EditorGUILayout.Toggle(Track.Loop, GUILayout.Width(30));

                GUILayout.Label("BPM", GUILayout.Width(30));
                Track.BPM = EditorGUILayout.IntField(Track.BPM, GUILayout.Width(30));
            }
            GUILayout.Label("", GUILayout.ExpandWidth(true));
            GUILayout.Label("Focused Player", GUILayout.ExpandWidth(false));
            GUI.enabled = false;
            EditorGUILayout.ObjectField("", m_focusedPlayer, typeof(MuzakPlayer), true, GUILayout.Width(200));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (!Track)
            {
                EditorGUILayout.HelpBox("Open a Track", MessageType.Info);
                return;
            }

            // Timeline
            var xScroll = new Vector2(TrackScroll.x, 0);
            var yScroll = new Vector2(0, TrackScroll.y);
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            {
                // Track list
                EditorGUILayout.BeginScrollView(yScroll, GUIStyle.none, GUIStyle.none, GUILayout.Width(126));
                EditorGUILayout.LabelField("Channels", EditorStyles.miniLabel, GUILayout.Height(20));
                foreach (var channel in Track.Channels)
                {
                    EditorGUILayout.BeginVertical("Window", GUILayout.Height(64), GUILayout.Width(110));
                    var newClip = (AudioClip)EditorGUILayout.ObjectField("", channel.Clip, typeof(AudioClip), true, GUILayout.Width(100));
                    if (newClip != channel.Clip)
                    {
                        if (!channel.Clip && !channel.Sequences.Any())
                        {
                            channel.Sequences.Add(new MuzakSequence
                            {
                                VolumeCurve = AnimationCurve.Linear(0, 1, 1, 1),
                                StrengthCurve = AnimationCurve.Linear(0, 0, 1, 1),
                                StartTime = 0,
                                Duration = newClip.length,
                                Probability = 1,
                            });
                        }
                        channel.Clip = newClip;
                    }
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(30), GUILayout.Height(23)))
                    {
                        Track.Channels.Remove(channel);
                        GUIUtility.ExitGUI();
                        return;
                    }
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Clipboard"), GUILayout.Width(30), GUILayout.Height(23)))
                    {
                        Track.Channels.Add(new MuzakChannel
                        {
                            Clip = channel.Clip,
                            Volume = channel.Volume,
                            Sequences = channel.Sequences.Select(s => s.Clone()).ToList()
                        });
                        GUIUtility.ExitGUI();
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("CreateAddNew"), GUILayout.Width(115)))
                {
                    Track.Channels.Add(new MuzakChannel
                    {
                        Volume = 1,
                    });
                }
                EditorGUILayout.EndScrollView();
            }

            {
                EditorGUILayout.BeginScrollView(TrackScroll, GUIStyle.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUILayout.BeginHorizontal(GUILayout.Width(Track.Duration * 64));
                for (var i = 0; i < Track.Duration + TIME_LEAD; i++)
                {
                    GUILayout.Label($"{i} . . . . . . . . . . . . . ", EditorStyles.miniBoldLabel, GUILayout.Width(SECOND_WIDTH), GUILayout.Height(15));
                }
                GUILayout.Label("", EditorStyles.miniBoldLabel, GUILayout.Width(SECOND_WIDTH * (Track.Duration - Mathf.FloorToInt(Track.Duration))), GUILayout.Height(15));
                EditorGUILayout.EndHorizontal();
                var lastRect = GUILayoutUtility.GetLastRect();
                lastRect.y += 5;
                lastRect.width = (Track.Duration + TIME_LEAD) * 63f;

                var durationPos = lastRect.xMin + Track.Duration * 63;
                GUI.Label(new Rect(new Vector2(durationPos, lastRect.y + 5), new Vector2(2, position.height)), "", EditorStyles.selectionRect);
                if (m_focusedPlayer)
                {
                    var playPos = lastRect.xMin + m_focusedPlayer.CurrentLoopTime * 63;
                    GUI.color = new Color(1, 0, 1, .5f);
                    GUI.Label(new Rect(new Vector2((float)playPos, lastRect.y + 5), new Vector2(2, position.height)), "", EditorStyles.selectionRect);
                    GUI.color = Color.white;
                }
                GUI.Label(new Rect(new Vector2(durationPos + 6, lastRect.y - 10), new Vector2(60, 20)), Track.Duration.ToString(), EditorStyles.miniLabel);

                lastRect.x += 3;
                var newDuration = GUI.HorizontalSlider(lastRect, Track.Duration, 0, Track.Duration + TIME_LEAD, GUIStyle.none, GUI.skin.horizontalSliderThumb);
                if (newDuration != Track.Duration && SnapEnabled)
                {
                    foreach (var channel in Track.Channels)
                    {
                        foreach (var sequence in channel.Sequences)
                        {
                            var finalStartTime = (float)sequence.StartTime;
                            if (Math.Abs(finalStartTime - newDuration) < SnapThreshold)
                            {
                                newDuration = finalStartTime;
                                break;
                            }
                            var finalEndTIme = (float)(sequence.StartTime + sequence.Duration);
                            if (Math.Abs(finalEndTIme - newDuration) < SnapThreshold)
                            {
                                newDuration = finalEndTIme;
                                break;
                            }
                        }
                    }
                }
                Track.Duration = newDuration;


                for (int channelIndex = 0; channelIndex < Track.Channels.Count; channelIndex++)
                {
                    MuzakChannel channel = Track.Channels[channelIndex];
                    EditorGUILayout.BeginHorizontal(GUILayout.Height(64));

                    // Draw BPM grid
                    if(Track.BPM > 30)
                    {
                        var bpmWidth = 63 * (60 / (float)Track.BPM);
                        var stepCount = ((Track.Duration + TIME_LEAD) * 63) / bpmWidth;
                        for (var i = 0; i < stepCount; ++i)
                        {
                            GUI.DrawTexture(new Rect(4 + i * bpmWidth, 22 + channelIndex * 67, bpmWidth, 66), GridTexture);
                        }
                    }
                    
                    // Draw Sequences
                    for (int sequenceIndex = 0; sequenceIndex < channel.Sequences.Count; sequenceIndex++)
                    {
                        var sequence = channel.Sequences[sequenceIndex];
                        var lastSequence = sequenceIndex > 0 ? channel.Sequences[sequenceIndex - 1] : null;
                        var currentOffset = lastSequence != null ? lastSequence.StartTime + lastSequence.Duration : 0;
                        if(sequenceIndex == 0 || sequence.StartTime - currentOffset > 1)
                        {
                            GUILayout.Label("", GUILayout.Width((float)(sequence.StartTime - currentOffset) * 63));
                        }
                        if(!m_colorMap.TryGetValue(sequence, out var color))
                        {
                            color = Color.white;
                        }
                        GUI.color = color;
                        GUILayout.Label("", EditorStyles.selectionRect, GUILayout.Height(64), GUILayout.Width((float)sequence.Duration * 63));
                        GUI.color = Color.white;
                        var buttonRect = GUILayoutUtility.GetLastRect();
                        if (buttonRect.Contains(Event.current.mousePosition))
                        {
                            if (GUI.Button(new Rect(buttonRect.xMax - 15, buttonRect.y + 5, 20, 20), EditorGUIUtility.IconContent("Clipboard"), GUIStyle.none))
                            {
                                var clone = sequence.Clone();
                                clone.StartTime = sequence.StartTime + sequence.Duration;
                                channel.Sequences.Add(clone);
                            }
                            else if (Event.current.button == 0)
                            {
                                if (Event.current.type == EventType.MouseDown)
                                {
                                    DragDown = true;
                                }
                                else if (Event.current.type == EventType.MouseUp)
                                {
                                    DragDown = false;
                                    // Select sequence
                                    MuzakSequenceEditor.SetData(Track, channelIndex, sequenceIndex);
                                    Event.current.Use();
                                }
                                if (DragDown)
                                {
                                    sequence.StartTime = System.Math.Max(0, sequence.StartTime + Event.current.delta.x / 64f);
                                }
                            }
                        }
                        
                        if (channel.Clip)
                        {
                            var textureRect = new Rect((float)sequence.Offset / channel.Clip.length, 0, Mathf.Min(1, (float)sequence.Duration / channel.Clip.length), 1);
                            GUI.DrawTextureWithTexCoords(buttonRect, MuzakClipDisplayUtility.GetTexture(channel.Clip), textureRect);
                        }

                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            var margins = new Vector2(130, 55);
            var scrollDisplaySize = new Vector2(position.width - margins.x, position.height - margins.y);
            var contentDisplaySize = new Vector2((Track.Duration + TIME_LEAD) * 63, Track.Channels.Count * 70);
            var scrollSize = new Vector2(Mathf.Min(scrollDisplaySize.x, (contentDisplaySize.x / scrollDisplaySize.x) * scrollDisplaySize.x), 
                Mathf.Min(scrollDisplaySize.y, (contentDisplaySize.y / scrollDisplaySize.y) * scrollDisplaySize.y));

            var newYScroll = GUILayout.VerticalScrollbar(yScroll.y, scrollSize.y, 0, contentDisplaySize.y, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(126);
            var newXScroll = GUILayout.HorizontalScrollbar(xScroll.x, scrollSize.x, 0, contentDisplaySize.x, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            TrackScroll = new Vector2(newXScroll, newYScroll);

            EditorUtility.SetDirty(Track);
        }
    }
}