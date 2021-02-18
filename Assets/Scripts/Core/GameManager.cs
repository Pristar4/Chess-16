﻿using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Chess.Game
{
    public class GameManager : MonoBehaviour
    {
        public enum PlayerType
        {
            Human,
            AI
        }

        public enum Result
        {
            Playing,
            WhiteIsMated,
            BlackIsMated,
            Stalemate,
            Repetition,
            FiftyMoveRule,
            InsufficientMaterial
        }

        public bool loadCustomPosition;
        public string customPosition = "1rbq1r1k/2pp2pp/p1n3p1/2b1p3/R3P3/1BP2N2/1P3PPP/1NBQ1RK1 w - - 0 1";

        public PlayerType whitePlayerType;
        public PlayerType blackPlayerType;
        public AISettings aiSettings;
        public Color[] colors;

        public bool useClocks;
        public Clock whiteClock;
        public Clock blackClock;
        public TMP_Text aiDiagnosticsUI;
        public TMP_Text resultUI;

        public ulong zobristDebug;
        private Player blackPlayer;
        private BoardUI boardUI;
        private List<Move> gameMoves;

        private Result gameResult;
        private Player playerToMove;
        private Board searchBoard; // Duplicate version of board used for ai search

        private Player whitePlayer;
        public Board board { get; private set; }

        private void Start()
        {
            //Application.targetFrameRate = 60;

            if (useClocks)
            {
                whiteClock.isTurnToMove = false;
                blackClock.isTurnToMove = false;
            }

            boardUI = FindObjectOfType<BoardUI>();
            gameMoves = new List<Move>();
            board = new Board();
            searchBoard = new Board();
            aiSettings.diagnostics = new Search.SearchDiagnostics();

            NewGame(whitePlayerType, blackPlayerType);
        }

        private void Update()
        {
            zobristDebug = board.ZobristKey;

            if (gameResult == Result.Playing)
            {
                LogAIDiagnostics();

                playerToMove.Update();

                if (useClocks)
                {
                    whiteClock.isTurnToMove = board.WhiteToMove;
                    blackClock.isTurnToMove = !board.WhiteToMove;
                }
            }

            if (Input.GetKeyDown(KeyCode.E)) ExportGame();
        }

        public event Action onPositionLoaded;
        public event Action<Move> onMoveMade;

        private void OnMoveChosen(Move move)
        {
            var animateMove = playerToMove is AIPlayer;
            board.MakeMove(move);
            searchBoard.MakeMove(move);

            gameMoves.Add(move);
            onMoveMade?.Invoke(move);
            boardUI.OnMoveMade(board, move, animateMove);

            NotifyPlayerToMove();
        }

        public void NewGame(bool humanPlaysWhite)
        {
            boardUI.SetPerspective(humanPlaysWhite);
            NewGame(humanPlaysWhite ? PlayerType.Human : PlayerType.AI,
                humanPlaysWhite ? PlayerType.AI : PlayerType.Human);
        }

        public void NewComputerVersusComputerGame()
        {
            boardUI.SetPerspective(true);
            NewGame(PlayerType.AI, PlayerType.AI);
        }

        private void NewGame(PlayerType whitePlayerType, PlayerType blackPlayerType)
        {
            gameMoves.Clear();
            if (loadCustomPosition)
            {
                board.LoadPosition(customPosition);
                searchBoard.LoadPosition(customPosition);
            }
            else
            {
                board.LoadStartPosition();
                searchBoard.LoadStartPosition();
            }

            onPositionLoaded?.Invoke();
            boardUI.UpdatePosition(board);
            boardUI.ResetSquareColours();

            CreatePlayer(ref whitePlayer, whitePlayerType);
            CreatePlayer(ref blackPlayer, blackPlayerType);

            gameResult = Result.Playing;
            PrintGameResult(gameResult);

            NotifyPlayerToMove();
        }

        private void LogAIDiagnostics()
        {
            var text = "";
            var d = aiSettings.diagnostics;
            //text += "AI Diagnostics";
            text += $"<color=#{ColorUtility.ToHtmlStringRGB(colors[3])}>Version 1.0\n";
            text += $"<color=#{ColorUtility.ToHtmlStringRGB(colors[0])}>Depth Searched: {d.lastCompletedDepth}";
            //text += $"\nPositions evaluated: {d.numPositionsEvaluated}";

            var evalString = "";
            if (d.isBook)
            {
                evalString = "Book";
            }
            else
            {
                var displayEval = d.eval / 100f;
                if (playerToMove is AIPlayer && !board.WhiteToMove) displayEval = -displayEval;
                evalString = $"{displayEval:00.00}".Replace(",", ".");
                if (Search.IsMateScore(d.eval)) evalString = $"mate in {Search.NumPlyToMateFromScore(d.eval)} ply";
            }

            text += $"\n<color=#{ColorUtility.ToHtmlStringRGB(colors[1])}>Eval: {evalString}";
            text += $"\n<color=#{ColorUtility.ToHtmlStringRGB(colors[2])}>Move: {d.moveVal}";

            aiDiagnosticsUI.text = text;
        }

        public void ExportGame()
        {
            var pgn = PGNCreator.CreatePGN(gameMoves.ToArray());
            var baseUrl = "https://www.lichess.org/paste?pgn=";
            var escapedPGN = UnityWebRequest.EscapeURL(pgn);
            var url = baseUrl + escapedPGN;

            Application.OpenURL(url);
            var t = new TextEditor();
            t.text = pgn;
            t.SelectAll();
            t.Copy();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private void NotifyPlayerToMove()
        {
            //gameResult = GetGameState ();
            PrintGameResult(gameResult);

            if (gameResult == Result.Playing)
            {
                playerToMove = board.WhiteToMove ? whitePlayer : blackPlayer;
                playerToMove.NotifyTurnToMove();
            }
            else
            {
                Debug.Log("Game Over");
            }
        }

        private void PrintGameResult(Result result)
        {
            var subtitleSize = resultUI.fontSize * 0.75f;
            var subtitleSettings = $"<color=#787878> <size={subtitleSize}>";

            if (result == Result.Playing)
            {
                resultUI.text = "";
            }
            else if (result == Result.WhiteIsMated || result == Result.BlackIsMated)
            {
                resultUI.text = "Checkmate!";
            }
            else if (result == Result.FiftyMoveRule)
            {
                resultUI.text = "Draw";
                resultUI.text += subtitleSettings + "\n(50 move rule)";
            }
            else if (result == Result.Repetition)
            {
                resultUI.text = "Draw";
                resultUI.text += subtitleSettings + "\n(3-fold repetition)";
            }
            else if (result == Result.Stalemate)
            {
                resultUI.text = "Draw";
                resultUI.text += subtitleSettings + "\n(Stalemate)";
            }
            else if (result == Result.InsufficientMaterial)
            {
                resultUI.text = "Draw";
                resultUI.text += subtitleSettings + "\n(Insufficient material)";
            }
        }

        /*Result GetGameState () {
            MoveGenerator moveGenerator = new MoveGenerator ();
            var moves = moveGenerator.GenerateMoves (board);

            // Look for mate/stalemate
            if (moves.Count == 0) {
                if (moveGenerator.InCheck ()) {
                    return (board.WhiteToMove) ? Result.WhiteIsMated : Result.BlackIsMated;
                }
                return Result.Stalemate;
            }

            // Fifty move rule
            if (board.fiftyMoveCounter >= 100) {
                return Result.FiftyMoveRule;
            }

            // Threefold repetition
            int repCount = board.RepetitionPositionHistory.Count ((x => x == board.ZobristKey));
            if (repCount == 3) {
                return Result.Repetition;
            }

            // Look for insufficient material (not all cases implemented yet)
            int numPawns = board.pawns[Board.WhiteIndex].Count + board.pawns[Board.BlackIndex].Count;
            int numRooks = board.rooks[Board.WhiteIndex].Count + board.rooks[Board.BlackIndex].Count;
            int numQueens = board.queens[Board.WhiteIndex].Count + board.queens[Board.BlackIndex].Count;
            int numKnights = board.knights[Board.WhiteIndex].Count + board.knights[Board.BlackIndex].Count;
            int numBishops = board.bishops[Board.WhiteIndex].Count + board.bishops[Board.BlackIndex].Count;

            if (numPawns + numRooks + numQueens == 0) {
                if (numKnights == 1 || numBishops == 1) {
                    return Result.InsufficientMaterial;
                }
            }

            return Result.Playing;
        }*/

        private void CreatePlayer(ref Player player, PlayerType playerType)
        {
            if (player != null) player.onMoveChosen -= OnMoveChosen;

            if (playerType == PlayerType.Human)
                player = new HumanPlayer(board);
            else
                player = new AIPlayer(searchBoard, aiSettings);
            player.onMoveChosen += OnMoveChosen;
        }
    }
}