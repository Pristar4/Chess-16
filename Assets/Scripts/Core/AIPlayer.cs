using System.Threading;
using System.Threading.Tasks;

namespace Chess.Game
{
    public class AIPlayer : Player
    {
        private const int bookMoveDelayMillis = 250;
        private readonly Board board;

        private readonly Book book;
        private CancellationTokenSource cancelSearchTimer;
        private Move move;
        private bool moveFound;

        private readonly Search search;
        private readonly AISettings settings;

        public AIPlayer(Board board, AISettings settings)
        {
            this.settings = settings;
            this.board = board;
            settings.requestAbortSearch += TimeOutThreadedSearch;
            search = new Search(board, settings);
            search.onSearchComplete += OnSearchComplete;
            search.searchDiagnostics = new Search.SearchDiagnostics();
            book = BookCreator.LoadBookFromFile(settings.book);
        }

        // Update running on Unity main thread. This is used to return the chosen move so as
        // not to end up on a different thread and unable to interface with Unity stuff.
        public override void Update()
        {
            if (moveFound)
            {
                moveFound = false;
                ChoseMove(move);
            }

            settings.diagnostics = search.searchDiagnostics;
        }

        public override void NotifyTurnToMove()
        {
            search.searchDiagnostics.isBook = false;
            moveFound = false;

            var bookMove = Move.InvalidMove;
            if (settings.useBook && board.plyCount <= settings.maxBookPly)
                if (book.HasPosition(board.ZobristKey))
                    bookMove = book.GetRandomBookMoveWeighted(board.ZobristKey);

            if (bookMove.IsInvalid)
            {
                if (settings.useThreading)
                    StartThreadedSearch();
                else
                    StartSearch();
            }
            else
            {
                search.searchDiagnostics.isBook = true;
                search.searchDiagnostics.moveVal = PGNCreator.NotationFromMove(FenUtility.CurrentFen(board), bookMove);
                settings.diagnostics = search.searchDiagnostics;
                Task.Delay(bookMoveDelayMillis).ContinueWith(t => PlayBookMove(bookMove));
            }
        }

        private void StartSearch()
        {
            search.StartSearch();
            moveFound = true;
        }

        private void StartThreadedSearch()
        {
            //Thread thread = new Thread (new ThreadStart (search.StartSearch));
            //thread.Start ();
            Task.Factory.StartNew(() => search.StartSearch(), TaskCreationOptions.LongRunning);

            if (!settings.endlessSearchMode)
            {
                cancelSearchTimer = new CancellationTokenSource();
                Task.Delay(settings.searchTimeMillis, cancelSearchTimer.Token)
                    .ContinueWith(t => TimeOutThreadedSearch());
            }
        }

        // Note: called outside of Unity main thread
        private void TimeOutThreadedSearch()
        {
            if (cancelSearchTimer == null || !cancelSearchTimer.IsCancellationRequested) search.EndSearch();
        }

        private void PlayBookMove(Move bookMove)
        {
            move = bookMove;
            moveFound = true;
        }

        private void OnSearchComplete(Move move)
        {
            // Cancel search timer in case search finished before timer ran out (can happen when a mate is found)
            cancelSearchTimer?.Cancel();
            moveFound = true;
            this.move = move;
        }
    }
}