#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StoryTests.cs" company="Ian Horswill">
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
using System.Linq;
using CatSAT;
using CatSAT.SAT;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class GraphTests
    {
        [TestMethod]
        public void GraphConnectedTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 100);
            graph.AssertConnected();
            graph.WriteDot(p.Solve(), "test.dot");
        }

        [TestMethod]
        public void SpanningForestTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 20);
            p.AddCustomConstraint(new GraphConnectedConstraint(graph));
            p.Solve();
            Assert.IsTrue(graph.SpanningForest.IsSpanningForest());
        }
        
        [TestMethod]
        public void GraphConnectedWithNumEdgesTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 20);
            p.AddCustomConstraint(new GraphConnectedConstraint(graph));
            p.Quantify(23, 23, graph.SATVariableToEdge.Values);
            graph.WriteDot(p.Solve(), "test.dot");
        }
        
        [TestMethod]
        public void GraphHasOneConnectedCycleTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 20);
            p.AddCustomConstraint(new GraphConnectedConstraint(graph));
            foreach (var vertex in graph.Vertices)
            {
                graph.VertexDegree(vertex, 2, 2);
            }
            graph.WriteDot(p.Solve(), "test_one_cycle.dot");
        }

        [TestMethod]
        public void NodesConnectedTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 40, 0);
            graph.AssertNodesConnected(0, 1);
            graph.WriteDot(p.Solve(), "test_nodes_connected.dot");
        }

        [TestMethod]
        public void NodesConnectedWithDensityTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 20);
            p.AddCustomConstraint(new NodesConnectedConstraint(graph, 0, 1));
            graph.Density(0.05f, 0.2f);
            graph.WriteDot(p.Solve(), "test.dot");
        }

        [TestMethod]
        public void GameGraphTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 10, 0);
            // todo: why does changing the order of the constraints make one of them not work?
            p.AddCustomConstraint(new NodesConnectedConstraint(graph, 0, 1));
            p.AddCustomConstraint(new GraphConnectedConstraint(graph));
            graph.Density(0.2f, 0.4f);
            graph.WriteDot(p.Solve(), "test_game_graph.dot");
        }

        [TestMethod]
        public void SubgraphConnectedTestOne()
        {
            var p = new Problem();
            var graph = new Graph(p, 5, 0);
            var subgraph = new Subgraph(graph, new[] { 0, 1 });
            subgraph.AssertConnected();
            graph.WriteDot(p.Solve(), "test_subgraph_connected.dot");
        }

        [TestMethod]
        public void SubgraphConnectedTestTwo()
        {
            var p = new Problem();
            var graph = new Graph(p, 5, 0);
            var subgraph = new Subgraph(graph, new[] { 0, 1, 4 });
            subgraph.AssertConnected();
            graph.WriteDot(p.Solve(), "test_subgraph_connected_two.dot");
        }

        [TestMethod]
        public void SubgraphConnectedTestThree()
        {
            var p = new Problem();
            var graph = new Graph(p, 20, 0);
            var subgraph = new Subgraph(graph, new[] { 1, 2, 3, 8, 9, 12, 13, 17 });
            subgraph.AssertConnected();
            graph.WriteDot(p.Solve(), "test_subgraph_connected_three.dot");
        }

        [TestMethod]
        public void NBridgesTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 6, 0);
            var subgraphOne = new Subgraph(graph, new[] { 0, 1, 2 });
            var subgraphTwo = new Subgraph(graph, new[] { 3, 4, 5 });
            subgraphOne.AssertConnected();
            subgraphTwo.AssertConnected();
            graph.AssertNBridges(1, 1, subgraphOne, subgraphTwo);
            graph.WriteDot(p.Solve(), "test_n_bridges.dot");
        }

        [TestMethod]
        public void GameTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 16, 0);
            var subgraphOne = new Subgraph(graph, new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            var subgraphTwo = new Subgraph(graph, new[] { 8, 9, 10, 11, 12, 13, 14, 15 });
            subgraphOne.AssertConnected();
            subgraphTwo.AssertConnected();
            subgraphOne.Density(0.3f, 0.7f);
            subgraphTwo.Density(0.3f, 0.7f);
            graph.AssertNBridges(1, 3, subgraphOne, subgraphTwo);
            graph.AssertConnected();
            graph.WriteDot(p.Solve(), "test_game.dot");
        }

        [TestMethod]
        public void FigureOne()
        {
            var p = new Problem();
            var g = new Graph(p, 5, 0);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "figure_one.dot");
        }
        
        [TestMethod]
        public void FigureTwo()
        {
            var p = new Problem();
            var g = new Graph(p, 5);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "figure_two.dot");
        }

        [TestMethod]
        public void FigureThree()
        {
            var p = new Problem();
            var g = new Graph(p, 40, 0);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "figure_three.dot");
        }

        [TestMethod]
        public void FigureFour()
        {
            var p = new Problem();
            var g = new Graph(p, 100);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "figure_four.dot");
        }

        [TestMethod]
        public void FigureFive()
        {
            var p = new Problem();
            var g = new Graph(p, 10, 0);
            g.AssertNodesConnected(0, 1);
            g.WriteDot(p.Solve(), "figure_five.dot");
        }

        [TestMethod]
        public void FigureSix()
        {
            var p = new Problem();
            var g = new Graph(p, 20);
            g.AssertNodesConnected(0, 1);
            g.AssertNodesConnected(2, 3);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "figure_six.dot");
        }

        [TestMethod]
        public void FigureSeven()
        {
            var p = new Problem();
            var g = new Graph(p, 5, 0);
            g.Density(0.2f, 0.2f);
            g.WriteDot(p.Solve(), "figure_seven.dot");
        }

        [TestMethod]
        public void FigureEight()
        {
            var p = new Problem();
            var g = new Graph(p, 5, 0);
            g.Density(0.9f, 0.9f);
            g.WriteDot(p.Solve(), "figure_eight.dot");
        }

        [TestMethod]
        public void FigureNine()
        {
            var p = new Problem();
            var g = new Graph(p, 10, 0);
            foreach (var v in g.Vertices)
            {
                g.VertexDegree(v, 2, 2);
            }
            g.AssertConnected();
            g.WriteDot(p.Solve(), "figure_nine.dot");
        }

        [TestMethod]
        public void FigureTen()
        {
            var p = new Problem();
            var g = new Graph(p, 12, 0);
            var s1 = new Subgraph(g, new[] { 0, 1, 2, 3, 4, 5 });
            var s2 = new Subgraph(g, new[] { 6, 7, 8, 9, 10, 11 });
            s1.AssertConnected();
            s2.AssertConnected();
            g.WriteDot(p.Solve(), "figure_ten.dot");
        }

        [TestMethod]
        public void FigureEleven()
        {
            var p = new Problem();
            var g = new Graph(p, 12, 0);
            var s1 = new Subgraph(g, new[] { 0, 1, 2});
            var s2 = new Subgraph(g, new[] { 3, 4, 5 });
            s1.AssertConnected();
            s2.AssertConnected();
            g.AssertNBridges(2, 2, s1, s2);
            g.WriteDot(p.Solve(), "figure_eleven.dot");
        }

        private (float, float) CalculateDensity(int numVertices, int minVertexDegree, int maxVertexDegree)
        {
            var numEdges = numVertices * (numVertices - 1) / 2;
            var minEdges = numVertices * minVertexDegree / 2;
            var maxEdges = numVertices * maxVertexDegree / 2;
            return ((float) minEdges / numEdges, (float) maxEdges / numEdges);
        }
        
        [TestMethod]
        public void FigureTwelve()
        {
            var p = new Problem();
            var g = new Graph(p, 20);
            var densityBounds = CalculateDensity(20, 1, 2);
            g.Density(densityBounds.Item1, densityBounds.Item2);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "figure_twelve.dot");
        }

        [TestMethod]
        public void FigureThirteen()
        {
            var p = new Problem();
            var g = new Graph(p, 15);
            var s1 = new Subgraph(g, new[] { 1, 2, 3, 4, 5 });
            var s2 = new Subgraph(g, new[] { 10, 13 });
            var s3 = new Subgraph(g, new[] { 12 });
            s1.AssertConnected();
            s2.AssertConnected();
            g.Density(0.2f, 0.3f);
            g.AssertNodesConnected(0, 10);
            g.AssertNodesConnected(9, 14);
            g.VertexDegree(12, 4, 5);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "figure_thirteen.dot");
        }

        [TestMethod]
        public void SlideFigureOne()
        {
            Problem p = new Problem();
            Graph g = new Graph(p, 15);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "slide_figure_one.dot");
        }

        [TestMethod]
        public void SlideFigureTwo()
        {
            Problem p = new Problem();
            Graph g = new Graph(p, 10);
            Subgraph s1 = new Subgraph(g, new[] { 0, 1, 2, 3, 4 });
            Subgraph s2 = new Subgraph(g, new[] { 7, 8, 9 });
            s1.AssertConnected();
            s2.AssertConnected();
            g.WriteDot(p.Solve(), "slide_figure_two.dot");
        }

        [TestMethod]
        public void SlideFigureThree()
        {
            Problem p = new Problem();
            Graph g = new Graph(p, 10);
            g.AssertNodesConnected(0, 5);
            g.AssertNodesConnected(1, 6);
            g.WriteDot(p.Solve(), "slide_figure_three.dot");
        }

        [TestMethod]
        public void SlideFigureFour()
        {
            Problem p = new Problem();
            Graph g = new Graph(p, 20);
            var densityBounds = CalculateDensity(20, 1, 2);
            Console.WriteLine($"lower: {densityBounds.Item1}, upper: {densityBounds.Item2}");
            g.Density(densityBounds.Item1, densityBounds.Item2);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "slide_figure_four.dot");
        }

        [TestMethod]
        public void SlideFigureFive()
        {
            Problem p = new Problem();
            Graph g = new Graph(p, 10);
            foreach (int v in g.Vertices)
            {
                g.VertexDegree(v, 2, 2);
            }
            g.AssertConnected();
            g.WriteDot(p.Solve(), "slide_figure_five.dot");
        }

        [TestMethod]
        public void SlideFigureSix()
        {
            Problem p = new Problem();
            Graph g = new Graph(p, 12, 0);
            Subgraph s1 = new Subgraph(g, new[] { 0, 1, 2 });
            Subgraph s2 = new Subgraph(g, new[] { 3, 4, 5 });
            s1.AssertConnected();
            s2.AssertConnected();
            g.AssertNBridges(2, 2, s1, s2);
            g.WriteDot(p.Solve(), "slide_figure_six.dot");
        }

        [TestMethod]
        public void SlideFigureSeven()
        {
            Problem p = new Problem();
            Graph g = new Graph(p, 15);
            Subgraph s1 = new Subgraph(g, new[] { 1, 2, 3, 4, 5 });
            Subgraph s2 = new Subgraph(g, new[] { 10, 13 });
            Subgraph s3 = new Subgraph(g, new[] { 12 });
            s1.AssertConnected();
            s2.AssertConnected();
            g.Density(0.2f, 0.3f);
            g.AssertNodesConnected(0, 10);
            g.AssertNodesConnected(9, 14);
            g.VertexDegree(12, 4, 5);
            g.AssertConnected();
            g.WriteDot(p.Solve(), "slide_figure_seven.dot");    
        }

        [TestMethod]
        public void BinaryTree()
        {
            const int n = 21;
            var p = new Problem();
            var g = new Graph(p, n);
            for (var i = 0; i < g.Vertices.Length; i++)
            {
                if (i < (n + 1) / 2)
                {
                    g.VertexDegree(i, 1, 1);
                }
                else if (i == g.Vertices.Length - 1)
                {
                    g.VertexDegree(i, 2, 2);
                }
                else
                {
                    g.VertexDegree(i, 3, 3);
                }
            }
            g.AssertConnected(); // not necessary by definition
            g.WriteDot(p.Solve(), "binary_tree.dot");
        }

        [TestMethod]
        public void BigGraph()
        {
            CatSAT.Random.SetSeed();
            var p = new Problem();
            var g = new Graph(p, 250, 0.01f);
            g.AssertConnected();
            var s = p.Solve();
            g.WriteDot(s, "big_graph.dot");
        }
    }
}