#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PredicateTests.cs" company="Ian Horswill">
// Copyright (C) 2018 Ian Horswill
//  
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//  
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CatSAT;
using static CatSAT.Language;
using static System.Linq.Enumerable;

namespace Tests
{
    [TestClass]
    public class PredicateTests
    {
        [TestInitialize]
        public void StartLogging()
        {
            Problem.LogPerformanceDataToConsole = true;
            Problem.LogFile = "../../../Test timings.csv";
        }

        [TestMethod]
        public void NRooksTest()
        {
            var n = 8;

            var p = new Problem("N rooks");

            // rook(i, j) means there's a root at row i, column j
            var rook = Predicate<int, int>("rook");

            //
            // Add constraint to the program
            //

            // There should be a total of n rooks
            p.Exactly(n, Range(0, n).SelectMany(i => Range(0, n).Select(j => rook(i, j))));

            // There should be one rook in each row
            foreach (var i in Range(0, n))
                p.Unique(Range(0, n), j => rook(i, j));

            // There should be one rook in each column
            foreach (var j in Range(0, n))
                p.Unique(Range(0, n), i => rook(i, j));

            var m = p.Solve();

            //
            // Test the constraint in the solution
            //

            m.Exactly(n, Range(0, n).SelectMany(i => Range(0, n).Select(j => rook(i, j))));
            foreach (var i in Range(0, n))
                m.Unique(Range(0, n), j => rook(i, j));
            foreach (var j in Range(0, n))
                m.Unique(Range(0, n), i => rook(i, j));
        }

        /// <summary>
        /// Same as N Rooks, but we don't tell it that there has to be a rook in every row and column.
        /// So it has less information to use to prune the search.
        /// </summary>
        [TestMethod]
        public void NRooksHarderTest()
        {
            var n = 8;

            var p = new Problem("N rooks hard") { Timeout = 2000000 };
            

            // rook(i, j) means there's a root at row i, column j
            var rook = Predicate<int, int>("rook");

            //
            // Add constraint to the program
            //

            // There should be a total of n rooks
            p.Exactly(n, Range(0, n).SelectMany(i => Range(0, n).Select(j => rook(i, j))));

            // There should be at most one rook in each row
            foreach (var i in Range(0, n))
                p.AtMost(1, Range(0, n), j => rook(i, j));

            // There should be at most one rook in each column
            foreach (var j in Range(0, n))
                p.AtMost(1, Range(0, n), i => rook(i, j));

            var m = p.Solve();

            //
            // Test the constraint in the solution
            //

            m.Exactly(n, Range(0, n).SelectMany(i => Range(0, n).Select(j => rook(i, j))));
            foreach (var i in Range(0, n))
                m.AtMost(1, Range(0, n).Select(j => rook(i, j)));
            foreach (var j in Range(0, n))
                m.AtMost(1, Range(0, n).Select(i => rook(i, j)));
        }

        [TestMethod]
        public void InductionTest()
        {
            var n = 8;

            var p = new Problem("induction test");
            var pred = Predicate<int>("pred");
            p.Assert(pred(0));
            for (var i = 0; i < n; i++)
                p.Assert(pred(i) == pred(i-1));
            for (int t = 0; t < 100; t++)
            {
                var m = p.Solve();
                Assert.IsTrue(m.All(Range(0, n).Select(i => pred(i))));
            }
        }

        [TestMethod, ExpectedException(typeof(NonTightProblemException))]
        public void NonstrictTest()
        {
            var n = 8;

            var p = new Problem("induction test");
            var pred = Predicate<int>("pred");

            for (var i = 1; i < n - 1; i++)
                // cell set iff neighbor is set.
            {
                p.Assert(
                    pred(i) <= pred(i - 1),
                    pred(i) <= pred(i + 1)
                );
            }

            // Solve it.
            for (int t = 0; t < 100; t++)
            {
                var m = p.Solve();
                // This has 3 solutions: all false, all true, all but one end true, all but either end true.
                var c = m.Count(Range(0, n).Select(pred));
                Assert.IsTrue(c == 0 || c >= n - 2);
            }
        }
        
        [TestMethod]
        public void ConnectedChainTest()
        {
            var n = 8;

            var p = new Problem("connected chain test");
            Func<int, int, Proposition> adjacent = (i, j) => Math.Abs(i - j) < 2;
            var connected = Predicate<int, int>("connected");

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                        p.Assert(connected(i, i));
                    else
                    {
                        // i is connected to j if the node on either side is connected to j and i is adjacent to the neighbor.
                        p.Assert(
                            connected(i, j) <= adjacent(i, i - 1), connected(i - 1, j),
                            connected(i, j) <= adjacent(i, i + 1), connected(i + 1, j)
                        );
                    }
                }
            }

            var m = p.Solve();
            for (int trial = 0; trial < 100; trial++)
            {
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        Assert.IsTrue(m.IsTrue(connected(i, j)));
                    }
                }
            }
        }

        [TestMethod,ExpectedException(typeof(NonTightProblemException))]
        public void TransitiveClosureTest()
        {
            // Compute the transitive closure of a 5-node graph using Floyd-Warshall
            // This *should* get constant-folded away
            var p = new Problem("transitive closure test");
            var vertices = new[] { "a", "b", "c", "d", "e" };
            var edges = new[]
            {
                new[] {"a", "b"},
                new[] {"a", "d"},
                new[] {"b", "c"},
                new[] {"d", "c"},
            };
            Proposition Adjacent(string v1, string v2) => edges.Any(e => (v1 == v2) || (e[0] == v1 && e[1] == v2) || (e[0] == v2 && e[1] == v1));

            var connected = SymmetricPredicate<string>("connected");

            foreach (var from in vertices)
            foreach (var to in vertices)
            {
                if (from == to)
                    continue;
                p.Assert(connected(from, to) <= Adjacent(from, to));
                foreach (var intermediary in vertices)
                    if (intermediary != from && intermediary != to)
                        p.Assert(connected(from, to) <= (Adjacent(from, intermediary) & connected(intermediary, to)));
            }

            p.Optimize();

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();

                // a, b, c, d should be a connected component, e should be unconnected to anything but e
                foreach (var v1 in vertices)
                foreach (var v2 in vertices)
                    Assert.IsTrue(s[connected(v1, v2)] == (v1 == v2) || (v1 != "e" && v2 != "e"));
            }
        }


        [TestMethod]
        public void FloydWarshallTest()
        {
            // Compute the transitive closure of a 5-node graph using Floyd-Warshall
            // This *should* get constant-folded away
            var p = new Problem("transitive closure test");
            var vertices = new[] {"a", "b", "c", "d", "e"};
            var edges = new[]
            {
                new[] {"a", "b"},
                new[] {"a", "d"},
                new[] {"b", "c"},
                new[] {"d", "c"},
            };
            Proposition Adjacent(string v1, string v2) => edges.Any(e => (v1 == v2) || (e[0] == v1 && e[1] == v2) || (e[0] == v2 && e[1] == v1));
            var floyd = Predicate<string, string, int>("d");
            // Inlines either adjacent or floyd, depending on k
            Proposition D(string v1, string v2, int k) => k == 0 ? Adjacent(v1, v2) : floyd(v1, v2, k);
            for (int k = 1; k < vertices.Length; k++)
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

            p.Optimize();

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();

                // a, b, c, d should be a connected component, e should be unconnected to anything but e
                foreach (var v1 in vertices)
                foreach (var v2 in vertices)
                    Assert.IsTrue(s[Connected(v1, v2)] == (v1 == v2) || (v1 != "e" && v2 != "e"));
            }
        }

        [TestMethod]
        public void InverseFloydWarshall5Test()
        {
            // Make a random 5-node undirected graph with designated connected components.
            // Computes transitive closure of using Floyd-Warshall
            InverseFWTest("IFW5", new[] { "a", "b", "c", "d", "e" });
        }

        [TestMethod]
        public void InverseFloydWarshall10Test()
        {
            // Make a random 5-node undirected graph with designated connected components.
            // Computes transitive closure of using Floyd-Warshall
            InverseFWTest("IFW10", new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" });
        }

        [TestMethod]
        public void InverseFloydWarshall20Test()
        {
            // Make a random 5-node undirected graph with designated connected components.
            // Computes transitive closure of using Floyd-Warshall
            InverseFWTest("IFW20", new[]
            {
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
                "11", "12", "13", "14", "15", "16", "17", "18", "19", "20"
            });
        }

        private static void InverseFWTest(string name, string[] vertices)
        {
            var p = new Problem(name);
            var adjacent = Predicate<string, string>("adjacent");
            var floyd = Predicate<string, string, int>("d");

            // Inlines either adjacent or floyd, depending on k
            Proposition D(string v1, string v2, int k) => k == 0 ? adjacent(v1, v2) : floyd(v1, v2, k);
            for (int k = 1; k < vertices.Length; k++)
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
                if (v1 == v2 || (v1 != "e" && v2 != "e"))
                    p.Assert(Connected(v1, v2));
                else
                    p.Assert(Not(Connected(v1, v2)));

            p.Optimize();

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();

                // a, b, c, d should be a connected component, e should be unconnected to anything but e
                foreach (var v1 in vertices)
                foreach (var v2 in vertices)
                    Assert.IsTrue(s[Connected(v1, v2)] == (v1 == v2) || (v1 != "e" && v2 != "e"));
            }
            p.LogPerformanceData();
        }

        [TestMethod]
        public void SudokuTest()
        {
            var p = new Problem("Sudoku");
            var digits = new[] {1, 2, 3, 4, 5, 6, 7, 8, 9};
            var cell = Predicate<int, int, int>("cell");
            foreach (var rank in digits)
                foreach (var d in digits)
                {
                    p.Unique(digits, row => cell(row, rank, d));
                    p.Unique(digits, column => cell(rank, column, d));
                }
            foreach (var row in digits)
                foreach (var col in digits)
                    p.Unique(digits, d => cell(row, col, d));

            for (int i = 0; i < 100; i++)
                p.Solve();

        }
    }
}
