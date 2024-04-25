using System;
using System.Collections.Generic;
using System.Linq;

namespace CatSAT.SAT
{
    /// <summary>
    /// The Union-Find data structure. Currently only works for integer-valued vertices.
    /// </summary>
    public class SpanningForest
    {
        /// <summary>
        /// The number of connected components in this partition.
        /// </summary>
        public int ConnectedComponentCount;

        /// <summary>
        /// 
        /// </summary>
        private Graph _graph;

        /// <summary>
        /// 
        /// </summary>
        private Subgraph _subgraph;
        
        /// <summary>
        /// 
        /// </summary>
        private HashSet<ushort> _edges;
        
        /// <summary>
        /// The number of vertices in this union-find data structure.
        /// </summary>
        private readonly int _verticesCount;

        /// <summary>
        /// The list of representatives and ranks for this union-find data structure, indexed by vertex number.
        /// </summary>
        private readonly (int representative, int rank)[] _representativesAndRanks;

        /// <summary>
        /// 
        /// </summary>
        private readonly bool _madeFromSubgraph;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        public SpanningForest(Graph graph)
        {
            _graph = graph;
            ConnectedComponentCount = graph.NumVertices;
            _verticesCount = graph.NumVertices;
            _representativesAndRanks = new (int representative, int rank)[_verticesCount];
            _edges = new HashSet<ushort>();
            for (int i = 0; i < _verticesCount; i++)
            {
                _representativesAndRanks[i].representative = i;
                _representativesAndRanks[i].rank = 0;
            }

            _madeFromSubgraph = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subgraph"></param>
        public SpanningForest(Subgraph subgraph)
        {
            _subgraph = subgraph;
            _graph = subgraph.OriginalGraph;
            ConnectedComponentCount = subgraph.Vertices.Length;
            _verticesCount = subgraph.Vertices.Length;
            _representativesAndRanks = new (int representative, int rank)[subgraph.OriginalGraph.NumVertices];
            _edges = new HashSet<ushort>();
            for (int i = 0; i < subgraph.OriginalGraph.NumVertices; i++)
            {
                _representativesAndRanks[i].representative = _subgraph.Vertices.Contains(i) ? i : -1;
                _representativesAndRanks[i].rank = _subgraph.Vertices.Contains(i) ? 0 : -1;
            }
        }
        
        /// <summary>
        /// Merges two vertices to have the same representative. Vertex n merges into vertex m. Uses union by rank.
        /// </summary>
        /// <param name="n">The vertex to be merged with m.</param>
        /// <param name="m">The vertex with the representative that will become the representative for n.</param>
        /// <returns>True if the edge was added to the spanning forest, false otherwise.</returns>
        public bool Union(int n, int m)
        {
            int nRepresentative = Find(n);
            int mRepresentative = Find(m);
            
            if (nRepresentative == mRepresentative) return false;

            _edges.Add(_graph.EdgeToSATVariable[_graph.Edges(n, m)]);
            if (_representativesAndRanks[nRepresentative].rank < _representativesAndRanks[mRepresentative].rank)
            {
                _representativesAndRanks[nRepresentative].representative = mRepresentative;
            }
            else if (_representativesAndRanks[nRepresentative].rank > _representativesAndRanks[mRepresentative].rank)
            {
                _representativesAndRanks[mRepresentative].representative = nRepresentative;
            }
            else
            {
                _representativesAndRanks[mRepresentative].representative = nRepresentative;
                _representativesAndRanks[nRepresentative].rank++;
            }
            
            ConnectedComponentCount--;
            return true;
        }
        
        /// <summary>
        /// Finds the representative of the specified vertex. Uses path compression.
        /// </summary>
        /// <param name="n">The vertex for which we return the representative.</param>
        /// <returns>The vertex's representative.</returns>
        public int Find(int n)
        {
            return _representativesAndRanks[n].representative == n ? n : Find(_representativesAndRanks[n].representative);
        }
        
        /// <summary>
        /// Returns whether two vertices are in the same equivalence class.
        /// </summary>
        /// <param name="n">The first vertex.</param>
        /// <param name="m">The second vertex.</param>
        /// <returns>True if the vertices are in the same equivalence class, false otherwise.</returns>
        public bool SameClass(int n, int m) => Find(n) == Find(m);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public bool WouldConnect(int n, int m, EdgeProposition edge)
        {
            var nRep = Find(n);
            var mRep = Find(m);
            var sourceRep = Find(edge.SourceVertex);
            var destRep = Find(edge.DestinationVertex);
            return (nRep == sourceRep && mRep == destRep) || (nRep == destRep && mRep == sourceRep);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public bool MightDisconnect(EdgeProposition edge) => _edges.Contains(edge.Index);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edgeIndex"></param>
        /// <returns></returns>
        public bool Contains(ushort edgeIndex) => _edges.Contains(edgeIndex);
        
        /// <summary>
        /// Resets the union-find data structure. All nodes become their own representatives and all ranks become 0.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _representativesAndRanks.Length; i++)
            {
                _representativesAndRanks[i] = (i, 0);
            }
            ConnectedComponentCount = _verticesCount;
            _edges.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public void PrintEdges()
        {
            Console.WriteLine(
                $"edges in spanning forest: {_edges.Select(edge => _graph.SATVariableToEdge[edge]).Aggregate("", (current, edge) => current + (edge + " "))}");
        }
        
        /// <summary>
        /// Determines whether the spanning tree has been correctly constructed.
        /// </summary>
        /// <returns>True if the spanning tree contains all the vertices in the graph, false otherwise.</returns>
        public bool IsSpanningTree()
        {
            HashSet<int> visited = new HashSet<int>();
            foreach (ushort index in _edges)
            {
                visited.Add(_graph.SATVariableToEdge[index].SourceVertex);
                visited.Add(_graph.SATVariableToEdge[index].DestinationVertex);
            }
            Console.WriteLine(string.Join(", ", visited));
            return visited.Count == _graph.Vertices.Length;
        }
    }
}