using System.IO;
using UnityEngine;

public class MultiPGNParser : MonoBehaviour
{
    public TextAsset[] inputFiles;
    public TextAsset outputFile;
    public bool append;

    [ContextMenu("Parse All")]
    private void ParseAll()
    {
        var allGames = "";
        foreach (var f in inputFiles) allGames += Parse(f.text);

        FileWriter.WriteToTextAsset_EditorOnly(outputFile, allGames, append);
    }

    private string Parse(string text)
    {
        var isReadingPGN = false;
        var currentPgn = "";
        var parsedGames = "";

        var reader = new StringReader(text);

        string line;
        while ((line = reader.ReadLine()) != null)
            if (line.Contains("["))
            {
                if (isReadingPGN)
                {
                    isReadingPGN = false;
                    parsedGames += currentPgn.Replace("  ", " ").Trim() + '\n';
                    currentPgn = "";
                }
            }
            else
            {
                isReadingPGN = true;
                var moves = line.Split(' ');
                foreach (var move in moves)
                {
                    var formattedMove = move;
                    if (formattedMove.Contains(".")) formattedMove = formattedMove.Split('.')[1];
                    currentPgn += formattedMove.Trim() + " ";
                }
            }

        return parsedGames;
    }
}