using System;

namespace CatSAT.SAT
{
    /// <summary>
    /// A proposition representing an edge in a graph (u, v).
    /// </summary>
    public class EdgeProposition : SpecialProposition
    {
        /// <summary>
        /// The integer corresponding to the source vertex of the edge, u.
        /// </summary>
        public int SourceVertex;

        /// <summary>
        /// The integer corresponding to the destination vertex of the edge, v.
        /// </summary>
        public int DestinationVertex;

        /// <summary>
        /// Overrides the initialize function for a special proposition. Sets the source and destination vertices.
        /// </summary>
        /// <param name="p">The problem.</param>
        /// <exception cref="Exception">If the EdgeProposition name is null, we cannot assign vertices accordingly.
        /// </exception>
        public override void Initialize(Problem p)
        {
            if (!(Name is Call n))
                throw new Exception(
                    "EdgeProposition name must be an object array containing the source and destination vertices.");
            SourceVertex = (int)n.Args[0];
            DestinationVertex = (int)n.Args[1];
        }
    }
}