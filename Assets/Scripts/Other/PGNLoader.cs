using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    using static BoardRepresentation;

    public static class PGNLoader
    {
        public static Move[] MovesFromPGN(string pgn, int maxPlyCount = int.MaxValue)
        {
            var algebraicMoves = new List<string>();

            var entries = pgn.Replace("\n", " ").Split(' ');
            for (var i = 0; i < entries.Length; i++)
            {
                // Reached move limit, so exit.
                // (This is used for example when creating book, where only interested in first n moves of game)
                if (algebraicMoves.Count == maxPlyCount) break;

                var entry = entries[i].Trim();

                if (entry.Contains(".") || entry == "1/2-1/2" || entry == "1-0" || entry == "0-1") continue;

                if (!string.IsNullOrEmpty(entry)) algebraicMoves.Add(entry);
            }

            return MovesFromAlgebraic(algebraicMoves.ToArray());
        }

        private static Move[] MovesFromAlgebraic(string[] algebraicMoves)
        {
            var board = new Board();
            board.LoadStartPosition();
            var moves = new List<Move>();

            for (var i = 0; i < algebraicMoves.Length; i++)
            {
                var move = MoveFromAlgebraic(board, algebraicMoves[i].Trim());
                if (move.IsInvalid)
                {
                    // move is illegal; discard and return moves up to this point
                    Debug.Log("illegal move in supplied pgn: " + algebraicMoves[i] + " move index: " + i);
                    var pgn = "";
                    foreach (var s in algebraicMoves) pgn += s + " ";
                    Debug.Log("problematic pgn: " + pgn);
                    moves.ToArray();
                }
                else
                {
                    moves.Add(move);
                }

                board.MakeMove(move);
            }

            return moves.ToArray();
        }

        private static Move MoveFromAlgebraic(Board board, string algebraicMove)
        {
            var moveGenerator = new MoveGenerator();

            // Remove unrequired info from move string
            algebraicMove = algebraicMove.Replace("+", "").Replace("#", "").Replace("x", "").Replace("-", "");
            var allMoves = moveGenerator.GenerateMoves(board);

            var move = new Move();

            foreach (var moveToTest in allMoves)
            {
                move = moveToTest;

                var moveFromIndex = move.StartSquare;
                var moveToIndex = move.TargetSquare;
                var movePieceType = Piece.PieceType(board.Square[moveFromIndex]);
                var fromCoord = CoordFromIndex(moveFromIndex);
                var toCoord = CoordFromIndex(moveToIndex);
                if (algebraicMove == "OO")
                {
                    // castle kingside
                    if (movePieceType == Piece.King && moveToIndex - moveFromIndex == 2) return move;
                }
                else if (algebraicMove == "OOO")
                {
                    // castle queenside
                    if (movePieceType == Piece.King && moveToIndex - moveFromIndex == -2) return move;
                }
                // Is pawn move if starts with any file indicator (e.g. 'e'4. Note that uppercase B is used for bishops) 
                else if (fileNames.Contains(algebraicMove[0].ToString()))
                {
                    if (movePieceType != Piece.Pawn) continue;
                    if (fileNames.IndexOf(algebraicMove[0]) == fromCoord.fileIndex)
                    {
                        // correct starting file
                        if (algebraicMove.Contains("="))
                        {
                            // is promotion
                            if (toCoord.rankIndex == 0 || toCoord.rankIndex == 7)
                            {
                                if (algebraicMove.Length == 5) // pawn is capturing to promote
                                {
                                    var targetFile = algebraicMove[1];
                                    if (fileNames.IndexOf(targetFile) != toCoord.fileIndex
                                    ) // Skip if not moving to correct file
                                        continue;
                                }

                                var promotionChar = algebraicMove[algebraicMove.Length - 1];

                                if (move.PromotionPieceType != GetPieceTypeFromSymbol(promotionChar))
                                    continue; // skip this move, incorrect promotion type

                                return move;
                            }
                        }
                        else
                        {
                            var targetFile = algebraicMove[algebraicMove.Length - 2];
                            var targetRank = algebraicMove[algebraicMove.Length - 1];

                            if (fileNames.IndexOf(targetFile) == toCoord.fileIndex) // correct ending file
                                if (targetRank.ToString() == (toCoord.rankIndex + 1).ToString()) // correct ending rank
                                    break;
                        }
                    }
                }
                else
                {
                    // regular piece move

                    var movePieceChar = algebraicMove[0];
                    if (GetPieceTypeFromSymbol(movePieceChar) != movePieceType)
                        continue; // skip this move, incorrect move piece type

                    var targetFile = algebraicMove[algebraicMove.Length - 2];
                    var targetRank = algebraicMove[algebraicMove.Length - 1];
                    if (fileNames.IndexOf(targetFile) == toCoord.fileIndex) // correct ending file
                        if (targetRank.ToString() == (toCoord.rankIndex + 1).ToString())
                        {
                            // correct ending rank

                            if (algebraicMove.Length == 4)
                            {
                                // addition char present for disambiguation (e.g. Nbd7 or R7e2)
                                var disambiguationChar = algebraicMove[1];

                                if (fileNames.Contains(disambiguationChar.ToString()))
                                {
                                    // is file disambiguation
                                    if (fileNames.IndexOf(disambiguationChar) != fromCoord.fileIndex
                                    ) // incorrect starting file
                                        continue;
                                }
                                else
                                {
                                    // is rank disambiguation
                                    if (disambiguationChar.ToString() != (fromCoord.rankIndex + 1).ToString()
                                    ) // incorrect starting rank
                                        continue;
                                }
                            }

                            break;
                        }
                }
            }

            return move;
        }

        private static int GetPieceTypeFromSymbol(char symbol)
        {
            switch (symbol)
            {
                case 'R':
                    return Piece.Rook;
                case 'N':
                    return Piece.Knight;
                case 'B':
                    return Piece.Bishop;
                case 'Q':
                    return Piece.Queen;
                case 'K':
                    return Piece.King;
                default:
                    return Piece.None;
            }
        }
    }
}