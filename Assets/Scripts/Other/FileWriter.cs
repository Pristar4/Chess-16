using System.IO;
using UnityEditor;
using UnityEngine;

public static class FileWriter
{
    public static void WriteToTextAsset_EditorOnly(TextAsset textAsset, string text, bool append)
    {
#if UNITY_EDITOR
        var outputPath = AssetDatabase.GetAssetPath(textAsset);
        var writer = new StreamWriter(outputPath, append);
        writer.Write(text);
        writer.Close();
#endif
    }
}