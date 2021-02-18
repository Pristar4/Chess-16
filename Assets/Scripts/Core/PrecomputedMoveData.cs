using System;
using System.Collections.Generic;

namespace Chess
{
    using static Math;

    public static class PrecomputedMoveData
    {
        // First 4 are orthogonal, last 4 are diagonals (N, S, W, E, NW, SE, NE, SW)
        public static readonly int[] directionOffsets = {8, -8, -1, 1, 7, -7, 9, -9};

        // Stores number of moves available in each of the 8 directions for every square on the board
        // Order of directions is: N, S, W, E, NW, SE, NE, SW
        // So for example, if availableSquares[0][1] == 7...
        // that means that there are 7 squares to the north of b1 (the square with index 1 in board array)
        public static readonly int[][] numSquaresToEdge;

        // Stores array of indices for each square a knight can land on from any square on the board
        // So for example, knightMoves[0] is equal to {10, 17}, meaning a knight on a1 can jump to c2 and b3
        public static readonly byte[][] knightMoves;
        public static readonly byte[][] kingMoves;

        // Pawn attack directions for white and black (NW, NE; SW SE)
        public static readonly byte[][] pawnAttackDirections =
        {
            new byte[] {4, 6},
            new byte[] {7, 5}
        };

        public static readonly int[][] pawnAttacksWhite;
        public static readonly int[][] pawnAttacksBlack;
        public static readonly int[] directionLookup;

        public static readonly ulong[] kingAttackBitboards;
        public static readonly ulong[] knightAttackBitboards;
        public static readonly ulong[][] pawnAttackBitboards;

        public static readonly ulong[] rookMoves;
        public static readonly ulong[] bishopMoves;
        public static readonly ulong[] queenMoves;

        // Aka manhattan distance (answers how many moves for a rook to get from square a to square b)
        public static int[,] orthogonalDistance;

        // Aka chebyshev distance (answers how many moves for a king to get from square a to square b)
        public static int[,] kingDistance;
        public static int[] centreManhattanDistance;

        // Initialize lookup data
        static PrecomputedMoveData()
        {
            pawnAttacksWhite = new int[64][];
            pawnAttacksBlack = new int[64][];
            numSquaresToEdge = new int[8][];
            knightMoves = new byte[64][];
            kingMoves = new byte[64][];
            numSquaresToEdge = new int[64][];

            rookMoves = new ulong[64];
            bishopMoves = new ulong[64];
            queenMoves = new ulong[64];

            // Calculate knight jumps and available squares for each square on the board.
            // See comments by variable definitions for more info.
            int[] allKnightJumps = {15, 17, -17, -15, 10, -6, 6, -10};
            knightAttackBitboards = new ulong[64];
            kingAttackBitboards = new ulong[64];
            pawnAttackBitboards = new ulong[64][];

            for (var squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                var y = squareIndex / 8;
                var x = squareIndex - y * 8;

                var north = 7 - y;
                var south = y;
                var west = x;
                var east = 7 - x;
                numSquaresToEdge[squareIndex] = new int[8];
                numSquaresToEdge[squareIndex][0] = north;
                numSquaresToEdge[squareIndex][1] = south;
                numSquaresToEdge[squareIndex][2] = west;
                numSquaresToEdge[squareIndex][3] = east;
                numSquaresToEdge[squareIndex][4] = Min(north, west);
                numSquaresToEdge[squareIndex][5] = Min(south, east);
                numSquaresToEdge[squareIndex][6] = Min(north, east);
                numSquaresToEdge[squareIndex][7] = Min(south, west);

                // Calculate all squares knight can jump to from current square
                var legalKnightJumps = new List<byte>();
                ulong knightBitboard = 0;
                foreach (var knightJumpDelta in allKnightJumps)
                {
                    var knightJumpSquare = squareIndex + knightJumpDelta;
                    if (knightJumpSquare >= 0 && knightJumpSquare < 64)
                    {
                        var knightSquareY = knightJumpSquare / 8;
                        var knightSquareX = knightJumpSquare - knightSquareY * 8;
                        // Ensure knight has moved max of 2 squares on x/y axis (to reject indices that have wrapped around side of board)
                        var maxCoordMoveDst = Max(Abs(x - knightSquareX), Abs(y - knightSquareY));
                        if (maxCoordMoveDst == 2)
                        {
                            legalKnightJumps.Add((byte) knightJumpSquare);
                            knightBitboard |= 1ul << knightJumpSquare;
                        }
                    }
                }

                knightMoves[squareIndex] = legalKnightJumps.ToArray();
                knightAttackBitboards[squareIndex] = knightBitboard;

                // Calculate all squares king can move to from current square (not including castling)
                var legalKingMoves = new List<byte>();
                foreach (var kingMoveDelta in directionOffsets)
                {
                    var kingMoveSquare = squareIndex + kingMoveDelta;
                    if (kingMoveSquare >= 0 && kingMoveSquare < 64)
                    {
                        var kingSquareY = kingMoveSquare / 8;
                        var kingSquareX = kingMoveSquare - kingSquareY * 8;
                        // Ensure king has moved max of 1 square on x/y axis (to reject indices that have wrapped around side of board)
                        var maxCoordMoveDst = Max(Abs(x - kingSquareX), Abs(y - kingSquareY));
                        if (maxCoordMoveDst == 1)
                        {
                            legalKingMoves.Add((byte) kingMoveSquare);
                            kingAttackBitboards[squareIndex] |= 1ul << kingMoveSquare;
                        }
                    }
                }

                kingMoves[squareIndex] = legalKingMoves.ToArray();

                // Calculate legal pawn captures for white and black
                var pawnCapturesWhite = new List<int>();
                var pawnCapturesBlack = new List<int>();
                pawnAttackBitboards[squareIndex] = new ulong[2];
                if (x > 0)
                {
                    if (y < 7)
                    {
                        pawnCapturesWhite.Add(squareIndex + 7);
                        pawnAttackBitboards[squareIndex][Board.WhiteIndex] |= 1ul << (squareIndex + 7);
                    }

                    if (y > 0)
                    {
                        pawnCapturesBlack.Add(squareIndex - 9);
                        pawnAttackBitboards[squareIndex][Board.BlackIndex] |= 1ul << (squareIndex - 9);
                    }
                }

                if (x < 7)
                {
                    if (y < 7)
                    {
                        pawnCapturesWhite.Add(squareIndex + 9);
                        pawnAttackBitboards[squareIndex][Board.WhiteIndex] |= 1ul << (squareIndex + 9);
                    }

                    if (y > 0)
                    {
                        pawnCapturesBlack.Add(squareIndex - 7);
                        pawnAttackBitboards[squareIndex][Board.BlackIndex] |= 1ul << (squareIndex - 7);
                    }
                }

                pawnAttacksWhite[squareIndex] = pawnCapturesWhite.ToArray();
                pawnAttacksBlack[squareIndex] = pawnCapturesBlack.ToArray();

                // Rook moves
                for (var directionIndex = 0; directionIndex < 4; directionIndex++)
                {
                    var currentDirOffset = directionOffsets[directionIndex];
                    for (var n = 0; n < numSquaresToEdge[squareIndex][directionIndex]; n++)
                    {
                        var targetSquare = squareIndex + currentDirOffset * (n + 1);
                        rookMoves[squareIndex] |= 1ul << targetSquare;
                    }
                }

                // Bishop moves
                for (var directionIndex = 4; directionIndex < 8; directionIndex++)
                {
                    var currentDirOffset = directionOffsets[directionIndex];
                    for (var n = 0; n < numSquaresToEdge[squareIndex][directionIndex]; n++)
                    {
                        var targetSquare = squareIndex + currentDirOffset * (n + 1);
                        bishopMoves[squareIndex] |= 1ul << targetSquare;
                    }
                }

                queenMoves[squareIndex] = rookMoves[squareIndex] | bishopMoves[squareIndex];
            }

            directionLookup = new int[127];
            for (var i = 0; i < 127; i++)
            {
                var offset = i - 63;
                var absOffset = Abs(offset);
                var absDir = 1;
                if (absOffset % 9 == 0)
                    absDir = 9;
                else if (absOffset % 8 == 0)
                    absDir = 8;
                else if (absOffset % 7 == 0) absDir = 7;

                directionLookup[i] = absDir * Sign(offset);
            }

            // Distance lookup
            orthogonalDistance = new int[64, 64];
            kingDistance = new int[64, 64];
            centreManhattanDistance = new int[64];
            for (var squareA = 0; squareA < 64; squareA++)
            {
                var coordA = BoardRepresentation.CoordFromIndex(squareA);
                var fileDstFromCentre = Max(3 - coordA.fileIndex, coordA.fileIndex - 4);
                var rankDstFromCentre = Max(3 - coordA.rankIndex, coordA.rankIndex - 4);
                centreManhattanDistance[squareA] = fileDstFromCentre + rankDstFromCentre;

                for (var squareB = 0; squareB < 64; squareB++)
                {
                    var coordB = BoardRepresentation.CoordFromIndex(squareB);
                    var rankDistance = Abs(coordA.rankIndex - coordB.rankIndex);
                    var fileDistance = Abs(coordA.fileIndex - coordB.fileIndex);
                    orthogonalDistance[squareA, squareB] = fileDistance + rankDistance;
                    kingDistance[squareA, squareB] = Max(fileDistance, rankDistance);
                }
            }
        }

        public static int NumRookMovesToReachSquare(int startSquare, int targetSquare)
        {
            return orthogonalDistance[startSquare, targetSquare];
        }

        public static int NumKingMovesToReachSquare(int startSquare, int targetSquare)
        {
            return kingDistance[startSquare, targetSquare];
        }
    }
}