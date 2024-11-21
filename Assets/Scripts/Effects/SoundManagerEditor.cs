#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Audio
{
    [CustomEditor(typeof(SoundManager))]
    public class SoundManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Play sound"))
            {
                var soundManager = (SoundManager)target;
                soundManager.PlayPreviewSound();
            }
        }
    }
}
#endif