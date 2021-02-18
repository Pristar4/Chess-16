using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Chess
{
    public class BookCreator : MonoBehaviour
    {
        public int maxPlyToRecord;

        public int minMovePlayCount = 10;

        public TextAsset gamesFile;
        public TextAsset bookFile;
        public bool append;

        private void Start()
        {
        }

        [ContextMenu("Create Book")]
        private void CreateBook()
        {
            var sw = Stopwatch.StartNew();
            var book = new Book();

            var reader = new StringReader(gamesFile.text);
            string pgn;
            var board = new Board();
            while (!string.IsNullOrEmpty(pgn = reader.ReadLine()))
            {
                var moves = PGNLoader.MovesFromPGN(pgn, maxPlyToRecord);
                board.LoadStartPosition();

                for (var i = 0; i < moves.Length; i++)
                {
                    book.Add(board.ZobristKey, moves[i]);
                    board.MakeMove(moves[i]);
                }
            }

            var bookString = "";

            foreach (var bookPositionsByZobristKey in book.bookPositions)
            {
                var key = bookPositionsByZobristKey.Key;
                var bookPosition = bookPositionsByZobristKey.Value;
                var line = key + ":";

                var isFirstMoveEntry = true;
                foreach (var moveCountByMove in bookPosition.numTimesMovePlayed)
                {
                    var moveValue = moveCountByMove.Key;
                    var moveCount = moveCountByMove.Value;
                    if (moveCount >= minMovePlayCount)
                    {
                        if (isFirstMoveEntry)
                            isFirstMoveEntry = false;
                        else
                            line += ",";
                        line += $" {moveValue} ({moveCount})";
                    }
                }

                var hasRecordedAnyMoves = !isFirstMoveEntry;
                if (hasRecordedAnyMoves) bookString += line + Environment.NewLine;
            }

            //string s = fastJSON.JSON.ToJSON (book);
            FileWriter.WriteToTextAsset_EditorOnly(bookFile, bookString, append);
            Debug.Log("Created book: " + sw.ElapsedMilliseconds + " ms.");

            //Book loadedBook = fastJSON.JSON.ToObject<Book> (s);
        }

        public static Book LoadBookFromFile(TextAsset bookFile)
        {
            var book = new Book();
            var reader = new StringReader(bookFile.text);

            string line;
            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                var positionKey = ulong.Parse(line.Split(':')[0]);
                var moveInfoStrings = line.Split(':')[1].Trim().Split(',');

                for (var i = 0; i < moveInfoStrings.Length; i++)
                {
                    var moveInfoString = moveInfoStrings[i].Trim();
                    if (!string.IsNullOrEmpty(moveInfoString))
                    {
                        var moveValue = ushort.Parse(moveInfoString.Split(' ')[0]);
                        var numTimesPlayedString = moveInfoString.Split(' ')[1].Replace("(", "").Replace(")", "");
                        var numTimesPlayed = int.Parse(numTimesPlayedString);
                        book.Add(positionKey, new Move(moveValue), numTimesPlayed);
                    }
                }
            }

            return book;
        }
    }
}