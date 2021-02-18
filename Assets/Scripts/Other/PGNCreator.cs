namespace Chess
{
    public static class PGNCreator
    {
        public static string CreatePGN(Move[] moves)
        {
            var pgn = "";
            var board = new Board();
            board.LoadStartPosition();

            for (var plyCount = 0; plyCount < moves.Length; plyCount++)
            {
                var moveString = NotationFromMove(board, moves[plyCount]);
                board.MakeMove(moves[plyCount]);

                if (plyCount % 2 == 0) pgn += plyCount / 2 + 1 + ". ";
                pgn += moveString + " ";
            }

            return pgn;
        }

        public static string NotationFromMove(string currentFen, Move move)
        {
            var board = new Board();
            board.LoadPosition(currentFen);
            return NotationFromMove(board, move);
        }

        private static string NotationFromMove(Board board, Move move)
        {
            var moveGen = new MoveGenerator();

            var movePieceType = Piece.PieceType(board.Square[move.StartSquare]);
            var capturedPieceType = Piece.PieceType(board.Square[move.TargetSquare]);

            if (move.MoveFlag == Move.Flag.Castling)
            {
                var delta = move.TargetSquare - move.StartSquare;
                if (delta == 2)
                    return "O-O";
                if (delta == -2) return "O-O-O";
            }

            var moveNotation = GetSymbolFromPieceType(movePieceType);

            // check if any ambiguity exists in notation (e.g if e2 can be reached via Nfe2 and Nbe2)
            if (movePieceType != Piece.Pawn && movePieceType != Piece.King)
            {
                var allMoves = moveGen.GenerateMoves(board);

                foreach (var altMove in allMoves)
                    if (altMove.StartSquare != move.StartSquare && altMove.TargetSquare == move.TargetSquare
                    ) // if moving to same square from different square
                        if (Piece.PieceType(board.Square[altMove.StartSquare]) == movePieceType)
                        {
                            // same piece type
                            var fromFileIndex = BoardRepresentation.FileIndex(move.StartSquare);
                            var alternateFromFileIndex = BoardRepresentation.FileIndex(altMove.StartSquare);
                            var fromRankIndex = BoardRepresentation.RankIndex(move.StartSquare);
                            var alternateFromRankIndex = BoardRepresentation.RankIndex(altMove.StartSquare);

                            if (fromFileIndex != alternateFromFileIndex)
                            {
                                // pieces on different files, thus ambiguity can be resolved by specifying file
                                moveNotation += BoardRepresentation.fileNames[fromFileIndex];
                                break; // ambiguity resolved
                            }

                            if (fromRankIndex != alternateFromRankIndex)
                            {
                                moveNotation += BoardRepresentation.rankNames[fromRankIndex];
                                break; // ambiguity resolved
                            }
                        }
            }

            if (capturedPieceType != 0)
            {
                // add 'x' to indicate capture
                if (movePieceType == Piece.Pawn)
                    moveNotation += BoardRepresentation.fileNames[BoardRepresentation.FileIndex(move.StartSquare)];
                moveNotation += "x";
            }
            else
            {
                // check if capturing ep
                if (move.MoveFlag == Move.Flag.EnPassantCapture)
                    moveNotation += BoardRepresentation.fileNames[BoardRepresentation.FileIndex(move.StartSquare)] +
                                    "x";
            }

            moveNotation += BoardRepresentation.fileNames[BoardRepresentation.FileIndex(move.TargetSquare)];
            moveNotation += BoardRepresentation.rankNames[BoardRepresentation.RankIndex(move.TargetSquare)];

            // add promotion piece
            if (move.IsPromotion)
            {
                var promotionPieceType = move.PromotionPieceType;
                moveNotation += "=" + GetSymbolFromPieceType(promotionPieceType);
            }

            board.MakeMove(move, true);
            var legalResponses = moveGen.GenerateMoves(board);
            // add check/mate symbol if applicable
            if (moveGen.InCheck())
            {
                if (legalResponses.Count == 0)
                    moveNotation += "#";
                else
                    moveNotation += "+";
            }

            board.UnmakeMove(move, true);

            return moveNotation;
        }

        private static string GetSymbolFromPieceType(int pieceType)
        {
            switch (pieceType)
            {
                case Piece.Rook:
                    return "R";
                case Piece.Knight:
                    return "N";
                case Piece.Bishop:
                    return "B";
                case Piece.Queen:
                    return "Q";
                case Piece.King:
                    return "K";
                default:
                    return "";
            }
        }
    }
}