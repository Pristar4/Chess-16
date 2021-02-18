using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Chess
{
    using static Math;

    public class Search
    {
        private const int transpositionTableSize = 64000;
        private const int immediateMateScore = 100000;
        private const int positiveInfinity = 9999999;
        private const int negativeInfinity = -positiveInfinity;
        private bool abortSearch;
        private int bestEval;
        private int bestEvalThisIteration;
        private Move bestMove;

        private Move bestMoveThisIteration;
        private readonly Board board;
        private int currentIterativeSearchDepth;
        private readonly Evaluation evaluation;

        private readonly Move invalidMove;
        private readonly MoveGenerator moveGenerator;
        private readonly MoveOrdering moveOrdering;
        private int numCutoffs;
        private int numNodes;
        private int numQNodes;
        private int numTranspositions;

        // Diagnostics
        public SearchDiagnostics searchDiagnostics;
        private Stopwatch searchStopwatch;
        private readonly AISettings settings;

        private readonly TranspositionTable tt;

        public Search(Board board, AISettings settings)
        {
            this.board = board;
            this.settings = settings;
            evaluation = new Evaluation();
            moveGenerator = new MoveGenerator();
            tt = new TranspositionTable(board, transpositionTableSize);
            moveOrdering = new MoveOrdering(moveGenerator, tt);
            invalidMove = Move.InvalidMove;
            var s = TranspositionTable.Entry.GetSize();
            //Debug.Log ("TT entry: " + s + " bytes. Total size: " + ((s * transpositionTableSize) / 1000f) + " mb.");
        }

        public event Action<Move> onSearchComplete;

        public void StartSearch()
        {
            InitDebugInfo();

            // Initialize search settings
            bestEvalThisIteration = bestEval = 0;
            bestMoveThisIteration = bestMove = Move.InvalidMove;
            tt.enabled = settings.useTranspositionTable;

            // Clearing the transposition table before each search seems to help
            // This makes no sense to me, I presume there is a bug somewhere but haven't been able to track it down yet
            if (settings.clearTTEachMove) tt.Clear();

            moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
            currentIterativeSearchDepth = 0;
            abortSearch = false;
            searchDiagnostics = new SearchDiagnostics();

            // Iterative deepening. This means doing a full search with a depth of 1, then with a depth of 2, and so on.
            // This allows the search to be aborted at any time, while still yielding a useful result from the last search.
            if (settings.useIterativeDeepening)
            {
                var targetDepth = settings.useFixedDepthSearch ? settings.depth : int.MaxValue;

                for (var searchDepth = 1; searchDepth <= targetDepth; searchDepth++)
                {
                    SearchMoves(searchDepth, 0, negativeInfinity, positiveInfinity);
                    if (abortSearch)
                    {
                        break;
                    }

                    currentIterativeSearchDepth = searchDepth;
                    bestMove = bestMoveThisIteration;
                    bestEval = bestEvalThisIteration;

                    // Update diagnostics
                    searchDiagnostics.lastCompletedDepth = searchDepth;
                    searchDiagnostics.move = bestMove.Name;
                    searchDiagnostics.eval = bestEval;
                    searchDiagnostics.moveVal = PGNCreator.NotationFromMove(FenUtility.CurrentFen(board), bestMove);

                    // Exit search if found a mate
                    if (IsMateScore(bestEval) && !settings.endlessSearchMode) break;
                }
            }
            else
            {
                SearchMoves(settings.depth, 0, negativeInfinity, positiveInfinity);
                bestMove = bestMoveThisIteration;
                bestEval = bestEvalThisIteration;
            }

            onSearchComplete?.Invoke(bestMove);

            if (!settings.useThreading) LogDebugInfo();
        }

        public (Move move, int eval) GetSearchResult()
        {
            return (bestMove, bestEval);
        }

        public void EndSearch()
        {
            abortSearch = true;
        }

        private int SearchMoves(int depth, int plyFromRoot, int alpha, int beta)
        {
            if (abortSearch) return 0;

            if (plyFromRoot > 0)
            {
                // Detect draw by repetition.
                // Returns a draw score even if this position has only appeared once in the game history (for simplicity).
                if (board.RepetitionPositionHistory.Contains(board.ZobristKey)) return 0;

                // Skip this position if a mating sequence has already been found earlier in
                // the search, which would be shorter than any mate we could find from here.
                // This is done by observing that alpha can't possibly be worse (and likewise
                // beta can't  possibly be better) than being mated in the current position.
                alpha = Max(alpha, -immediateMateScore + plyFromRoot);
                beta = Min(beta, immediateMateScore - plyFromRoot);
                if (alpha >= beta) return alpha;
            }

            // Try looking up the current position in the transposition table.
            // If the same position has already been searched to at least an equal depth
            // to the search we're doing now,we can just use the recorded evaluation.
            var ttVal = tt.LookupEvaluation(depth, plyFromRoot, alpha, beta);
            if (ttVal != TranspositionTable.lookupFailed)
            {
                numTranspositions++;
                if (plyFromRoot == 0)
                {
                    bestMoveThisIteration = tt.GetStoredMove();
                    bestEvalThisIteration = tt.entries[tt.Index].value;
                    //Debug.Log ("move retrieved " + bestMoveThisIteration.Name + " Node type: " + tt.entries[tt.Index].nodeType + " depth: " + tt.entries[tt.Index].depth);
                }

                return ttVal;
            }

            if (depth == 0)
            {
                var evaluation = QuiescenceSearch(alpha, beta);
                return evaluation;
            }

            var moves = moveGenerator.GenerateMoves(board);
            moveOrdering.OrderMoves(board, moves, settings.useTranspositionTable);
            // Detect checkmate and stalemate when no legal moves are available
            if (moves.Count == 0)
            {
                if (moveGenerator.InCheck())
                {
                    var mateScore = immediateMateScore - plyFromRoot;
                    return -mateScore;
                }

                return 0;
            }

            var evalType = TranspositionTable.UpperBound;
            var bestMoveInThisPosition = invalidMove;

            for (var i = 0; i < moves.Count; i++)
            {
                board.MakeMove(moves[i], true);
                var eval = -SearchMoves(depth - 1, plyFromRoot + 1, -beta, -alpha);
                board.UnmakeMove(moves[i], true);
                numNodes++;

                // Move was *too* good, so opponent won't allow this position to be reached
                // (by choosing a different move earlier on). Skip remaining moves.
                if (eval >= beta)
                {
                    tt.StoreEvaluation(depth, plyFromRoot, beta, TranspositionTable.LowerBound, moves[i]);
                    numCutoffs++;
                    return beta;
                }

                // Found a new best move in this position
                if (eval > alpha)
                {
                    evalType = TranspositionTable.Exact;
                    bestMoveInThisPosition = moves[i];

                    alpha = eval;
                    if (plyFromRoot == 0)
                    {
                        bestMoveThisIteration = moves[i];
                        bestEvalThisIteration = eval;
                    }
                }
            }

            tt.StoreEvaluation(depth, plyFromRoot, alpha, evalType, bestMoveInThisPosition);

            return alpha;
        }

        // Search capture moves until a 'quiet' position is reached.
        private int QuiescenceSearch(int alpha, int beta)
        {
            // A player isn't forced to make a capture (typically), so see what the evaluation is without capturing anything.
            // This prevents situations where a player ony has bad captures available from being evaluated as bad,
            // when the player might have good non-capture moves available.
            var eval = evaluation.Evaluate(board);
            searchDiagnostics.numPositionsEvaluated++;
            if (eval >= beta) return beta;
            if (eval > alpha) alpha = eval;

            var moves = moveGenerator.GenerateMoves(board, false);
            moveOrdering.OrderMoves(board, moves, false);
            for (var i = 0; i < moves.Count; i++)
            {
                board.MakeMove(moves[i], true);
                eval = -QuiescenceSearch(-beta, -alpha);
                board.UnmakeMove(moves[i], true);
                numQNodes++;

                if (eval >= beta)
                {
                    numCutoffs++;
                    return beta;
                }

                if (eval > alpha) alpha = eval;
            }

            return alpha;
        }

        public static bool IsMateScore(int score)
        {
            const int maxMateDepth = 1000;
            return Abs(score) > immediateMateScore - maxMateDepth;
        }

        public static int NumPlyToMateFromScore(int score)
        {
            return immediateMateScore - Abs(score);
        }

        private void LogDebugInfo()
        {
            AnnounceMate();
            Debug.Log(
                $"Best move: {bestMoveThisIteration.Name} Eval: {bestEvalThisIteration} Search time: {searchStopwatch.ElapsedMilliseconds} ms.");
            Debug.Log(
                $"Num nodes: {numNodes} num Qnodes: {numQNodes} num cutoffs: {numCutoffs} num TThits {numTranspositions}");
        }

        private void AnnounceMate()
        {
            if (IsMateScore(bestEvalThisIteration))
            {
                var numPlyToMate = NumPlyToMateFromScore(bestEvalThisIteration);
                //int numPlyToMateAfterThisMove = numPlyToMate - 1;

                var numMovesToMate = (int) Ceiling(numPlyToMate / 2f);

                var sideWithMate = bestEvalThisIteration * (board.WhiteToMove ? 1 : -1) < 0 ? "Black" : "White";

                Debug.Log($"{sideWithMate} can mate in {numMovesToMate} move{(numMovesToMate > 1 ? "s" : "")}");
            }
        }

        private void InitDebugInfo()
        {
            searchStopwatch = Stopwatch.StartNew();
            numNodes = 0;
            numQNodes = 0;
            numCutoffs = 0;
            numTranspositions = 0;
        }

        [Serializable]
        public class SearchDiagnostics
        {
            public int lastCompletedDepth;
            public string moveVal;
            public string move;
            public int eval;
            public bool isBook;
            public int numPositionsEvaluated;
        }
    }
}