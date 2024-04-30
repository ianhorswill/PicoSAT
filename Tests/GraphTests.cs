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
    }
}