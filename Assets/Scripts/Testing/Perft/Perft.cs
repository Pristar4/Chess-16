using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Chess.Testing
{
    public class Perft : MonoBehaviour
    {
        [Header("Single Test")] public string testFen;

        public int depth;
        public bool divide;
        public TextAsset expectedResults;

        [Header("Suite Test")] public TextAsset fullPerftSuite;

        public TextAsset fastPerftSuite;

        [Tooltip("Log breakdown of time spent on make/unmake vs movegen. Adds some overhead.")]
        public bool enableTimingStats;

        [Header("UI")] public TMP_Text logUI;

        public Toggle timingStatsToggle;

        private Board board;

        // Timers
        private Stopwatch makeMoveTimer;
        private MoveGenerator moveGenerator;
        private Stopwatch moveGenTimer;

        private Dictionary<string, int> perftDivideResults;
        private Stopwatch unmakeMoveTimer;

        private void Start()
        {
            board = new Board();
            timingStatsToggle.SetIsOnWithoutNotify(enableTimingStats);
            timingStatsToggle.onValueChanged.AddListener(v => enableTimingStats = v);
            logUI.text = "";
            moveGenerator = new MoveGenerator();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Profiler.BeginSample("Chess Test");
                RunSingleTest();
                Profiler.EndSample();
            }
        }

        public void StartFullSuiteRoutine(int repetitions)
        {
            StartCoroutine(RunSuite(fullPerftSuite, repetitions));
        }

        public void StartFastSuiteRoutine(int repetitions)
        {
            StartCoroutine(RunSuite(fastPerftSuite, repetitions));
        }

        public IEnumerator RunSuite(TextAsset perftSuite, int repetitions = 1)
        {
            var suiteTotalTimer = 0;
            var minTime = int.MaxValue;
            var maxTime = 0;

            var tests = GetSuiteTests(perftSuite);

            for (var repeatCount = 0; repeatCount < repetitions; repeatCount++)
            {
                ClearLog();
                if (repetitions == 1)
                    LogMessage("Running Test Suite...");
                else
                    LogMessage($"Running Test Suite... (repetition {repeatCount + 1} of {repetitions})");

                var suiteTime = 0;
                var allPassed = true;

                makeMoveTimer = new Stopwatch();
                unmakeMoveTimer = new Stopwatch();
                moveGenTimer = new Stopwatch();

                for (var i = 0; i < tests.Length; i++)
                {
                    yield return new WaitForEndOfFrame();
                    var test = tests[i];

                    board.LoadPosition(test.fen);

                    var sw = new Stopwatch();
                    sw.Start();
                    var numNodes = SearchWithTimingStats(test.depth);
                    sw.Stop();
                    var success = numNodes == test.expectedNodeCount;

                    allPassed &= success;
                    suiteTime += (int) sw.ElapsedMilliseconds;

                    var logString = "Test {0}/{1} {2}. Generated {3} moves in {4} ms.";
                    LogMessage(string.Format(logString, i + 1, tests.Length, success ? "Passed" : "Failed", numNodes,
                        sw.ElapsedMilliseconds));
                }

                LogMessage(string.Format("{0}. Total time: {1} ms", allPassed ? "Suite passed" : "Suite failed",
                    suiteTime));
                if (enableTimingStats)
                {
                    LogMessage("Timing breakdown: (note that enabling this adds some overhead)");
                    LogMessage($"Make move: {makeMoveTimer.ElapsedMilliseconds} ms");
                    LogMessage($"Unmake move: {unmakeMoveTimer.ElapsedMilliseconds} ms");
                    LogMessage($"MoveGen: {moveGenTimer.ElapsedMilliseconds} ms");
                }

                suiteTotalTimer += suiteTime;
                minTime = Mathf.Min(minTime, suiteTime);
                maxTime = Mathf.Max(maxTime, suiteTime);

                if (repeatCount < repetitions - 1) yield return new WaitForSeconds(1);
            }

            if (repetitions > 1)
                LogMessage(
                    $"Suite run {repetitions} times. Time (ms): min = {minTime} max = {maxTime} avg = {suiteTotalTimer / repetitions}");
        }

        public void RunSingleTest()
        {
            moveGenerator = new MoveGenerator();
            board.LoadPosition(testFen);
            var sw = new Stopwatch();
            sw.Start();
            var numNodes = 0;
            if (divide)
            {
                perftDivideResults = new Dictionary<string, int>();
                numNodes = SearchDivide(depth, depth);
                ComparePerftDivideResults(testFen);
            }
            else
            {
                numNodes = Search(depth);
            }

            sw.Stop();

            LogMessage(string.Format("Num nodes: {0} at depth: {1} in {2} ms", numNodes, depth,
                sw.ElapsedMilliseconds));
        }

        private int Search(int depth)
        {
            var moves = moveGenerator.GenerateMoves(board);

            if (depth == 1) return moves.Count;

            var numLocalNodes = 0;

            for (var i = 0; i < moves.Count; i++)
            {
                board.MakeMove(moves[i]);
                var numNodesFromThisPosition = Search(depth - 1);
                numLocalNodes += numNodesFromThisPosition;
                board.UnmakeMove(moves[i]);
            }

            return numLocalNodes;
        }

        private int SearchWithTimingStats(int depth, bool batchMode = true)
        {
            if (depth == 0 && !batchMode) return 1;
            if (enableTimingStats) moveGenTimer.Start();
            var moves = moveGenerator.GenerateMoves(board);
            if (enableTimingStats) moveGenTimer.Stop();

            var numLocalNodes = 0;

            if (depth == 1 && batchMode) return moves.Count;

            for (var i = 0; i < moves.Count; i++)
            {
                if (enableTimingStats) makeMoveTimer.Start();
                board.MakeMove(moves[i]);
                if (enableTimingStats) makeMoveTimer.Stop();
                var numNodesFromThisPosition = SearchWithTimingStats(depth - 1);
                numLocalNodes += numNodesFromThisPosition;
                if (enableTimingStats) unmakeMoveTimer.Start();
                board.UnmakeMove(moves[i]);
                if (enableTimingStats) unmakeMoveTimer.Stop();
            }

            return numLocalNodes;
        }

        private int SearchDivide(int startDepth, int currentDepth)
        {
            var moves = moveGenerator.GenerateMoves(board);

            if (currentDepth == 1) return moves.Count;

            var numLocalNodes = 0;

            for (var i = 0; i < moves.Count; i++)
            {
                board.MakeMove(moves[i]);
                var numMovesForThisNode = SearchDivide(startDepth, currentDepth - 1);
                numLocalNodes += numMovesForThisNode;
                board.UnmakeMove(moves[i]);

                if (currentDepth == startDepth)
                    perftDivideResults.Add(PerftUtility.MoveName(moves[i]), numMovesForThisNode);
            }

            return numLocalNodes;
        }

        private void ComparePerftDivideResults(string fen)
        {
            var expected = expectedResults.text.Split('\n');
            var expectedPerftDResults = new Dictionary<string, int>();
            foreach (var line in expected)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var moveName = line.Split(':')[0];
                var nodeCount = line.Split(':')[1].Trim();
                expectedPerftDResults.Add(moveName, int.Parse(nodeCount));
            }

            foreach (var move in expectedPerftDResults.Keys)
                if (perftDivideResults.ContainsKey(move))
                {
                    var expectedValue = expectedPerftDResults[move];
                    var actualValue = perftDivideResults[move];

                    if (expectedValue != actualValue)
                    {
                        board.LoadPosition(fen);
                        var movesFromPos = moveGenerator.GenerateMoves(board);
                        for (var i = 0; i < movesFromPos.Count; i++)
                        {
                            var m = movesFromPos[i];
                            if (PerftUtility.MoveName(m) == move)
                            {
                                board.MakeMove(m);
                                break;
                            }
                        }

                        Debug.Log(string.Format("{0}: Expected {1} but had {2}", move, expectedValue, actualValue));
                        Debug.Log("Fen after this move: " + FenUtility.CurrentFen(board));
                    }
                }
                else
                {
                    Debug.Log("Expected move: " + move + ", but was not found.");
                }
        }

        private void LogMessage(string message)
        {
            Debug.Log(message);
            if (Application.isPlaying) logUI.text += message + "\n";
        }

        private void ClearLog()
        {
            if (Application.isPlaying) logUI.text = "";
        }

        public Test[] GetSuiteTests(TextAsset suiteFile)
        {
            var testList = new List<Test>();

            var suiteText = suiteFile.text;
            suiteText = suiteText.Split('{')[1].Split('}')[0];
            var testStrings = suiteText.Split('\n');

            for (var i = 0; i < testStrings.Length; i++)
            {
                var testString = testStrings[i].Trim();
                var sections = testString.Split(',');
                if (sections.Length == 3)
                {
                    var test = new Test
                        {depth = int.Parse(sections[0]), expectedNodeCount = int.Parse(sections[1]), fen = sections[2]};
                    testList.Add(test);
                }
            }

            return testList.ToArray();
        }

        [Serializable]
        public struct Test
        {
            public string fen;
            public int depth;
            public int expectedNodeCount;
        }
    }
}