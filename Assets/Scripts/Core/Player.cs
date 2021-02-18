using System;

namespace Chess.Game
{
    public abstract class Player
    {
        public event Action<Move> onMoveChosen;

        public abstract void Update();

        public abstract void NotifyTurnToMove();

        protected virtual void ChoseMove(Move move)
        {
            onMoveChosen?.Invoke(move);
        }
    }
}