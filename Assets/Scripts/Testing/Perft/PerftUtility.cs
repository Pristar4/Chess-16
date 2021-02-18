namespace Chess.Testing
{
    public static class PerftUtility
    {
        // Move name matching stockfish command line output format (for perft comparison)
        public static string MoveName(Move move)
        {
            var from = BoardRepresentation.SquareNameFromIndex(move.StartSquare);
            var to = BoardRepresentation.SquareNameFromIndex(move.TargetSquare);
            var promotion = "";
            var specialMoveFlag = move.MoveFlag;

            switch (specialMoveFlag)
            {
                case Move.Flag.PromoteToRook:
                    promotion += "r";
                    break;
                case Move.Flag.PromoteToKnight:
                    promotion += "n";
                    break;
                case Move.Flag.PromoteToBishop:
                    promotion += "b";
                    break;
                case Move.Flag.PromoteToQueen:
                    promotion += "q";
                    break;
            }

            return from + to + promotion;
        }
    }
}