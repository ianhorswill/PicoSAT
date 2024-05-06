using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CatSAT;
using CatSAT.SAT;
using static CatSAT.Language;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class GraphStopwatchTests
    {
        private static readonly int[] GraphSizes = { 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        private const int NumIterations = 100;

        private double[,] _stopwatchMilliseconds = new double[NumIterations, GraphSizes.Length];

        private static readonly string[] FiveVertices = { "A", "B", "C", "D", "E" };
        private static readonly string[] TenVertices = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
        
        private static readonly string[][] Vertices = { FiveVertices, TenVertices };

        [TestMethod]
        public void GraphConnectedStopwatch()
        {
            for (var row = 0; row < GraphSizes.Length; row++)
            {
                var size = GraphSizes[row];

                var p = new Problem();
                var graph = new Graph(p, size);
                graph.AssertConnected();

                for (var col = 0; col < NumIterations; col++)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    p.Solve();
                    stopwatch.Stop();
                    _stopwatchMilliseconds[col, row] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }

            WriteResultsToFile("GraphConnected", ".txt", _stopwatchMilliseconds, GraphSizes);
        }

        [TestMethod]
        public void NodesConnectedStopwatch()
        {
            for (var row = 0; row < GraphSizes.Length; row++)
            {
                var size = GraphSizes[row];

                var p = new Problem();
                var graph = new Graph(p, size);
                graph.AssertNodesConnected(0, 1);

                for (var col = 0; col < NumIterations; col++)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    p.Solve();
                    stopwatch.Stop();
                    _stopwatchMilliseconds[col, row] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }

            WriteResultsToFile("NodesConnected", ".txt", _stopwatchMilliseconds, GraphSizes);
        }

        [TestMethod]
        public void TwoSubgraphsConnectedStopwatch()
        {
            for (var row = 0; row < GraphSizes.Length; row++)
            {
                var size = GraphSizes[row];

                var p = new Problem();
                var graph = new Graph(p, size);
                var subgraph1Vertices = Enumerable.Range(0, size / 2).ToArray();
                var subgraph2Vertices = Enumerable.Range(size / 2, size - subgraph1Vertices.Length).ToArray();
                var subgraph1 = new Subgraph(graph, subgraph1Vertices);
                var subgraph2 = new Subgraph(graph, subgraph2Vertices);
                subgraph1.AssertConnected();
                subgraph2.AssertConnected();

                for (var col = 0; col < NumIterations; col++)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    p.Solve();
                    stopwatch.Stop();
                    _stopwatchMilliseconds[col, row] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }

            WriteResultsToFile("TwoSubgraphsConnected", ".txt", _stopwatchMilliseconds, GraphSizes);
        }

        [TestMethod]
        public void CycleStopwatch()
        {
            for (var row = 0; row < GraphSizes.Length; row++)
            {
                var size = GraphSizes[row];

                var p = new Problem();
                var graph = new Graph(p, size);
                foreach (var vertex in graph.Vertices)
                {
                    graph.VertexDegree(vertex, 2, 2);
                }

                for (var col = 0; col < NumIterations; col++)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    p.Solve();
                    stopwatch.Stop();
                    _stopwatchMilliseconds[col, row] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }

            WriteResultsToFile("Cycle", ".txt", _stopwatchMilliseconds, GraphSizes);
        }

        [TestMethod]
        public void DensityStopwatch()
        {
            for (var row = 0; row < GraphSizes.Length; row++)
            {
                var size = GraphSizes[row];

                var p = new Problem();
                var graph = new Graph(p, size);
                graph.Density(0.5f, 0.5f);

                for (var col = 0; col < NumIterations; col++)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    p.Solve();
                    stopwatch.Stop();
                    _stopwatchMilliseconds[col, row] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }

            WriteResultsToFile("Density", ".txt", _stopwatchMilliseconds, GraphSizes);
        }

        [TestMethod]
        public void BridgesStopwatch()
        {
            for (var row = 0; row < GraphSizes.Length; row++)
            {
                var size = GraphSizes[row];

                var p = new Problem();
                var graph = new Graph(p, size);
                var subgraph1Vertices = Enumerable.Range(0, size / 2).ToArray();
                var subgraph2Vertices = Enumerable.Range(size / 2, size - subgraph1Vertices.Length).ToArray();
                var subgraph1 = new Subgraph(graph, subgraph1Vertices);
                var subgraph2 = new Subgraph(graph, subgraph2Vertices);
                graph.AssertNBridges(size / 2, size / 2, subgraph1, subgraph2);

                for (var col = 0; col < NumIterations; col++)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    p.Solve();
                    stopwatch.Stop();
                    _stopwatchMilliseconds[col, row] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }

            WriteResultsToFile("Bridges", ".txt", _stopwatchMilliseconds, GraphSizes);
        }

        [TestMethod]
        public void InverseFloydWarshallStopwatch()
        {
            var graphSizes = new[] { 5, 10 };
            var milliseconds = new double[NumIterations, graphSizes.Length];
            
            for (var row = 0; row < graphSizes.Length; row++)
            {
                var size = graphSizes[row];
                var name = $"IFW{size}";
                var vertices = Vertices[row];

                var p = new Problem(name);

                var adjacent = Predicate<string, string>("adjacent");
                var floyd = Predicate<string, string, int>("d");

                Proposition D(string v1, string v2, int k) => k == 0 ? adjacent(v1, v2) : floyd(v1, v2, k);
                for (var k = 1; k < vertices.Length; k++)
                {
                    var vk = vertices[k];
                    foreach (var v1 in vertices)
                    foreach (var v2 in vertices)
                        p.Assert(
                            D(v1, v2, k) <= D(v1, v2, k - 1),
                            D(v1, v2, k) <= (D(v1, vk, k - 1) & D(vk, v2, k - 1))
                        );
                }
                
                Proposition Connected(string v1, string v2) => D(v1, v2, vertices.Length - 1);

                // Now constrain its connectivity
                foreach (var v1 in vertices)
                foreach (var v2 in vertices)
                    if (v1 == v2 || (v1 != vertices.Last() && v2 != vertices.Last()))
                        p.Assert(Connected(v1, v2));
                    else
                        p.Assert(Not(Connected(v1, v2)));

                p.Optimize();

                for (var col = 0; col < NumIterations; col++)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var s = p.Solve();
                    stopwatch.Stop();
                    foreach (var v1 in vertices)
                    foreach (var v2 in vertices)
                        Assert.IsTrue(s[Connected(v1, v2)] == (v1 == v2) || (v1 != vertices.Last() && v2 != vertices.Last()));
                    milliseconds[col, row] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }
            
            WriteResultsToFile("FloydWarshall", ".txt", milliseconds, graphSizes);
        }

        private void WriteResultsToFile(string filename, string extension, double[,] resultsArray, int[] graphSizes)
        {
            using (var file = new System.IO.StreamWriter($"{filename}Stats{extension}"))
            {
                file.WriteLine($"{filename} Stats");
                for (var row = 0; row < graphSizes.Length; row++)
                {
                    file.WriteLine($"Size: {graphSizes[row]}");
                    var millisecondsRemovedOutliers = RemoveOutliers(GetRow(resultsArray, row));
                    file.WriteLine($"     Average: {GetAverage(millisecondsRemovedOutliers)}");
                    file.WriteLine($"     Median: {GetMedian(millisecondsRemovedOutliers)}");
                    file.WriteLine(
                        $"     Standard Deviation: {GetStandardDeviation(millisecondsRemovedOutliers)}");
                    file.WriteLine($"     Minimum: {GetMinimum(millisecondsRemovedOutliers)}");
                    file.WriteLine($"     Maximum: {GetMaximum(millisecondsRemovedOutliers)}");
                }
            }

            using (var file = new System.IO.StreamWriter($"{filename}{extension}"))
            {
                file.WriteLine($"{filename} Results");
                for (var row = 0; row < graphSizes.Length; row++)
                {
                    file.WriteLine($"Size: {graphSizes[row]}");
                    for (var col = 0; col < NumIterations; col++)
                    {
                        file.WriteLine($"    {resultsArray[col, row]}");
                    }
                }
            }
        }

        private double[] GetColumn(double[,] matrix, int columnNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(0))
                .Select(x => matrix[x, columnNumber])
                .ToArray();
        }

        private double[] GetRow(double[,] matrix, int rowNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(1))
                .Select(x => matrix[rowNumber, x])
                .ToArray();
        }

        private double[] RemoveOutliers(double[] milliseconds)
        {
            var average = GetAverage(milliseconds);
            var standardDeviation = GetStandardDeviation(milliseconds);
            var lowerBound = average - 2 * standardDeviation;
            var upperBound = average + 2 * standardDeviation;
            return milliseconds.Where(x => x >= lowerBound && x <= upperBound).ToArray();
        }

        private double GetAverage(IReadOnlyCollection<double> milliseconds) => milliseconds.Sum() / milliseconds.Count;

        private double GetMedian(IReadOnlyCollection<double> milliseconds)
        {
            var sorted = milliseconds.OrderBy(x => x).ToList();
            var count = sorted.Count;
            if (count % 2 == 0)
            {
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
            }

            return sorted[count / 2];
        }

        private double GetStandardDeviation(IReadOnlyCollection<double> milliseconds)
        {
            var average = GetAverage(milliseconds);
            var sum = milliseconds.Sum(d => Math.Pow(d - average, 2));
            return Math.Sqrt(sum / milliseconds.Count);
        }

        private double GetMinimum(IReadOnlyCollection<double> milliseconds) => milliseconds.Min();

        private double GetMaximum(IReadOnlyCollection<double> milliseconds) => milliseconds.Max();
    }
}