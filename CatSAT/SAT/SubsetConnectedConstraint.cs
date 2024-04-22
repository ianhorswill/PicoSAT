using System;
using System.Collections.Generic;
using System.Text;

namespace CatSAT.SAT
{
    /// <summary>
    /// 
    /// </summary>
    internal class SubsetConnectedConstraint : CustomConstraint
    {
        /// <summary>
        /// The graph corresponding to this constraint.
        /// </summary>
        public Subgraph Subgraph;

        /// <summary>
        /// 
        /// </summary>
        private SpanningForest SubgraphSpanningForest => Subgraph.SpanningForest;
        
        // todo: will either need separate spanning forest just for the subset of the graph
        // todo: or check that all of the vertices in the subset have the same representative, O(n)
        
        /// <summary>
        /// The risk associated with removing an edge which is in the spanning tree.
        /// </summary>
        private const int EdgeRemovalRisk = 1;

        /// <summary>
        /// The risk associated with adding an edge which connects two previously unconnected components.
        /// </summary>
        private const int EdgeAdditionRisk = -1;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subgraph"></param>
        public SubsetConnectedConstraint(Subgraph subgraph) : base(false, (ushort)short.MaxValue, subgraph.OriginalGraph.EdgeVariables, 1)
        {
            Subgraph = subgraph;
            foreach (var edge in subgraph.SATVariableToEdge.Values)
            {
                subgraph.OriginalGraph.Problem.SATVariables[edge.Index].CustomConstraints.Add(this);
            }
        }

        /// <inheritdoc />
        public override int CustomFlipRisk(ushort index, bool adding)
        {
            var componentCount = SubgraphSpanningForest.ConnectedComponentCount;
            if (componentCount == 1 && adding) return 0;
            var edge = Subgraph.SATVariableToEdge[index];
            return adding ? AddingRisk(edge) : RemovingRisk(edge);
        }

        /// <summary>
        /// Returns the associated cost with adding this edge to the graph.
        /// </summary>
        /// <param name="edge">The edge proposition to be flipped to true.</param>
        /// <returns>The cost of adding this edge. Positive cost is unfavorable, negative cost is favorable.</returns>
        private int AddingRisk(EdgeProposition edge) =>
            Subgraph.AreConnected(edge.SourceVertex, edge.DestinationVertex) ? 0 : EdgeAdditionRisk;

        /// <summary>
        /// Returns the associated cost with removing this edge from the graph.
        /// </summary>
        /// <param name="edge">The edge proposition to be flipped to false.</param>
        /// <returns>The cost of removing this edge. Positive cost is unfavorable, negative cost is favorable.</returns>
        private int RemovingRisk(EdgeProposition edge) => SubgraphSpanningForest.Contains(edge.Index) ? EdgeRemovalRisk : 0;
        
        /// <summary>
        /// Find the edge (proposition) to flip that will lead to the lowest cost.
        /// </summary>
        /// <param name="b">The current BooleanSolver.</param>
        /// <returns>The index of the edge (proposition) to flip.</returns>
        public override ushort GreedyFlip(BooleanSolver b)
        {
            List<short> disjuncts = UnPredeterminedDisjuncts;
            ushort lastFlipOfThisClause = b.LastFlip[Index];

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
                if (!Subgraph.SATVariableToEdge.TryGetValue(selectedVar, out var edge)) continue;
                if (Subgraph.AreConnected(edge.SourceVertex, edge.DestinationVertex)) continue;
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
            EdgeProposition edgeProp = Subgraph.SATVariableToEdge[pIndex];
            if (adding)
            {
                Subgraph.ConnectInSpanningTree(edgeProp.SourceVertex, edgeProp.DestinationVertex, true);
                if (SubgraphSpanningForest.ConnectedComponentCount == 1 && b.UnsatisfiedClauses.Contains(Index))
                    b.UnsatisfiedClauses.Remove(Index);
            }
            else
            {
                int previousComponentCount = SubgraphSpanningForest.ConnectedComponentCount;
                Subgraph.Disconnect(edgeProp.SourceVertex, edgeProp.DestinationVertex);
                if (SubgraphSpanningForest.ConnectedComponentCount > 1 && previousComponentCount == 1)
                    b.UnsatisfiedClauses.Add(Index);
            }
        }

        /// <inheritdoc />
        internal override bool EquivalentTo(Constraint c) => false;

        /// <inheritdoc />
        internal override void Decompile(Problem p, StringBuilder b)
        {
            b.Append("SubsetConnectedConstraint");
        }

        /// <inheritdoc />
        public override void Reset()
        {
            Subgraph.Reset();
        }

        #region Counting methods
        /// <inheritdoc />
        public override bool IsSatisfied(ushort satisfiedDisjuncts)
        {
            Subgraph.EnsureSpanningTreeBuilt();
            return SubgraphSpanningForest.ConnectedComponentCount == 1;
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