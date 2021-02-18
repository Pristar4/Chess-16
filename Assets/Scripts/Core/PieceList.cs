public class PieceList
{
    // Map to go from index of a square, to the index in the occupiedSquares array where that square is stored
    private readonly int[] map;

    // Indices of squares occupied by given piece type (only elements up to Count are valid, the rest are unused/garbage)
    public int[] occupiedSquares;

    public PieceList(int maxPieceCount = 32)
    {
        occupiedSquares = new int[maxPieceCount];
        map = new int[256];
        Count = 0;
    }

    public int Count { get; private set; }

    public int this[int index] => occupiedSquares[index];

    public void AddPieceAtSquare(int square)
    {
        //occupiedSquares[numPieces] = square;
        map[square] = Count;
        Count++;
    }

    public void RemovePieceAtSquare(int square)
    {
        var pieceIndex = map[square]; // get the index of this element in the occupiedSquares array
        occupiedSquares[pieceIndex] =
            occupiedSquares[Count - 1]; // move last element in array to the place of the removed element
        map[occupiedSquares[pieceIndex]] =
            pieceIndex; // update map to point to the moved element's new location in the array
        Count--;
    }

    public void MovePiece(int startSquare, int targetSquare)
    {
        var pieceIndex = map[startSquare]; // get the index of this element in the occupiedSquares array
        occupiedSquares[pieceIndex] = targetSquare;
        map[targetSquare] = pieceIndex;
    }
}