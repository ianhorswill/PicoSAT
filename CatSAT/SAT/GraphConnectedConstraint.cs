using System;
using System.Collections.Generic;
using System.Text;

namespace CatSAT.SAT
{
    // todo: look into making it so that edges not in spanning tree (red ones) wouldn't get considered by greedy flip
    /// <summary>
    /// A class that represents a constraint on the graph that the graph must be connected.
    /// </summary>
    internal class GraphConnectedConstraint : CustomConstraint
    {
        /// <summary>
        /// The graph corresponding to this constraint.
        /// </summary>
        public Graph Graph;

        /// <summary>
        /// The spanning forest of the graph.
        /// </summary>
        private SpanningForest SpanningForest => Graph.SpanningForest;
        
        /// <summary>
        /// The risk associated with removing an edge which is in the spanning tree.
        /// </summary>
        private const int EdgeRemovalRisk = 1;

        /// <summary>
        /// The risk associated with adding an edge which connects two previously unconnected components.
        /// </summary>
        private const int EdgeAdditionRisk = -1;

        /// <summary>
        /// The GraphConnectedConstraint constructor.
        /// </summary>
        /// <param name="graph">The graph corresponding to this constraint.</param>
        public GraphConnectedConstraint(Graph graph) : base(false, (ushort)short.MaxValue, graph.EdgeVariables, 1)
        {
            Graph = graph;
            foreach (var edge in graph.SATVariableToEdge.Values)
            {
                graph.Problem.SATVariables[edge.Index].CustomConstraints.Add(this);
            }
        }

        /// <inheritdoc />
        public override int CustomFlipRisk(ushort index, bool adding)
        {
            var componentCount = SpanningForest.ConnectedComponentCount;
            if (componentCount == 1 && adding) return 0;
            var edge = Graph.SATVariableToEdge[index];
            return adding ? AddingRisk(edge) : RemovingRisk(edge);
        }

        /// <summary>
        /// Returns the associated cost with adding this edge to the graph.
        /// </summary>
        /// <param name="edge">The edge proposition to be flipped to true.</param>
        /// <returns>The cost of adding this edge. Positive cost is unfavorable, negative cost is favorable.</returns>
        private int AddingRisk(EdgeProposition edge) =>
            Graph.AreConnected(edge.SourceVertex, edge.DestinationVertex) ? 0 : EdgeAdditionRisk;

        /// <summary>
        /// Returns the associated cost with removing this edge from the graph.
        /// </summary>
        /// <param name="edge">The edge proposition to be flipped to false.</param>
        /// <returns>The cost of removing this edge. Positive cost is unfavorable, negative cost is favorable.</returns>
        private int RemovingRisk(EdgeProposition edge) => SpanningForest.Contains(edge.Index) ? EdgeRemovalRisk : 0;
        
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
            do prime = Random.Prime(); while (prime <= dCount);
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
            if (adding)
            {
                Graph.ConnectInSpanningForest(edgeProp.SourceVertex, edgeProp.DestinationVertex);
                if (SpanningForest.ConnectedComponentCount == 1 && b.UnsatisfiedClauses.Contains(Index))
                    b.UnsatisfiedClauses.Remove(Index);
            }
            else
            {
                var previousComponentCount = SpanningForest.ConnectedComponentCount;
                Graph.Disconnect(edgeProp.SourceVertex, edgeProp.DestinationVertex);
                if (SpanningForest.ConnectedComponentCount > 1 && previousComponentCount == 1)
                    b.UnsatisfiedClauses.Add(Index);
            }
        }

        /// <inheritdoc />
        internal override bool EquivalentTo(Constraint c) => false;

        /// <inheritdoc />
        internal override void Decompile(Problem p, StringBuilder b)
        {
            b.Append("GraphConnectedConstraint");
        }

        /// <inheritdoc />
        public override void Reset()
        {
            Graph.Reset();
        }

        #region Counting methods
        /// <inheritdoc />
        public override bool IsSatisfied(ushort satisfiedDisjuncts)
        {
            Graph.EnsureSpanningForestBuilt();
            return SpanningForest.ConnectedComponentCount == 1;
        }

        /// <inheritdoc />
        public override bool MaxFalseLiterals(int falseLiterals)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override bool MaxTrueLiterals(int trueLiterals)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override int ThreatCountDeltaDecreasing(ushort count)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override int ThreatCountDeltaIncreasing(ushort count)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void UpdateTrueNegativeAndFalsePositive(BooleanSolver b)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void UpdateTruePositiveAndFalseNegative(BooleanSolver b)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}