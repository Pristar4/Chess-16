using System.Collections.Generic;

namespace Chess
{
    public class MoveOrdering
    {
        private const int maxMoveCount = 218;

        private const int squareControlledByOpponentPawnPenalty = 350;
        private const int capturedPieceValueMultiplier = 10;
        private readonly Move invalidMove;

        private readonly MoveGenerator moveGenerator;

        private readonly int[] moveScores;
        private readonly TranspositionTable transpositionTable;

        public MoveOrdering(MoveGenerator moveGenerator, TranspositionTable tt)
        {
            moveScores = new int[maxMoveCount];
            this.moveGenerator = moveGenerator;
            transpositionTable = tt;
            invalidMove = Move.InvalidMove;
        }

        public void OrderMoves(Board board, List<Move> moves, bool useTT)
        {
            var hashMove = invalidMove;
            if (useTT) hashMove = transpositionTable.GetStoredMove();

            for (var i = 0; i < moves.Count; i++)
            {
                var score = 0;
                var movePieceType = Piece.PieceType(board.Square[moves[i].StartSquare]);
                var capturePieceType = Piece.PieceType(board.Square[moves[i].TargetSquare]);
                var flag = moves[i].MoveFlag;

                if (capturePieceType != Piece.None
                    ) // Order moves to try capturing the most valuable opponent piece with least valuable of own pieces first
                    // The capturedPieceValueMultiplier is used to make even 'bad' captures like QxP rank above non-captures
                    score = capturedPieceValueMultiplier * GetPieceValue(capturePieceType) -
                            GetPieceValue(movePieceType);

                if (movePieceType == Piece.Pawn)
                {
                    if (flag == Move.Flag.PromoteToQueen)
                        score += Evaluation.queenValue;
                    else if (flag == Move.Flag.PromoteToKnight)
                        score += Evaluation.knightValue;
                    else if (flag == Move.Flag.PromoteToRook)
                        score += Evaluation.rookValue;
                    else if (flag == Move.Flag.PromoteToBishop) score += Evaluation.bishopValue;
                }
                else
                {
                    // Penalize moving piece to a square attacked by opponent pawn
                    if (BitBoardUtility.ContainsSquare(moveGenerator.opponentPawnAttackMap, moves[i].TargetSquare))
                        score -= squareControlledByOpponentPawnPenalty;
                }

                if (Move.SameMove(moves[i], hashMove)) score += 10000;

                moveScores[i] = score;
            }

            Sort(moves);
        }

        private static int GetPieceValue(int pieceType)
        {
            switch (pieceType)
            {
                case Piece.Queen:
                    return Evaluation.queenValue;
                case Piece.Rook:
                    return Evaluation.rookValue;
                case Piece.Knight:
                    return Evaluation.knightValue;
                case Piece.Bishop:
                    return Evaluation.bishopValue;
                case Piece.Pawn:
                    return Evaluation.pawnValue;
                default:
                    return 0;
            }
        }

        private void Sort(List<Move> moves)
        {
            // Sort the moves list based on scores
            for (var i = 0; i < moves.Count - 1; i++)
            for (var j = i + 1; j > 0; j--)
            {
                var swapIndex = j - 1;
                if (moveScores[swapIndex] < moveScores[j])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                }
            }
        }
    }
}