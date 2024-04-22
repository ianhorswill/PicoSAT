using System;
using System.Collections.Generic;
using System.Linq;

namespace CatSAT.SAT
{
    /// <summary>
    /// 
    /// </summary>
    public class Subgraph
    {
        /// <summary>
        /// 
        /// </summary>
        public Graph OriginalGraph;

        /// <summary>
        /// 
        /// </summary>
        public int[] Vertices;

        /// <summary>
        /// 
        /// </summary>
        public SpanningForest SpanningForest => new SpanningForest(this);

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<ushort, EdgeProposition> SATVariableToEdge = new Dictionary<ushort, EdgeProposition>();

        /// <summary>
        /// 
        /// </summary>
        private bool _spanningTreeBuilt = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="originalGraph"></param>
        /// <param name="vertices"></param>
        public Subgraph(Graph originalGraph, IEnumerable<int> vertices)
        {
            OriginalGraph = originalGraph;
            int[] subgraphVertices = vertices as int[] ?? vertices.ToArray();
            Vertices = subgraphVertices.ToArray();

            foreach (var pair in originalGraph.SATVariableToEdge.Where(pair =>
                         subgraphVertices.Contains(pair.Value.SourceVertex) &&
                         subgraphVertices.Contains(pair.Value.DestinationVertex)))
            {
                SATVariableToEdge[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool AreConnected(int n, int m) => SpanningForest.SameClass(n, m);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <param name="connectInOriginalGraph"></param>
        public void ConnectInSpanningTree(int n, int m, bool connectInOriginalGraph = false)
        {
            bool edgeAdded = SpanningForest.Union(n, m);
            if (connectInOriginalGraph) OriginalGraph.ConnectInSpanningTree(n, m);
            if (edgeAdded) Console.WriteLine($"Connected {n} and {m} in Subgraph.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        public void Disconnect(int n, int m)
        {
            if (!SpanningForest.Contains(OriginalGraph.Edges(n, m).Index)) return;
            SpanningForest.Clear();
            _spanningTreeBuilt = false;
            OriginalGraph.Disconnect(n, m);
            Console.WriteLine($"Disconnected {n} and {m} in Subgraph.");
        }

        /// <summary>
        /// 
        /// </summary>
        private void RebuildSpanningTree()
        {
            SpanningForest.Clear();
            // todo: down the road, keep a list/hashset of all the edges that are true, and only iterate over those
            foreach (EdgeProposition edgeProposition in SATVariableToEdge.Values.Where(edgeProposition =>
                         OriginalGraph.Solver.Propositions[edgeProposition.Index]))
            {
                ConnectInSpanningTree(edgeProposition.SourceVertex, edgeProposition.DestinationVertex);
            }
            _spanningTreeBuilt = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Reset()
        {
            _spanningTreeBuilt = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void EnsureSpanningTreeBuilt()
        {
            if (_spanningTreeBuilt) return;
            RebuildSpanningTree();
        }

        /// <summary>
        /// 
        /// </summary>
        public void AssertConnected()
        {
            OriginalGraph.Problem.AddCustomConstraint(new SubsetConnectedConstraint(this));
        }
    }
}