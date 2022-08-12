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
            /*EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Animation.Play"), EditorStyles.miniButtonLeft))
            {
                player.PlayTrack(player.Track);
            }
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Animation.Stop"), EditorStyles.miniButtonLeft))
            {
                //player.StopAllCoroutines();
            }
            EditorGUILayout.EndHorizontal();*/


            base.OnInspectorGUI();
        }
    }
}