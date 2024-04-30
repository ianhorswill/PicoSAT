using System;
using System.Collections.Generic;
using System.Text;

namespace CatSAT.SAT
{
    /// <summary>
    /// A class that represents a constraint on the graph. For now, the constraint is that two nodes must be connected.
    /// </summary>
    public class NodesConnectedConstraint : CustomConstraint
    {
        /// <summary>
        /// The graph corresponding to this constraint.
        /// </summary>
        public Graph Graph;

        /// <summary>
        /// The first node to be connected.
        /// </summary>
        public int SourceNode;

        /// <summary>
        /// The second node to be connected.
        /// </summary>
        public int DestinationNode;

        /// <summary>
        /// The queue used for BFS.
        /// </summary>
        private Queue<int> _queue;

        /// <summary>
        /// The spanning forest of the graph.
        /// </summary>
        private SpanningForest SpanningForest => Graph.SpanningForest;

        /// <summary>
        /// The number of vertices in the graph.
        /// </summary>
        private int NumVertices => Graph.NumVertices;

        /// <summary>
        /// The edges in the path between the source node and the destination node.
        /// </summary>
        private HashSet<ushort> _edgesInPath;

        /// <summary>
        /// The default risk associated with removing an edge.
        /// </summary>
        private const int EdgeRemovalRisk = 1;

        /// <summary>
        /// The default risk associated with adding an edge.
        /// </summary>
        private const int EdgeAdditionRisk = -1;

        /// <summary>
        /// True if the source node and destination node are connected, false otherwise.
        /// </summary>
        private bool _connected = false;

        /// <summary>
        /// The list of predecessors in the path from source node to vertex node, indexed by vertex number.
        /// </summary>
        private int[] _predecessors;

        /// <summary>
        /// The NodesConnectedConstraint constructor.
        /// </summary>
        /// <param name="graph">The graph corresponding to this constraint.</param>
        /// <param name="sourceNode">The first node to be connected.</param>
        /// <param name="destinationNode">The second node to be connected.</param>
        public NodesConnectedConstraint(Graph graph, int sourceNode, int destinationNode) : base(false,
            (ushort)short.MaxValue, graph.EdgeVariables, 1)
        {
            Graph = graph;
            SourceNode = sourceNode;
            DestinationNode = destinationNode;
            _queue = new Queue<int>(NumVertices);
            _predecessors = new int[NumVertices];
            _edgesInPath = new HashSet<ushort>(NumVertices - 1);
            foreach (var edge in graph.SATVariableToEdge.Values)
            {
                graph.Problem.SATVariables[edge.Index].CustomConstraints.Add(this);
            }
        }

        /// <summary>
        /// Does a breadth-first search to find the minimum path between the source node and the destination node.
        /// </summary>
        private void ShortestPath()
        {
            for (var i = 0; i < NumVertices; i++)
                _predecessors[i] = -1;

            _edgesInPath.Clear();
            _queue.Clear();
            _predecessors[SourceNode] = SourceNode;
            _queue.Enqueue(SourceNode);
            
            while (_queue.Count > 0)
            {
                var currentNode = _queue.Dequeue();

                for (var vertex = 0; vertex < NumVertices; vertex++)
                {
                    if (vertex == currentNode) continue; // no self edges
                    if (_predecessors[vertex] != -1) continue; // already found predecessor
                    if (!Graph.AdjacentVertices(vertex, currentNode))
                        continue; // no edge between vertex and currentNode
                    _predecessors[vertex] = currentNode;
                    if (vertex == DestinationNode) goto foundIt;
                    _queue.Enqueue(vertex);
                }
            }

            throw new Exception("No path found between source and destination nodes.");
            foundIt:
            for (var node = DestinationNode; node != SourceNode; node = _predecessors[node])
            {
                var edge = Graph.Edges(_predecessors[node], node);
                _edgesInPath.Add(edge.Index);
            }
        }

        /// <inheritdoc />
        public override int CustomFlipRisk(ushort index, bool adding)
        {
            var edge = Graph.SATVariableToEdge[index];
            var previouslyConnected = Graph.AreConnected(edge.SourceVertex, edge.DestinationVertex);
            if (previouslyConnected && adding) return 0;
            return adding ? AddingRisk(edge) : RemovingRisk(edge);
        }

        /// <summary>
        /// Returns the associated cost with adding this edge to the graph.
        /// </summary>
        /// <param name="edge">The edge proposition to be flipped to true.</param>
        /// <returns>The cost of adding this edge. Positive cost is unfavorable, negative cost is favorable.</returns>
        private int AddingRisk(EdgeProposition edge)
        {
            if (SpanningForest.WouldConnect(SourceNode, DestinationNode, edge)) return EdgeAdditionRisk * 2;
            return Graph.AreConnected(edge.SourceVertex, edge.DestinationVertex) ? 0 : EdgeAdditionRisk;
        }

        /// <summary>
        /// Returns the associated cost with removing this edge from the graph.
        /// </summary>
        /// <param name="edge">The edge proposition to be flipped to false.</param>
        /// <returns>The cost of removing this edge. Positive cost is unfavorable, negative cost is favorable.</returns>
        private int RemovingRisk(EdgeProposition edge)
        {
            return _connected ? _edgesInPath.Contains(edge.Index) ? EdgeRemovalRisk * 2 : 0
                : SpanningForest.Contains(edge.Index) ? EdgeRemovalRisk : 0;
        }

        /// <summary>
        /// Find the edge (proposition) to flip that will lead to the lowest cost.
        /// </summary>
        /// <param name="b">The current BooleanSolver.</param>
        /// <returns>The index of the edge (proposition) to flip.</returns>
        public override ushort GreedyFlip(BooleanSolver b)
        {
            var disjuncts = UnPredeterminedDisjuncts;
            var lastFlipOfThisClause = b.LastFlip[Index];

            var best = 0;
            var bestDelta = int.MaxValue;

            var dCount = (uint)disjuncts.Count;
            var index = Random.InRange(dCount);
            uint prime;
            do prime = Random.Prime();
            while (prime <= dCount);
            for (var i = 0; i < dCount; i++)
            {
                var literal = disjuncts[(int)index];
                index = (index + prime) % dCount;
                var selectedVar = (ushort)Math.Abs(literal);
                if (selectedVar == lastFlipOfThisClause) continue;
                var edge = Graph.SATVariableToEdge[selectedVar];
                if (Graph.AreConnected(edge.SourceVertex, edge.DestinationVertex)) continue;
                var delta = b.UnsatisfiedClauseDelta(selectedVar);
                if (delta <= 0)
                {
                    best = selectedVar;
                    break;
                }

                if (delta >= bestDelta) continue;
                best = selectedVar;
                bestDelta = delta;
            }

            if (best == 0) return (ushort)Math.Abs(disjuncts.RandomElement());
            return (ushort)best;
        }

        /// <inheritdoc />
        public override void UpdateCustomConstraint(BooleanSolver b, ushort pIndex, bool adding)
        {
            var edgeProp = Graph.SATVariableToEdge[pIndex];
            var previouslyConnected = Graph.AreConnected(SourceNode, DestinationNode);
            if (adding)
            {
                Graph.ConnectInSpanningForest(edgeProp.SourceVertex, edgeProp.DestinationVertex);
                if (previouslyConnected || !Graph.AreConnected(SourceNode, DestinationNode) ||
                    !b.UnsatisfiedClauses.Contains(Index)) return;

                // we have connected the two nodes via some path. find this path
                b.UnsatisfiedClauses.Remove(Index);
                _connected = true;
                ShortestPath();
            }
            else
            {
                Graph.Disconnect(edgeProp.SourceVertex, edgeProp.DestinationVertex);
                if (!previouslyConnected || Graph.AreConnected(SourceNode, DestinationNode)) return;

                b.UnsatisfiedClauses.Add(Index);
                _connected = false;
            }
        }

        /// <inheritdoc />
        internal override bool EquivalentTo(Constraint c) => false;

        /// <inheritdoc />
        internal override void Decompile(Problem p, StringBuilder b)
        {
            b.Append("NodesConnectedConstraint");
        }

        /// <inheritdoc />
        public override void Reset()
        {
            _connected = false;
            Graph.Reset();
        }

        #region Counting methods

        /// <inheritdoc />
        public override bool IsSatisfied(ushort satisfiedDisjuncts)
        {
            Graph.EnsureSpanningForestBuilt();
            return Graph.AreConnected(SourceNode, DestinationNode);
        }

        /// <inheritdoc />
        public override int ThreatCountDeltaIncreasing(ushort count)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override int ThreatCountDeltaDecreasing(ushort count)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override void UpdateTruePositiveAndFalseNegative(BooleanSolver b)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override void UpdateTrueNegativeAndFalsePositive(BooleanSolver b)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool MaxFalseLiterals(int falseLiterals)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool MaxTrueLiterals(int trueLiterals)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}