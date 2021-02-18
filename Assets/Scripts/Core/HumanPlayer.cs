using UnityEngine;

namespace Chess.Game
{
    public class HumanPlayer : Player
    {
        public enum InputState
        {
            None,
            PieceSelected,
            DraggingPiece
        }

        private readonly Board board;

        private readonly BoardUI boardUI;
        private readonly Camera cam;

        private InputState currentState;
        private Coord selectedPieceSquare;

        public HumanPlayer(Board board)
        {
            boardUI = Object.FindObjectOfType<BoardUI>();
            cam = Camera.main;
            this.board = board;
        }

        public override void NotifyTurnToMove()
        {
        }

        public override void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

            if (currentState == InputState.None)
                HandlePieceSelection(mousePos);
            else if (currentState == InputState.DraggingPiece)
                HandleDragMovement(mousePos);
            else if (currentState == InputState.PieceSelected) HandlePointAndClickMovement(mousePos);

            if (Input.GetMouseButtonDown(1)) CancelPieceSelection();
        }

        private void HandlePointAndClickMovement(Vector2 mousePos)
        {
            if (Input.GetMouseButton(0)) HandlePiecePlacement(mousePos);
        }

        private void HandleDragMovement(Vector2 mousePos)
        {
            boardUI.DragPiece(selectedPieceSquare, mousePos);
            // If mouse is released, then try place the piece
            if (Input.GetMouseButtonUp(0)) HandlePiecePlacement(mousePos);
        }

        private void HandlePiecePlacement(Vector2 mousePos)
        {
            Coord targetSquare;
            if (boardUI.TryGetSquareUnderMouse(mousePos, out targetSquare))
            {
                if (targetSquare.Equals(selectedPieceSquare))
                {
                    boardUI.ResetPiecePosition(selectedPieceSquare);
                    if (currentState == InputState.DraggingPiece)
                    {
                        currentState = InputState.PieceSelected;
                    }
                    else
                    {
                        currentState = InputState.None;
                        boardUI.DeselectSquare(selectedPieceSquare);
                    }
                }
                else
                {
                    var targetIndex =
                        BoardRepresentation.IndexFromCoord(targetSquare.fileIndex, targetSquare.rankIndex);
                    if (Piece.IsColour(board.Square[targetIndex], board.ColourToMove) && board.Square[targetIndex] != 0)
                    {
                        CancelPieceSelection();
                        HandlePieceSelection(mousePos);
                    }
                    else
                    {
                        TryMakeMove(selectedPieceSquare, targetSquare);
                    }
                }
            }
            else
            {
                CancelPieceSelection();
            }
        }

        private void CancelPieceSelection()
        {
            if (currentState != InputState.None)
            {
                currentState = InputState.None;
                boardUI.DeselectSquare(selectedPieceSquare);
                boardUI.ResetPiecePosition(selectedPieceSquare);
            }
        }

        private void TryMakeMove(Coord startSquare, Coord targetSquare)
        {
            var startIndex = BoardRepresentation.IndexFromCoord(startSquare);
            var targetIndex = BoardRepresentation.IndexFromCoord(targetSquare);
            var moveIsLegal = false;
            var chosenMove = new Move();

            var moveGenerator = new MoveGenerator();
            var wantsKnightPromotion = Input.GetKey(KeyCode.LeftAlt);

            var legalMoves = moveGenerator.GenerateMoves(board);
            for (var i = 0; i < legalMoves.Count; i++)
            {
                var legalMove = legalMoves[i];

                if (legalMove.StartSquare == startIndex && legalMove.TargetSquare == targetIndex)
                {
                    if (legalMove.IsPromotion)
                    {
                        if (legalMove.MoveFlag == Move.Flag.PromoteToQueen && wantsKnightPromotion) continue;
                        if (legalMove.MoveFlag != Move.Flag.PromoteToQueen && !wantsKnightPromotion) continue;
                    }

                    moveIsLegal = true;
                    chosenMove = legalMove;
                    //	Debug.Log (legalMove.PromotionPieceType);
                    break;
                }
            }

            if (moveIsLegal)
            {
                ChoseMove(chosenMove);
                currentState = InputState.None;
            }
            else
            {
                CancelPieceSelection();
            }
        }

        private void HandlePieceSelection(Vector2 mousePos)
        {
            if (Input.GetMouseButtonDown(0))
                if (boardUI.TryGetSquareUnderMouse(mousePos, out selectedPieceSquare))
                {
                    var index = BoardRepresentation.IndexFromCoord(selectedPieceSquare);
                    // If square contains a piece, select that piece for dragging
                    if (Piece.IsColour(board.Square[index], board.ColourToMove))
                    {
                        boardUI.HighlightLegalMoves(board, selectedPieceSquare);
                        boardUI.SelectSquare(selectedPieceSquare);
                        currentState = InputState.DraggingPiece;
                    }
                }
        }
    }
}