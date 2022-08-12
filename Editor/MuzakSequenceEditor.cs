using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Muzak
{
    public class MuzakSequenceEditor : EditorWindow
    {
        public Vector2 Scroll { get; private set; }
        public MuzakTrack CurrentTrack { get; private set; }
        public MuzakChannel CurrentChannel => CurrentTrack.Channels.ElementAtOrDefault(m_channelIndex);
        private int m_channelIndex;
        public MuzakSequence CurrentSequence => CurrentChannel?.Sequences.ElementAtOrDefault(m_sequenceIndex);
        private int m_sequenceIndex;
        public static void SetData(MuzakTrack track, int channel, int sequence)
        {
            var w = GetWindow<MuzakSequenceEditor>($"Edit Sequence", true, typeof(MuzakEditor));
            w.CurrentTrack = track;
            w.m_channelIndex = channel;
            w.m_sequenceIndex = sequence;
        }

        private void OnGUI()
        {
            titleContent = new GUIContent("Sequence Editor");
            if (!CurrentTrack || CurrentChannel == null || CurrentSequence == null)
            {
                EditorGUILayout.HelpBox("No sequence selected.", MessageType.Info);                
                return;
            }
            GUI.skin = Resources.Load<GUISkin>("MuzakSkin");
            GUI.enabled = false;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginVertical("Window", GUILayout.ExpandWidth(true), GUILayout.Height(20));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Track    ");
            EditorGUILayout.ObjectField("", CurrentTrack, typeof(MuzakTrack), true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Channel");
            EditorGUILayout.ObjectField("", CurrentChannel.Clip, typeof(AudioClip), true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUI.enabled = true;

            Scroll = EditorGUILayout.BeginScrollView(Scroll);
            GUILayout.Label("", GUILayout.Height(64), GUILayout.ExpandWidth(true));
            var lastRect = GUILayoutUtility.GetLastRect();
            var selectedRect = new Rect(
                lastRect.x + (float)((CurrentSequence.Offset / CurrentChannel.Clip.length) * lastRect.width),
                lastRect.y,
                ((float)CurrentSequence.Duration / CurrentChannel.Clip.length) * lastRect.width,
                lastRect.height);
            GUI.Label(selectedRect, "", EditorStyles.selectionRect);
            GUI.DrawTexture(lastRect, MuzakClipDisplayUtility.GetTexture(CurrentChannel.Clip));

            // Offset field and duration field
            CurrentSequence.Offset = EditorGUILayout.DoubleField("Offset", CurrentSequence.Offset);
            EditorGUILayout.BeginHorizontal();
            CurrentSequence.Duration = System.Math.Min(CurrentChannel.Clip.length - CurrentSequence.Offset, EditorGUILayout.DoubleField("Duration", CurrentSequence.Duration));
            if (GUILayout.Button(EditorGUIUtility.IconContent("StepButton"), GUILayout.Height(20), GUILayout.Width(30)))
            {
                CurrentSequence.Duration = CurrentChannel.Clip.length;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Volume over time");
            CurrentSequence.VolumeCurve = EditorGUILayout.CurveField(CurrentSequence.VolumeCurve);

            GUILayout.Label("Volume over strength");
            CurrentSequence.StrengthCurve = EditorGUILayout.CurveField(CurrentSequence.StrengthCurve);

            CurrentSequence.Probability = EditorGUILayout.Slider("Probability", CurrentSequence.Probability, 0, 1);

            EditorUtility.SetDirty(CurrentTrack);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }
}