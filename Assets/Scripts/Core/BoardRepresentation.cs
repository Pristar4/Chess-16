namespace Chess
{
    public static class BoardRepresentation
    {
        public const string fileNames = "abcdefghijklmnop";
        public const string rankNames = "123456789abcdefg";
        public const int boardSize = 16;

        public const int a1 = 0;
        public const int b1 = 1;
        public const int c1 = 2;
        public const int d1 = 3;
        public const int e1 = 4;
        public const int f1 = 5;
        public const int g1 = 6;
        public const int h1 = 7;
        public const int i1 = 8;
        public const int j1 = 9;
        public const int k1 = 10;
        public const int l1 = 11;
        public const int m1 = 12;
        public const int n1 = 13;
        public const int o1 = 14;
        public const int p1 = 15;

        public const int a8 = 240;
        public const int b8 = 241;
        public const int c8 = 242;
        public const int d8 = 243;
        public const int e8 = 244;
        public const int f8 = 245;
        public const int g8 = 246;
        public const int h8 = 247;
        public const int i8 = 248;
        public const int j8 = 249;
        public const int k8 = 250;
        public const int l8 = 251;
        public const int m8 = 252;
        public const int n8 = 253;
        public const int o8 = 254;
        public const int p8 = 255;

        // Rank (0 to 7) of square 
        public static int RankIndex(int squareIndex)
        {
            return squareIndex >> 3;
        }

        // File (0 to 7) of square 
        public static int FileIndex(int squareIndex)
        {
            return squareIndex & 0b000111;
        }

        public static int IndexFromCoord(int fileIndex, int rankIndex)
        {
            return rankIndex * boardSize + fileIndex;
        }

        public static int IndexFromCoord(Coord coord)
        {
            return IndexFromCoord(coord.fileIndex, coord.rankIndex);
        }

        public static Coord CoordFromIndex(int squareIndex)
        {
            return new Coord(FileIndex(squareIndex), RankIndex(squareIndex));
        }

        public static bool LightSquare(int fileIndex, int rankIndex)
        {
            return (fileIndex + rankIndex) % 2 != 0;
        }

        public static string SquareNameFromCoordinate(int fileIndex, int rankIndex)
        {
            return fileNames[fileIndex] + "" + (rankIndex + 1);
        }

        public static string SquareNameFromIndex(int squareIndex)
        {
            return SquareNameFromCoordinate(CoordFromIndex(squareIndex));
        }

        public static string SquareNameFromCoordinate(Coord coord)
        {
            return SquareNameFromCoordinate(coord.fileIndex, coord.rankIndex);
        }
    }
}