using System.Collections.Generic;

namespace Chess
{
    using static PrecomputedMoveData;
    using static BoardRepresentation;

    public class PseudoLegalMoveGenerator
    {
        private Board board;
        private int friendlyColour;
        private int friendlyColourIndex;
        private int friendlyKingSquare;

        private bool genQuiets;
        private bool genUnderpromotions;
        private bool isWhiteToMove;

        // ---- Instance variables ----
        private List<Move> moves;
        private int opponentColour;
        private int opponentColourIndex;

        private bool HasKingsideCastleRight
        {
            get
            {
                var mask = board.WhiteToMove ? 1 : 4;
                return (board.currentGameState & mask) != 0;
            }
        }

        private bool HasQueensideCastleRight
        {
            get
            {
                var mask = board.WhiteToMove ? 2 : 16;
                return (board.currentGameState & mask) != 0;
            }
        }

        // Generates list of legal moves in current position.
        // Quiet moves (non captures) can optionally be excluded. This is used in quiescence search.
        public List<Move> GenerateMoves(Board board, bool includeQuietMoves = true, bool includeUnderPromotions = true)
        {
            this.board = board;
            genQuiets = includeQuietMoves;
            genUnderpromotions = includeUnderPromotions;
            Init();
            GenerateKingMoves();

            GenerateSlidingMoves();
            GenerateKnightMoves();
            GeneratePawnMoves();

            return moves;
        }

        public bool Illegal()
        {
            return SquareAttacked(board.KingSquare[1 - board.ColourToMoveIndex], board.ColourToMove);
        }

        public bool SquareAttacked(int attackSquare, int attackerColour)
        {
            var attackerColourIndex = attackerColour == Piece.White ? Board.WhiteIndex : Board.BlackIndex;
            var friendlyColourIndex = 1 - attackerColourIndex;
            var friendlyColour = attackerColour == Piece.White ? Piece.Black : Piece.White;

            var startDirIndex = 0;
            var endDirIndex = 16;

            var opponentKingSquare = board.KingSquare[attackerColourIndex];
            if (kingDistance[opponentKingSquare, attackSquare] == 1) return true;

            if (board.queens[attackerColourIndex].Count == 0)
            {
                startDirIndex = board.rooks[attackerColourIndex].Count > 0 ? 0 : 4;
                endDirIndex = board.bishops[attackerColourIndex].Count > 0 ? 15 : 4;
            }

            for (var dir = startDirIndex; dir < endDirIndex; dir++)
            {
                var isDiagonal = dir > 3;

                var n = numSquaresToEdge[attackSquare][dir];
                var directionOffset = directionOffsets[dir];

                for (var i = 0; i < n; i++)
                {
                    var squareIndex = attackSquare + directionOffset * (i + 1);
                    var piece = board.Square[squareIndex];

                    // This square contains a piece
                    if (piece != Piece.None)
                    {
                        if (Piece.IsColour(piece, friendlyColour))
                            break;
                        // This square contains an enemy piece

                        var pieceType = Piece.PieceType(piece);

                        // Check if piece is in bitmask of pieces able to move in current direction
                        if (isDiagonal && Piece.IsBishopOrQueen(pieceType) ||
                            !isDiagonal && Piece.IsRookOrQueen(pieceType))
                            return true;
                        break;
                    }
                }
            }

            // Knight attacks
            var knightAttackSquares = knightMoves[attackSquare];
            for (var i = 0; i < knightAttackSquares.Length; i++)
                if (board.Square[knightAttackSquares[i]] == (Piece.Knight | attackerColour))
                    return true;

            // check if enemy pawn is controlling this square
            for (var i = 0;
                i < 2;
                i++) // Check if square exists diagonal to friendly king from which enemy pawn could be attacking it
                if (numSquaresToEdge[attackSquare][pawnAttackDirections[friendlyColourIndex][i]] > 0)
                {
                    // move in direction friendly pawns attack to get square from which enemy pawn would attack
                    var s = attackSquare + directionOffsets[pawnAttackDirections[friendlyColourIndex][i]];

                    var piece = board.Square[s];
                    if (piece == (Piece.Pawn | attackerColour)) // is enemy pawn
                        return true;
                }

            return false;
        }

        // Note, this will only return correct value after GenerateMoves() has been called in the current position
        public bool InCheck()
        {
            return false;
            //return SquareAttacked (friendlyKingSquare, board.ColourToMoveIndex);
        }

        private void Init()
        {
            moves = new List<Move>(256);

            isWhiteToMove = board.ColourToMove == Piece.White;
            friendlyColour = board.ColourToMove;
            opponentColour = board.OpponentColour;
            friendlyKingSquare = board.KingSquare[board.ColourToMoveIndex];
            friendlyColourIndex = board.WhiteToMove ? Board.WhiteIndex : Board.BlackIndex;
            opponentColourIndex = 1 - friendlyColourIndex;
        }

        private void GenerateKingMoves()
        {
            for (var i = 0; i < kingMoves[friendlyKingSquare].Length; i++)
            {
                int targetSquare = kingMoves[friendlyKingSquare][i];
                var pieceOnTargetSquare = board.Square[targetSquare];

                // Skip squares occupied by friendly pieces
                if (Piece.IsColour(pieceOnTargetSquare, friendlyColour)) continue;

                var isCapture = Piece.IsColour(pieceOnTargetSquare, opponentColour);
                if (!isCapture
                    ) // King can't move to square marked as under enemy control, unless he is capturing that piece
                    // Also skip if not generating quiet moves
                    if (!genQuiets)
                        continue;

                // Safe for king to move to this square

                moves.Add(new Move(friendlyKingSquare, targetSquare));

                // Castling:
                if (!isCapture && !SquareAttacked(friendlyKingSquare, opponentColour))
                {
                    // Castle kingside
                    if ((targetSquare == f1 || targetSquare == f8) && HasKingsideCastleRight)
                    {
                        if (!SquareAttacked(targetSquare, opponentColour))
                        {
                            var castleKingsideSquare = targetSquare + 1;
                            if (board.Square[castleKingsideSquare] == Piece.None)
                                moves.Add(new Move(friendlyKingSquare, castleKingsideSquare, Move.Flag.Castling));
                        }
                    }
                    // Castle queenside
                    else if ((targetSquare == d1 || targetSquare == d8) && HasQueensideCastleRight)
                    {
                        if (!SquareAttacked(targetSquare, opponentColour))
                        {
                            var castleQueensideSquare = targetSquare - 1;
                            if (board.Square[castleQueensideSquare] == Piece.None &&
                                board.Square[castleQueensideSquare - 1] == Piece.None)
                                moves.Add(new Move(friendlyKingSquare, castleQueensideSquare, Move.Flag.Castling));
                        }
                    }
                }
            }
        }

        private void GenerateSlidingMoves()
        {
            var rooks = board.rooks[friendlyColourIndex];
            for (var i = 0; i < rooks.Count; i++) GenerateSlidingPieceMoves(rooks[i], 0, 4);

            var bishops = board.bishops[friendlyColourIndex];
            for (var i = 0; i < bishops.Count; i++) GenerateSlidingPieceMoves(bishops[i], 4, 8);

            var queens = board.queens[friendlyColourIndex];
            for (var i = 0; i < queens.Count; i++) GenerateSlidingPieceMoves(queens[i], 0, 8);
        }

        private void GenerateSlidingPieceMoves(int startSquare, int startDirIndex, int endDirIndex)
        {
            for (var directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
            {
                var currentDirOffset = directionOffsets[directionIndex];

                for (var n = 0; n < numSquaresToEdge[startSquare][directionIndex]; n++)
                {
                    var targetSquare = startSquare + currentDirOffset * (n + 1);
                    var targetSquarePiece = board.Square[targetSquare];

                    // Blocked by friendly piece, so stop looking in this direction
                    if (Piece.IsColour(targetSquarePiece, friendlyColour)) break;
                    var isCapture = targetSquarePiece != Piece.None;

                    if (genQuiets || isCapture) moves.Add(new Move(startSquare, targetSquare));

                    // If square not empty, can't move any further in this direction
                    // Also, if this move blocked a check, further moves won't block the check
                    if (isCapture) break;
                }
            }
        }

        private void GenerateKnightMoves()
        {
            var myKnights = board.knights[friendlyColourIndex];

            for (var i = 0; i < myKnights.Count; i++)
            {
                var startSquare = myKnights[i];

                for (var knightMoveIndex = 0; knightMoveIndex < knightMoves[startSquare].Length; knightMoveIndex++)
                {
                    int targetSquare = knightMoves[startSquare][knightMoveIndex];
                    var targetSquarePiece = board.Square[targetSquare];
                    var isCapture = Piece.IsColour(targetSquarePiece, opponentColour);
                    if (genQuiets || isCapture)
                    {
                        // Skip if square contains friendly piece, or if in check and knight is not interposing/capturing checking piece
                        if (Piece.IsColour(targetSquarePiece, friendlyColour)) continue;
                        moves.Add(new Move(startSquare, targetSquare));
                    }
                }
            }
        }

        private void GeneratePawnMoves()
        {
            var myPawns = board.pawns[friendlyColourIndex];
            var pawnOffset = friendlyColour == Piece.White ? 16 : -16;
            var startRank = board.WhiteToMove ? 1 : 6;
            var finalRankBeforePromotion = board.WhiteToMove ? 6 : 1;

            var enPassantFile = ((int) (board.currentGameState >> 4) & 15) - 1;
            var enPassantSquare = -1;
            if (enPassantFile != -1) enPassantSquare = 16 * (board.WhiteToMove ? 5 : 2) + enPassantFile;

            for (var i = 0; i < myPawns.Count; i++)
            {
                var startSquare = myPawns[i];
                var rank = RankIndex(startSquare);
                var oneStepFromPromotion = rank == finalRankBeforePromotion;

                if (genQuiets)
                {
                    var squareOneForward = startSquare + pawnOffset;

                    // Square ahead of pawn is empty: forward moves
                    if (board.Square[squareOneForward] == Piece.None)
                    {
                        // Pawn not pinned, or is moving along line of pin

                        if (oneStepFromPromotion)
                            MakePromotionMoves(startSquare, squareOneForward);
                        else
                            moves.Add(new Move(startSquare, squareOneForward));

                        // Is on starting square (so can move two forward if not blocked)
                        if (rank == startRank)
                        {
                            var squareTwoForward = squareOneForward + pawnOffset;
                            if (board.Square[squareTwoForward] == Piece.None
                            ) // Not in check, or pawn is interposing checking piece

                                moves.Add(new Move(startSquare, squareTwoForward, Move.Flag.PawnTwoForward));
                        }
                    }
                }

                // Pawn captures.
                for (var j = 0; j < 2; j++) // Check if square exists diagonal to pawn
                    if (numSquaresToEdge[startSquare][pawnAttackDirections[friendlyColourIndex][j]] > 0)
                    {
                        // move in direction friendly pawns attack to get square from which enemy pawn would attack
                        var pawnCaptureDir = directionOffsets[pawnAttackDirections[friendlyColourIndex][j]];
                        var targetSquare = startSquare + pawnCaptureDir;
                        var targetPiece = board.Square[targetSquare];

                        // Regular capture
                        if (Piece.IsColour(targetPiece, opponentColour))
                        {
                            if (oneStepFromPromotion)
                                MakePromotionMoves(startSquare, targetSquare);
                            else
                                moves.Add(new Move(startSquare, targetSquare));
                        }

                        // Capture en-passant
                        if (targetSquare == enPassantSquare)
                        {
                            var epCapturedPawnSquare = targetSquare + (board.WhiteToMove ? -16 : 16);

                            moves.Add(new Move(startSquare, targetSquare, Move.Flag.EnPassantCapture));
                        }
                    }
            }
        }

        private void MakePromotionMoves(int fromSquare, int toSquare)
        {
            moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToQueen));
            if (genUnderpromotions)
            {
                moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToKnight));
                moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToRook));
                moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToBishop));
            }
        }
    }
}