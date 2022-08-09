using UnityEditor;
using UnityEngine;

namespace Muzak
{
    public class MuzakSequenceEditor : EditorWindow
    {
        public MuzakTrack CurrentTrack { get; private set; }
        public MuzakChannel CurrentChannel { get; private set; }
        public MuzakSequence CurrentSequence { get; private set; }
        public static void SetData(MuzakTrack track, MuzakChannel channel, MuzakSequence sequence)
        {
            var w = GetWindow<MuzakSequenceEditor>($"Edit Sequence: {channel.Clip}", true, typeof(MuzakEditor));
            w.CurrentTrack = track;
            w.CurrentChannel = channel;
            w.CurrentSequence = sequence;
        }

        private void OnGUI()
        {
            GUI.skin = Resources.Load<GUISkin>("MuzakSkin");
            GUI.enabled = false;
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

            var minTime = 0f;
            var maxTime = CurrentChannel.Clip.length;
            var startTime = (float)CurrentSequence.Offset;
            var endTime = startTime + CurrentSequence.Duration;

            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("", GUILayout.Height(64), GUILayout.ExpandWidth(true));
            var lastRect = GUILayoutUtility.GetLastRect();
            var selectedRect = new Rect(
                lastRect.x + (float)((CurrentSequence.Offset / CurrentChannel.Clip.length) * lastRect.width),
                lastRect.y,
                (CurrentSequence.Duration / CurrentChannel.Clip.length) * lastRect.width,
                lastRect.height);
            GUI.Label(selectedRect, "", EditorStyles.selectionRect);
            GUI.DrawTexture(lastRect, MuzakClipDisplayUtility.GetTexture(CurrentChannel.Clip));
            EditorGUILayout.MinMaxSlider(ref startTime, ref endTime, minTime, maxTime);
            CurrentSequence.Offset = startTime;
            CurrentSequence.Duration = endTime - startTime;
            EditorGUILayout.EndVertical();
        }
    }
}