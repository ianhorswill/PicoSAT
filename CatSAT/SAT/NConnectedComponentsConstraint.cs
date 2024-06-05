using System;
using System.Text;

namespace CatSAT.SAT
{
    /// <summary>
    /// A class that represents a constraint on the graph. For now, the constraint is that the graph must have the
    /// specified number of connected components, n.
    /// </summary>
    public class NConnectedComponentsConstraint : CustomConstraint
    {
        /// <summary>
        /// The graph corresponding to this constraint.
        /// </summary>
        public Graph Graph;

        /// <summary>
        /// The number of connected components the graph must have according to this constraint.
        /// </summary>
        public int TargetNumComponents;

        /// <summary>
        /// The spanning forest of the graph.
        /// </summary>
        private SpanningForest SpanningForest => Graph.SpanningForest;

        /// <summary>
        /// The risk associated with adding/removing a favorable edge.
        /// </summary>
        private const int FavorableRisk = -1;

        /// <summary>
        /// The risk associated with adding/removing an unfavorable edge.
        /// </summary>
        private const int UnfavorableRisk = 1;

        /// <summary>
        /// The NConnectedComponentsConstraint constructor.
        /// </summary>
        /// <param name="graph">The graph corresponding to this constraint.</param>
        /// <param name="n">The number of connected components this graph must have according to the constraint.</param>
        public NConnectedComponentsConstraint(Graph graph, int n) : base(false, (ushort)short.MaxValue,
            graph.EdgeVariables, 1)
        {
            Graph = graph;
            TargetNumComponents = n;
            foreach (var edge in graph.SATVariableToEdge.Values)
            {
                graph.Problem.SATVariables[edge.Index].CustomConstraints.Add(this);
            }
        }

        /// <inheritdoc />
        public override int CustomFlipRisk(ushort index, bool adding)
        {
            int difference = SpanningForest.ConnectedComponentCount - TargetNumComponents;
            var edge = Graph.SATVariableToEdge[index];
            return adding ? AddingRisk(edge, difference) : RemovingRisk(edge, difference);
        }

        /// <summary>
        /// Returns the associated cost with adding this edge to the graph.
        /// </summary>
        /// <param name="edge">The edge proposition to be flipped to true.</param>
        /// <param name="difference">The difference between the current number of connected components and the target
        /// number of connected components in the graph.</param>
        /// <returns>The cost of adding this edge. Positive cost is unfavorable, negative cost is favorable.</returns>
        private int AddingRisk(EdgeProposition edge, int difference)
        {
            // curr = target or curr < target
            if (difference == 0 || difference < 0)
            {
                return Graph.AreConnected(edge.SourceVertex, edge.DestinationVertex) ? 0 : UnfavorableRisk;
            }

            // curr > target
            return Graph.AreConnected(edge.SourceVertex, edge.DestinationVertex) ? FavorableRisk : 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="difference"></param>
        /// <returns></returns>
        private int RemovingRisk(EdgeProposition edge, int difference)
        {
            // curr = target or curr > target
            if (difference == 0 || difference > 0)
            {
                return SpanningForest.MightDisconnect(edge) ? UnfavorableRisk : 0;
            }

            // curr < target
            return SpanningForest.MightDisconnect(edge) ? FavorableRisk : 0;
        }

        /// <inheritdoc />
        public override void UpdateCustomConstraint(BooleanSolver b, ushort pIndex, bool adding)
        {
            EdgeProposition edgeProp = Graph.SATVariableToEdge[pIndex];
            SpanningForest.PrintEdges(); // todo: remove
            // Console.WriteLine($"{(adding ? "adding" : "removing")} {edgeProp}");
            if (adding)
            {
                Graph.ConnectInSpanningForest(edgeProp.SourceVertex, edgeProp.DestinationVertex);
                if (SpanningForest.ConnectedComponentCount == TargetNumComponents && b.UnsatisfiedClauses.Contains(Index))
                {
                    b.UnsatisfiedClauses.Remove(Index);
                }
            }
            else
            {
                int previousComponentCount = SpanningForest.ConnectedComponentCount;
                Graph.Disconnect(edgeProp.SourceVertex, edgeProp.DestinationVertex);
                if (SpanningForest.ConnectedComponentCount != TargetNumComponents && previousComponentCount == TargetNumComponents)
                {
                    b.UnsatisfiedClauses.Add(Index);
                }
            }
        }

        /// <inheritdoc />
        internal override bool EquivalentTo(Constraint c) => false;

        /// <inheritdoc />
        internal override void Decompile(Problem p, StringBuilder b)
        {
            b.Append("NConnectedComponentsConstraint");
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
            return SpanningForest.ConnectedComponentCount == TargetNumComponents;
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
        public override int ThreatCountDeltaIncreasing(ushort count)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override int ThreatCountDeltaDecreasing(ushort count)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void UpdateTruePositiveAndFalseNegative(BooleanSolver b)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void UpdateTrueNegativeAndFalsePositive(BooleanSolver b)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}