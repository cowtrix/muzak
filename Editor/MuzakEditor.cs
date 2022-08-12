using System;
using System.Collections;
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

        public const int TIME_LEAD = 1;
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

        private void OnGUI()
        {
            GUI.skin = Resources.Load<GUISkin>("MuzakSkin");

            // Toolbar
            EditorGUILayout.BeginHorizontal("Window", GUILayout.ExpandWidth(true), GUILayout.Height(20));
            GUILayout.Label(EditorGUIUtility.IconContent("Open"), GUILayout.Width(25));
            Track = (MuzakTrack)EditorGUILayout.ObjectField("", Track, typeof(MuzakTrack), true, GUILayout.Width(200));
            GUILayout.Label(EditorGUIUtility.IconContent("MainStageView"), GUILayout.Width(20));
            SnapEnabled = EditorGUILayout.Toggle(SnapEnabled, GUILayout.Width(20));
            GUI.enabled = SnapEnabled;
            SnapThreshold = EditorGUILayout.FloatField(SnapThreshold, GUILayout.Width(30));
            GUI.enabled = true;

            GUILayout.Label(EditorGUIUtility.IconContent("d_preAudioLoopOff"), GUILayout.Width(20));
            Track.Loop = EditorGUILayout.Toggle(Track.Loop, GUILayout.Width(30));

            GUILayout.Label("BPM", GUILayout.Width(30));
            Track.BPM = EditorGUILayout.IntField(Track.BPM, GUILayout.Width(30));

            EditorGUILayout.EndHorizontal();

            if (!Track)
            {
                EditorGUILayout.HelpBox("Open a Track", MessageType.Info);
                return;
            }

            var ySize = Track.Channels.Count * 70;

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
                                StartThreshold = 0,
                                EndThreshold = 0,
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
                    if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(30)))
                    {
                        Track.Channels.Remove(channel);
                        GUIUtility.ExitGUI();
                        return;
                    }
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Clipboard"), GUILayout.Width(30)))
                    {
                        Track.Channels.Add(new MuzakChannel
                        {
                            Clip = channel.Clip,
                            Volume = channel.Volume,
                            Sequences = channel.Sequences.Select(s => new MuzakSequence
                            {
                                Duration = s.Duration,
                                StartThreshold = s.StartThreshold,
                                EndThreshold = s.EndThreshold,
                                Offset = s.Offset,
                                StrengthCurve = s.StrengthCurve,
                                VolumeCurve = s.VolumeCurve,
                                Probability = s.Probability,
                                StartTime = s.StartTime
                            }).ToList()
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
                EditorGUILayout.BeginScrollView(xScroll, GUIStyle.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUILayout.BeginHorizontal(GUILayout.Width(Track.Duration * 64));
                //GUILayout.Label("", GUILayout.Width(2));
                for (var i = 0; i < Track.Duration + TIME_LEAD; i++)
                {
                    GUILayout.Label($"{i} . . . . . . . . . . . . . ", EditorStyles.miniBoldLabel, GUILayout.Width(SECOND_WIDTH), GUILayout.Height(15));
                }
                GUILayout.Label("", EditorStyles.miniBoldLabel, GUILayout.Width(SECOND_WIDTH * (Track.Duration - Mathf.FloorToInt(Track.Duration))), GUILayout.Height(15));
                EditorGUILayout.EndHorizontal();
                var lastRect = GUILayoutUtility.GetLastRect();
                lastRect.y += 5;
                lastRect.width = (Track.Duration + TIME_LEAD) * 64f;

                var durationPos = lastRect.xMin + Track.Duration * 63;
                GUI.Label(new Rect(new Vector2(durationPos, lastRect.y + 5), new Vector2(2, position.height)), "", EditorStyles.selectionRect);
                GUI.Label(new Rect(new Vector2(durationPos, lastRect.y - 10), new Vector2(60, 20)), Track.Duration.ToString(), EditorStyles.miniLabel);

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

                    var bpmWidth = 63 * (60 / (float)Track.BPM);
                    var stepCount = ((Track.Duration + TIME_LEAD) * 63) / bpmWidth;
                    for (var i = 0; i < stepCount; ++i)
                    {
                        GUI.DrawTexture(new Rect(4 + i * bpmWidth + TrackScroll.x, 22 + channelIndex * 67 + TrackScroll.y, bpmWidth, 64), GridTexture);
                    }

                    //GUILayout.Label("", GUILayout.Width(3));
                    var offsetCounter = 0.0;
                    for (int sequenceIndex = 0; sequenceIndex < channel.Sequences.Count; sequenceIndex++)
                    {
                        MuzakSequence sequence = channel.Sequences[sequenceIndex];
                        offsetCounter += sequence.StartTime;
                        GUILayout.Label("", GUILayout.Width((float)offsetCounter * 64));
                        GUILayout.Label("", EditorStyles.selectionRect, GUILayout.Height(64), GUILayout.Width((float)sequence.Duration * 63));
                        var buttonRect = GUILayoutUtility.GetLastRect();
                        if (buttonRect.Contains(Event.current.mousePosition) && Event.current.button == 0)
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

            {

            }

            var yScrollSize = Mathf.Min(1, ((this.position.height - 80) / (float)ySize));
            var xScrollSize = Mathf.Min(1, (this.position.width - 150) / (float)((Track.Duration + TIME_LEAD) * 32));
            var newYScroll = GUILayout.VerticalScrollbar(yScroll.y, yScrollSize, 0, 1, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(126);
            var newXScroll = GUILayout.HorizontalScrollbar(xScroll.x, xScrollSize, 0, 1, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            TrackScroll = new Vector2(newXScroll, newYScroll);

            EditorUtility.SetDirty(Track);
        }
    }
}