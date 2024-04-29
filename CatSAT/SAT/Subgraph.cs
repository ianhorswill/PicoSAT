using System;
using System.Collections.Generic;
using System.Linq;

namespace CatSAT.SAT
{
    /// <summary>
    /// The representation of a subgraph (subset of nodes in a graph).
    /// </summary>
    public class Subgraph
    {
        /// <summary>
        /// The original graph to which the subgraph belongs.
        /// </summary>
        public Graph OriginalGraph;

        /// <summary>
        /// The list of vertices in this subgraph.
        /// </summary>
        public int[] Vertices;

        /// <summary>
        /// The current union-find partition of the subgraph.
        /// </summary>
        public SpanningForest SpanningForest;

        /// <summary>
        /// The table that maps a SAT variable index (ushort) to the edge proposition.
        /// </summary>
        public Dictionary<ushort, EdgeProposition> SATVariableToEdge = new Dictionary<ushort, EdgeProposition>();

        /// <summary>
        /// True if the spanning tree has been built, false otherwise.
        /// </summary>
        private bool _spanningTreeBuilt = false;

        /// <summary>
        /// The subgraph constructor. Creates the SAT variables for the edges and maps them to edge propositions from
        /// the original graph.
        /// </summary>
        /// <param name="originalGraph">The original graph.</param>
        /// <param name="vertices">The list of vertices in the subgraph.</param>
        public Subgraph(Graph originalGraph, IEnumerable<int> vertices)
        {
            OriginalGraph = originalGraph;
            var subgraphVertices = vertices as int[] ?? vertices.ToArray();
            Vertices = subgraphVertices.ToArray();

            foreach (var pair in originalGraph.SATVariableToEdge.Where(pair =>
                         subgraphVertices.Contains(pair.Value.SourceVertex) &&
                         subgraphVertices.Contains(pair.Value.DestinationVertex)))
            {
                SATVariableToEdge[pair.Key] = pair.Value;
            }

            SpanningForest = new SpanningForest(this);
            originalGraph.Subgraphs.Add(this);
        }

        /// <summary>
        /// Returns whether the two specified vertices are connected in the current partition.
        /// </summary>
        /// <param name="n">The first vertex.</param>
        /// <param name="m">The second vertex.</param>
        /// <returns>True if the two vertices are connected, false otherwise.</returns>
        public bool AreConnected(int n, int m) => SpanningForest.SameClass(n, m);

        /// <summary>
        /// Adds the edge (n, m) to the spanning tree for the subgraph, and optionally to the original graph if it is
        /// not already there.
        /// </summary>
        /// <param name="n">The first vertex in the edge.</param>
        /// <param name="m">The second vertex in the edge.</param>
        /// <param name="connectInOriginalGraph">If true, will also connect the edge in the original graph. If false,
        /// will not.</param>
        public void ConnectInSpanningTree(int n, int m, bool connectInOriginalGraph = false)
        {
            var edgeAdded = SpanningForest.Union(n, m);
            if (connectInOriginalGraph) OriginalGraph.ConnectInSpanningTree(n, m);
            if (edgeAdded) Console.WriteLine($"Connected {n} and {m} in Subgraph.");
        }

        /// <summary>
        /// Removes the edge (n, m) from the spanning tree, if it is present there. Also removes it from the original
        /// graph.
        /// </summary>
        /// <param name="n">The first vertex.</param>
        /// <param name="m">The second vertex.</param>
        public void Disconnect(int n, int m)
        {
            if (!SpanningForest.Contains(OriginalGraph.Edges(n, m).Index)) return;
            SpanningForest.Clear();
            _spanningTreeBuilt = false;
            OriginalGraph.Disconnect(n, m);
            Console.WriteLine($"Disconnected {n} and {m} in Subgraph.");
        }

        /// <summary>
        /// Rebuilds the spanning tree with the current edge propositions which are true. Called after removing an edge.
        /// </summary>
        private void RebuildSpanningTree()
        {
            SpanningForest.Clear();
            // todo: down the road, keep a list/hashset of all the edges that are true, and only iterate over those
            foreach (var edgeProposition in SATVariableToEdge.Values.Where(edgeProposition =>
                         OriginalGraph.Solver.Propositions[edgeProposition.Index]))
            {
                ConnectInSpanningTree(edgeProposition.SourceVertex, edgeProposition.DestinationVertex);
            }

            _spanningTreeBuilt = true;
        }

        /// <summary>
        /// Sets the spanning tree built flag to false.
        /// </summary>
        public void Reset()
        {
            _spanningTreeBuilt = false;
        }

        /// <summary>
        /// Checks whether the spanning tree for the subgraph has been built. If not, rebuilds it.
        /// </summary>
        public void EnsureSpanningTreeBuilt()
        {
            if (_spanningTreeBuilt) return;
            RebuildSpanningTree();
        }

        /// <summary>
        /// Adds a subset connected constraint to the problem.
        /// </summary>
        public void AssertConnected()
        {
            OriginalGraph.Problem.AddCustomConstraint(new SubsetConnectedConstraint(this));
        }

        /// <summary>
        /// Asserts that the subgraph has density (percentage of edges present in the subgraph) between min and max.
        /// </summary>
        /// <param name="min">The minimum bound on the subgraph's density.</param>
        /// <param name="max">The maximum bound on the subgraph's density.</param>
        public void Density(float min, float max)
        {
            var edgeCount = SATVariableToEdge.Count;
            OriginalGraph.Problem.Quantify((int)(min * edgeCount), (int)(max * edgeCount),
                SATVariableToEdge.Values);
        }
    }
}