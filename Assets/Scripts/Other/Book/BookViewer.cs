using System;
using System.Collections.Generic;
using System.Diagnostics;
using Chess.Game;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Chess
{
    public class BookViewer : MonoBehaviour
    {
        public TextAsset bookFile;
        public float arrowWidth = 0.1f;
        public float arrowHeadSize = 0.1f;
        public Material arrowMaterial;
        public Color mostCommonCol;
        public Color rarestCol;
        private int arrowIndex;

        private List<GameObject> arrowObjects;
        private Board board;
        private BoardUI boardUI;

        private Book book;
        private Stack<Move> moves;
        private Player player;

        private void Start()
        {
            moves = new Stack<Move>();
            arrowObjects = new List<GameObject>();
            board = new Board();
            var sw = Stopwatch.StartNew();
            book = BookCreator.LoadBookFromFile(bookFile);
            Debug.Log("Book loaded: " + sw.ElapsedMilliseconds + " ms.");

            board.LoadStartPosition();
            boardUI = FindObjectOfType<BoardUI>();
            boardUI.UpdatePosition(board);

            player = new HumanPlayer(board);
            player.onMoveChosen += OnMoveChosen;
            player.NotifyTurnToMove();
            DrawBookMoves();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
                if (moves.Count > 0)
                {
                    var move = moves.Pop();
                    board.UnmakeMove(move);
                    if (moves.Count > 0)
                    {
                        boardUI.OnMoveMade(board, moves.Peek());
                    }
                    else
                    {
                        boardUI.UpdatePosition(board);
                        boardUI.ResetSquareColours(false);
                    }
                }

            DrawBookMoves();
            player.Update();
        }

        private void DrawBookMoves()
        {
            ClearArrows();
            arrowIndex = 0;

            if (book.HasPosition(board.ZobristKey))
            {
                var bookPosition = book.GetBookPosition(board.ZobristKey);
                var mostPlayed = 0;
                var leastPlayed = int.MaxValue;
                foreach (var moveInfo in bookPosition.numTimesMovePlayed)
                {
                    var numTimesPlayed = moveInfo.Value;
                    mostPlayed = Math.Max(mostPlayed, numTimesPlayed);
                    leastPlayed = Math.Min(leastPlayed, numTimesPlayed);
                }

                foreach (var moveInfo in bookPosition.numTimesMovePlayed)
                {
                    var move = new Move(moveInfo.Key);
                    var numTimesPlayed = moveInfo.Value;
                    Vector2 startPos = boardUI.PositionFromCoord(BoardRepresentation.CoordFromIndex(move.StartSquare));
                    Vector2 endPos = boardUI.PositionFromCoord(BoardRepresentation.CoordFromIndex(move.TargetSquare));
                    var t = Mathf.InverseLerp(leastPlayed, mostPlayed, numTimesPlayed);
                    if (mostPlayed == leastPlayed) t = 1;

                    var col = Color.Lerp(rarestCol, mostCommonCol, t);
                    DrawArrow2D(startPos, endPos, arrowWidth, arrowHeadSize, col, zPos: -1 - t);
                }
            }
        }

        private void OnMoveChosen(Move move)
        {
            board.MakeMove(move);
            moves.Push(move);
            boardUI.OnMoveMade(board, move);
            DrawBookMoves();
        }

        /// Draw a 2D arrow (on xy plane)
        private void DrawArrow2D(Vector2 start, Vector2 end, float lineWidth, float headSize, Color color,
            bool flatHead = true, float zPos = 0)
        {
            if (arrowIndex >= arrowObjects.Count)
            {
                var arrowObject = new GameObject("Arrow");
                arrowObject.transform.parent = transform;

                var renderer = arrowObject.AddComponent<MeshRenderer>();
                var filter = arrowObject.AddComponent<MeshFilter>();
                renderer.material = arrowMaterial;
                filter.mesh = new Mesh();
                arrowObjects.Add(arrowObject);
            }

            arrowObjects[arrowIndex].transform.position = new Vector3(0, 0, zPos);
            arrowObjects[arrowIndex].SetActive(true);
            arrowObjects[arrowIndex].GetComponent<MeshRenderer>().material.color = color;
            var mesh = arrowObjects[arrowIndex].GetComponent<MeshFilter>().mesh;
            ArrowMesh.CreateArrowMesh(ref mesh, start, end, lineWidth, headSize, flatHead);
            arrowIndex++;
        }

        private void ClearArrows()
        {
            for (var i = 0; i < arrowObjects.Count; i++) arrowObjects[i].SetActive(false);
        }
    }
}