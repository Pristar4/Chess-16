using UnityEditor;
using UnityEngine;

namespace Chess.Testing
{
    [CustomEditor(typeof(Perft))]
    public class PerftEditor : Editor
    {
        private Perft perft;

        private void OnEnable()
        {
            perft = (Perft) target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10);
            if (GUILayout.Button("Run Single")) perft.RunSingleTest();
        }
    }
}