using System.Collections.Generic;

namespace Chess
{
    public static class FenUtility
    {
        public const string startFen =
            "rrrrrnbqkbnrrrrr/pppppppppppppppp/16/16/16/16/16/16/16/16/16/16/16/16/PPPPPPPPPPPPPPPP/RRRRRNBQKBNRRRRR w KQkq - 0 1";

        public static int boardSize = 16;

        private static readonly Dictionary<char, int> pieceTypeFromSymbol = new Dictionary<char, int>
        {
            ['k'] = Piece.King, ['p'] = Piece.Pawn, ['n'] = Piece.Knight, ['b'] = Piece.Bishop, ['r'] = Piece.Rook,
            ['q'] = Piece.Queen
        };

        // Load position from fen string
        public static LoadedPositionInfo PositionFromFen(string fen)
        {
            var loadedPositionInfo = new LoadedPositionInfo();
            var sections = fen.Split(' ');

            var file = 0;
            var rank = 15;

            foreach (var symbol in sections[0])
                if (symbol == '/')
                {
                    file = 0;
                    rank--;
                }
                else
                {
                    if (char.IsDigit(symbol))
                    {
                        file += (int) char.GetNumericValue(symbol);
                    }
                    else
                    {
                        var pieceColour = char.IsUpper(symbol) ? Piece.White : Piece.Black;
                        var pieceType = pieceTypeFromSymbol[char.ToLower(symbol)];
                        loadedPositionInfo.squares[rank * 16 + file] = pieceType | pieceColour;
                        file++;
                    }
                }

            loadedPositionInfo.whiteToMove = sections[1] == "w";

            var castlingRights = sections.Length > 2 ? sections[2] : "KQkq";
            loadedPositionInfo.whiteCastleKingside = castlingRights.Contains("K");
            loadedPositionInfo.whiteCastleQueenside = castlingRights.Contains("Q");
            loadedPositionInfo.blackCastleKingside = castlingRights.Contains("k");
            loadedPositionInfo.blackCastleQueenside = castlingRights.Contains("q");

            if (sections.Length > 3)
            {
                var enPassantFileName = sections[3][0].ToString();
                if (BoardRepresentation.fileNames.Contains(enPassantFileName))
                    loadedPositionInfo.epFile = BoardRepresentation.fileNames.IndexOf(enPassantFileName) + 1;
            }

            // Half-move clock
            if (sections.Length > 4) int.TryParse(sections[4], out loadedPositionInfo.plyCount);
            return loadedPositionInfo;
        }

        // Get the fen string of the current position
        public static string CurrentFen(Board board)
        {
            var fen = "";
            for (var rank = 15; rank >= 0; rank--)
            {
                var numEmptyFiles = 0;
                for (var file = 0; file < 16; file++)
                {
                    var i = rank * 16 + file;
                    var piece = board.Square[i];
                    if (piece != 0)
                    {
                        if (numEmptyFiles != 0)
                        {
                            fen += numEmptyFiles;
                            numEmptyFiles = 0;
                        }

                        var isBlack = Piece.IsColour(piece, Piece.Black);
                        var pieceType = Piece.PieceType(piece);
                        var pieceChar = ' ';
                        switch (pieceType)
                        {
                            case Piece.Rook:
                                pieceChar = 'R';
                                break;
                            case Piece.Knight:
                                pieceChar = 'N';
                                break;
                            case Piece.Bishop:
                                pieceChar = 'B';
                                break;
                            case Piece.Queen:
                                pieceChar = 'Q';
                                break;
                            case Piece.King:
                                pieceChar = 'K';
                                break;
                            case Piece.Pawn:
                                pieceChar = 'P';
                                break;
                        }

                        fen += isBlack ? pieceChar.ToString().ToLower() : pieceChar.ToString();
                    }
                    else
                    {
                        numEmptyFiles++;
                    }
                }

                if (numEmptyFiles != 0) fen += numEmptyFiles;
                if (rank != 0) fen += '/';
            }

            // Side to move
            fen += ' ';
            fen += board.WhiteToMove ? 'w' : 'b';

            // Castling
            var whiteKingside = (board.currentGameState & 1) == 1;
            var whiteQueenside = ((board.currentGameState >> 1) & 1) == 1;
            var blackKingside = ((board.currentGameState >> 2) & 1) == 1;
            var blackQueenside = ((board.currentGameState >> 3) & 1) == 1;
            fen += ' ';
            fen += whiteKingside ? "K" : "";
            fen += whiteQueenside ? "Q" : "";
            fen += blackKingside ? "k" : "";
            fen += blackQueenside ? "q" : "";
            fen += (board.currentGameState & 15) == 0 ? "-" : "";

            // En-passant
            fen += ' ';
            var epFile = (int) (board.currentGameState >> 4) & 15;
            if (epFile == 0)
            {
                fen += '-';
            }
            else
            {
                var fileName = BoardRepresentation.fileNames[epFile - 1].ToString();
                var epRank = board.WhiteToMove ? 6 : 3;
                fen += fileName + epRank;
            }

            // 50 move counter
            fen += ' ';
            fen += board.fiftyMoveCounter;

            // Full-move count (should be one at start, and increase after each move by black)
            fen += ' ';
            fen += board.plyCount / 2 + 1;

            return fen;
        }

        public class LoadedPositionInfo
        {
            public bool blackCastleKingside;
            public bool blackCastleQueenside;
            public int epFile;
            public int plyCount;
            public int[] squares;
            public bool whiteCastleKingside;
            public bool whiteCastleQueenside;
            public bool whiteToMove;

            public LoadedPositionInfo()
            {
                squares = new int[256];
            }
        }
    }
}