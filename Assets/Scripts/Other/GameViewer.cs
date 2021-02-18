using Chess.Game;
using UnityEngine;

namespace Chess
{
    public class GameViewer : MonoBehaviour
    {
        [Multiline] public string pgn;

        private Board board;
        private BoardUI boardUI;

        private Move[] gameMoves;
        private int moveIndex;

        private void Start()
        {
            gameMoves = PGNLoader.MovesFromPGN(pgn);
            board = new Board();
            board.LoadStartPosition();
            boardUI = FindObjectOfType<BoardUI>();
            boardUI.UpdatePosition(board);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                if (moveIndex < gameMoves.Length)
                {
                    board.MakeMove(gameMoves[moveIndex]);
                    boardUI.OnMoveMade(board, gameMoves[moveIndex]);
                    moveIndex++;
                }
        }
    }
}