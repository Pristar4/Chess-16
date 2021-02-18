using System;
using System.Collections.Generic;

namespace Chess
{
    [Serializable]
    public class Book
    {
        public Dictionary<ulong, BookPosition> bookPositions;

        public Book()
        {
            bookPositions = new Dictionary<ulong, BookPosition>();
        }

        public bool HasPosition(ulong positionKey)
        {
            return bookPositions.ContainsKey(positionKey);
        }

        public BookPosition GetBookPosition(ulong key)
        {
            return bookPositions[key];
        }

        public Move GetRandomBookMove(ulong key)
        {
            var p = bookPositions[key];
            var moves = new List<ushort>(p.numTimesMovePlayed.Keys).ToArray();
            var prng = new Random();
            var randomMove = moves[prng.Next(0, moves.Length)];
            return new Move(randomMove);
        }

        public Move GetRandomBookMoveWeighted(ulong key)
        {
            var p = bookPositions[key];
            var moves = new List<ushort>(p.numTimesMovePlayed.Keys).ToArray();
            var numTimesMovePlayed = new List<int>(p.numTimesMovePlayed.Values).ToArray();

            var moveWeights = new float[moves.Length];
            for (var i = 0; i < moveWeights.Length; i++) moveWeights[i] = numTimesMovePlayed[i];

            // Smooth weights to increase probability of rarer moves
            // (strength of 1 would make all moves equally likely)
            SmoothWeights(moveWeights, 0.5f);

            float sum = 0;
            for (var i = 0; i < moveWeights.Length; i++) sum += moveWeights[i];

            var moveProbabilitiesCumul = new float[moveWeights.Length];
            float previousProbability = 0;
            for (var i = 0; i < moveWeights.Length; i++)
            {
                moveProbabilitiesCumul[i] = previousProbability + moveWeights[i] / sum;
                previousProbability = moveProbabilitiesCumul[i];
            }

            var prng = new Random();
            var t = (float) prng.NextDouble();

            //for (int i = 0; i < moves.Length; i++) {
            //Debug.Log ((new Move (moves[i]).Name) + "  " + moveProbabilitiesCumul[i] + "  " + t);
            //}

            for (var i = 0; i < moves.Length; i++)
                if (t <= moveProbabilitiesCumul[i])
                    return new Move(moves[i]);

            return new Move(moves[0]);
        }

        private void SmoothWeights(float[] weights, float strength = 0.1f)
        {
            float sum = 0;
            for (var i = 0; i < weights.Length; i++) sum += weights[i];
            var avg = sum / weights.Length;

            for (var i = 0; i < weights.Length; i++)
            {
                var offsetFromAvg = avg - weights[i];
                weights[i] += offsetFromAvg * strength;
            }
        }

        public void Add(ulong positionKey, Move move)
        {
            if (!bookPositions.ContainsKey(positionKey)) bookPositions.Add(positionKey, new BookPosition());

            bookPositions[positionKey].AddMove(move);
        }

        public void Add(ulong positionKey, Move move, int numTimesPlayed)
        {
            if (!bookPositions.ContainsKey(positionKey)) bookPositions.Add(positionKey, new BookPosition());

            bookPositions[positionKey].AddMove(move, numTimesPlayed);
        }
    }

    [Serializable]
    public class BookPosition
    {
        public Dictionary<ushort, int> numTimesMovePlayed;

        public BookPosition()
        {
            numTimesMovePlayed = new Dictionary<ushort, int>();
        }

        public void AddMove(Move move, int numTimesPlayed = 1)
        {
            var moveValue = move.Value;

            if (numTimesMovePlayed.ContainsKey(moveValue))
                numTimesMovePlayed[moveValue]++;
            else
                numTimesMovePlayed.Add(moveValue, numTimesPlayed);
        }
    }
}