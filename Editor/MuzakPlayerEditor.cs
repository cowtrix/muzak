using UnityEditor;
using UnityEngine;

namespace Muzak
{
    [CustomEditor(typeof(MuzakPlayer))]
    public class MuzakPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var player = (MuzakPlayer)target;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_PlayButton On"), EditorStyles.miniButtonLeft))
            {
                player.Play();
            }
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_PauseButton On"), EditorStyles.miniButtonMid))
            {
                player.Pause();
            }
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_PreMatQuad"), EditorStyles.miniButtonRight))
            {
                player.Stop();
            }
            EditorGUILayout.EndHorizontal();


            base.OnInspectorGUI();
        }
    }
}