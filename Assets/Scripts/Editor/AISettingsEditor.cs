using UnityEditor;
using UnityEngine;

namespace Chess.EditorScripts
{
    [CustomEditor(typeof(AISettings))]
    public class AISettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var settings = target as AISettings;

            if (settings.useThreading)
                if (GUILayout.Button("Abort Search"))
                    settings.RequestAbortSearch();
        }
    }
}