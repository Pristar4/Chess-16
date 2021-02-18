using Chess.Game;
using UnityEditor;
using UnityEngine;

namespace Chess.EditorScripts
{
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : Editor
    {
        private Editor aiSettingsEditor;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var manager = target as GameManager;

            var foldout = true;
            DrawSettingsEditor(manager.aiSettings, ref foldout, ref aiSettingsEditor);
        }

        private void DrawSettingsEditor(Object settings, ref bool foldout, ref Editor editor)
        {
            if (settings != null)
            {
                foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
                if (foldout)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();
                }
            }
        }
    }
}