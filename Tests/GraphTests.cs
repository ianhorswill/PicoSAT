﻿#region Copyright
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
            // p.AddCustomConstraint(new GraphConnectedConstraint(graph));
            graph.AssertConnected();
            graph.WriteDot(p.Solve(), "test.dot");
        }

        [TestMethod]
        public void SpanningTreeTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 20);
            p.AddCustomConstraint(new GraphConnectedConstraint(graph));
            p.Solve();
            Assert.IsTrue(graph.SpanningForest.IsSpanningTree());
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
            p.AddCustomConstraint(new NodesConnectedConstraint(graph, 0, 1));
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
        public void OneConnectedComponentTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 5);
            p.AddCustomConstraint(new NConnectedComponentsConstraint(graph, 1));
            graph.WriteDot(p.Solve(), "test_one_connected_component.dot");
        }

        [TestMethod]
        public void TwoConnectedComponentsTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 4, 1);
            p.AddCustomConstraint(new NConnectedComponentsConstraint(graph, 2));
            graph.WriteDot(p.Solve(), "test_two_connected_components.dot");
        }

        [TestMethod]
        public void FiveConnectedComponentsTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 5);
            p.AddCustomConstraint(new NConnectedComponentsConstraint(graph, 5));
            graph.WriteDot(p.Solve(), "test_five_connected_components.dot");
        }

        [TestMethod]
        public void SubgraphConnectedTest()
        {
            var p = new Problem();
            var graph = new Graph(p, 5, 0);
            var subgraph = new Subgraph(graph, new[] { 0, 1 });
            subgraph.AssertConnected();
            graph.WriteDot(p.Solve(), "test_subgraph_connected.dot");
        }
        
        // todo: make an imaginarium-like test
        [TestMethod]
        public void ImaginariumTest()
        {
            
        }
    }
}