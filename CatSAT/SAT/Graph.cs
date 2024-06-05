using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static CatSAT.Language;

namespace CatSAT.SAT
{
    /// <summary>
    /// The representation of a graph.
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// The list of vertices in this graph.
        /// </summary>
        public int[] Vertices;

        /// <summary>
        /// The number of vertices in the graph.
        /// </summary>
        public int NumVertices;

        /// <summary>
        /// The function that returns the proposition that the edge between two vertices exists. The integers are
        /// the indices of the vertices in the Vertices array.
        /// </summary>
        public Func<int, int, EdgeProposition> Edges;

        /// <summary>
        /// The table that maps a SAT variable index (ushort) to the edge proposition.
        /// </summary>
        public Dictionary<ushort, EdgeProposition> SATVariableToEdge = new Dictionary<ushort, EdgeProposition>();

        /// <summary>
        /// The table that maps an edge proposition to the SAT variable index (ushort).
        /// </summary>
        public Dictionary<EdgeProposition, ushort> EdgeToSATVariable = new Dictionary<EdgeProposition, ushort>();

        /// <summary>
        /// The current union-find partition of the graph.
        /// </summary>
        public SpanningForest SpanningForest;

        /// <summary>
        /// The problem corresponding to this graph.
        /// </summary>
        public readonly Problem Problem;

        /// <summary>
        /// The BooleanSolver for the problem corresponding to this graph.
        /// </summary>
        public BooleanSolver Solver => Problem.BooleanSolver;

        /// <summary>
        /// The list of subgraphs in the graph (if there are any).
        /// </summary>
        public List<Subgraph> Subgraphs;

        /// <summary>
        /// True if the spanning forest has been built, false otherwise.
        /// </summary>
        private bool _spanningForestBuilt = false;

        /// <summary>
        /// The graph constructor. Initializes the vertices and spanning forest, and creates all possible edges. Creates
        /// the SAT variables for the edges and maps them to the edge propositions, and vice versa.
        /// </summary>
        /// <param name="p">The problem corresponding to the graph.</param>
        /// <param name="numVertices">The number of vertices in the graph.</param>
        /// <param name="initialDensity">The initial density of true/false assignments.</param>
        public Graph(Problem p, int numVertices, float initialDensity = 0.5f)
        {
            Problem = p;
            Vertices = new int[numVertices];
            NumVertices = numVertices;
            for (var i = 0; i < numVertices; i++)
                Vertices[i] = i;
            SpanningForest = new SpanningForest(this);
            Edges = SymmetricPredicateOfType<int, EdgeProposition>("Edges");
            Subgraphs = new List<Subgraph>();
            for (var i = 0; i < numVertices; i++)
            {
                for (var j = 0; j < i; j++)
                {
                    var edgeProposition = Edges(i, j);
                    edgeProposition.InitialProbability = initialDensity;
                    SATVariableToEdge.Add(edgeProposition.Index, edgeProposition);
                    EdgeToSATVariable.Add(edgeProposition, edgeProposition.Index);
                }
            }
        }

        /// <summary>
        /// The list of the shorts corresponding to the SAT variables of the edges in the graph.
        /// </summary>
        public short[] EdgeVariables => SATVariableToEdge.Select(pair => (short)pair.Key).ToArray();

        /// <summary>
        /// Returns the index of the given vertex. Returns -1 if the vertex is not in the graph.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>The index of the vertex.</returns>
        /// <exception cref="ArgumentException">Throws an exception if the vertex is not found in the graph.</exception>
        public int VertexToIndex(int vertex)
        {
            var index = Array.IndexOf(Vertices, vertex);
            if (index == -1)
                throw new ArgumentException("Vertex not found in graph.");
            return index;
        }

        /// <summary>
        /// Returns the vertex at the given index.
        /// </summary>
        /// <param name="index">The specified index.</param>
        /// <returns>The vertex at the specified index.</returns>
        public int IndexToVertex(int index) => Vertices[index];

        /// <summary>
        /// Returns all of the edges connected to the specified vertex.
        /// </summary>
        /// <param name="vertex">The vertex of interest.</param>
        /// <returns>The EdgePropositions including that vertex.</returns>
        private IEnumerable<EdgeProposition> EdgesIncidentTo(int vertex)
        {
            return from v in Vertices where v != vertex select Edges(v, vertex);
        }

        /// <summary>
        /// Asserts that the specified vertex has degree between min and max.
        /// </summary>
        /// <param name="vertex">The vertex of interest.</param>
        /// <param name="min">The minimum bound on the degree.</param>
        /// <param name="max">The maximum bound on the degree.</param>
        public void VertexDegree(int vertex, int min, int max)
        {
            Problem.Quantify(min, max, EdgesIncidentTo(vertex));
        }

        /// <summary>
        /// Asserts that the graph has density (percentage of edges present in the graph) between min and max.
        /// </summary>
        /// <param name="min">The minimum bound on the graph's density.</param>
        /// <param name="max">The maximum bound on the graph's density.</param>
        public void Density(float min, float max)
        {
            var edgeCount = SATVariableToEdge.Count;
            Problem.Quantify((int)Math.Round(min * edgeCount), (int)Math.Round(max * edgeCount),
                SATVariableToEdge.Values);
        }

        /// <summary>
        /// Returns whether the two specified vertices are connected in the current partition.
        /// </summary>
        /// <param name="n">The first vertex.</param>
        /// <param name="m">The second vertex.</param>
        /// <returns>True if the two vertices are connected, false otherwise.</returns>
        public bool AreConnected(int n, int m) => SpanningForest.SameClass(n, m);

        /// <summary>
        /// Adds the edge (n, m) to the spanning forest. 
        /// </summary>
        /// <param name="n">The first vertex in the edge.</param>
        /// <param name="m">The second vertex in the edge.</param>
        public void ConnectInSpanningForest(int n, int m)
        {
            var edgeAdded = SpanningForest.Union(n, m);
            // if (edgeAdded) Console.WriteLine($"Connected {n} and {m} in Graph.");
        }

        /// <summary>
        /// Removes the edge (n, m) from the spanning forest, if it is present there.
        /// </summary>
        /// <param name="n">The first vertex.</param>
        /// <param name="m">The second vertex.</param>
        public void Disconnect(int n, int m)
        {
            if (!SpanningForest.Contains(Edges(n, m).Index)) return;
            SpanningForest.Clear();
            _spanningForestBuilt = false;
            // Console.WriteLine($"Disconnected {n} and {m} in Graph.");
            RebuildSpanningForest();
        }

        /// <summary>
        /// Returns whether or not two vertices are adjacent to each other (i.e., share an edge).
        /// </summary>
        /// <param name="n">The first vertex.</param>
        /// <param name="m">The second vertex.</param>
        /// <returns>True if the edge (n, m) exists, false otherwise.</returns>
        public bool AdjacentVertices(int n, int m) => Solver.Propositions[EdgeToSATVariable[Edges(n, m)]];

        /// <summary>
        /// Rebuilds the spanning forest with the current edge propositions which are true. Called after removing an edge.
        /// </summary>
        private void RebuildSpanningForest()
        {
            SpanningForest.Clear();
            foreach (var edgeProposition in SATVariableToEdge.Values.Where(edgeProposition =>
                         Solver.Propositions[edgeProposition.Index]))
            {
                ConnectInSpanningForest(edgeProposition.SourceVertex, edgeProposition.DestinationVertex);
            }

            _spanningForestBuilt = true;
        }

        /// <summary>
        /// Writes the Dot file to visualize the graph.
        /// </summary>
        /// <param name="solution">The graph's solution.</param>
        /// <param name="path">The file path for the outputted Dot file.</param>
        public void WriteDot(Solution solution, string path)
        {
            using var file = File.CreateText(path);
            {
                file.WriteLine("graph G {");
                file.WriteLine("   layout = fdp;");
                foreach (var vertex in Vertices)
                    file.WriteLine($"   {vertex};");
                if (Subgraphs.Count > 0)
                {
                    var subgraphCounter = 0;
                    foreach (var subgraph in Subgraphs)
                    {
                        file.WriteLine($"   subgraph cluster{subgraphCounter} {{");
                        foreach (var vertex in subgraph.Vertices)
                        {
                            file.WriteLine($"      {vertex};");
                        }

                        file.WriteLine("   }");
                        subgraphCounter++;
                    }
                }

                foreach (var edge in SATVariableToEdge.Select(pair => pair.Value).Where(edge => solution[edge]))
                    file.WriteLine(
                        $"   {edge.SourceVertex} -- {edge.DestinationVertex} [color={EdgeColor(edge.Index)}];");
                file.WriteLine("}");
            }
        }

        /// <summary>
        /// Sets the color of the edge ot be green if it is in the spanning forest, red otherwise.
        /// </summary>
        /// <param name="index">The index corresponding to the edge.</param>
        /// <returns>The color of the edge as a string.</returns>
        private string EdgeColor(ushort index) => SpanningForest.Contains(index) ? "green" : "red";

        /// <summary>
        /// Checks whether the spanning forest for the graph has been built. If not, rebuilds it.
        /// </summary>
        public void EnsureSpanningForestBuilt()
        {
            if (_spanningForestBuilt) return;
            RebuildSpanningForest();
        }

        /// <summary>
        /// Sets the spanning forest built flag to false.
        /// </summary>
        public void Reset()
        {
            _spanningForestBuilt = false;
        }

        /// <summary>
        /// Generates all edges between vertices of two subgraphs, where one vertex is in the first subgraph and the
        /// other vertex is in the second subgraph.
        /// </summary>
        /// <param name="subgraphOne">The first subgraph.</param>
        /// <param name="subgraphTwo">The second subgraph.</param>
        private IEnumerable<EdgeProposition> EdgesBetweenSubgraphs(Subgraph subgraphOne, Subgraph subgraphTwo)
        {
            var edges = (from vertexOne in subgraphOne.Vertices
                from vertexTwo in subgraphTwo.Vertices
                select Edges(vertexOne, vertexTwo)).ToList();
            return edges;
        }

        /// <summary>
        /// Adds a graph connected constraint to the problem.
        /// </summary>
        public void AssertConnected()
        {
            Problem.AddCustomConstraint(new GraphConnectedConstraint(this));
        }

        /// <summary>
        /// Adds a constraint that the two specified nodes must be connected in the graph.
        /// </summary>
        /// <param name="sourceNode">The source node.</param>
        /// <param name="destinationNode">The destination node.</param>
        public void AssertNodesConnected(int sourceNode, int destinationNode)
        {
            Problem.AddCustomConstraint(new NodesConnectedConstraint(this, sourceNode, destinationNode));
        }

        /// <summary>
        /// Adds a constraint that the graph must have exactly n connected components.
        /// </summary>
        /// <param name="n">The number of connected components.</param>
        public void AssertNConnectedComponents(int n)
        {
            Problem.AddCustomConstraint(new NConnectedComponentsConstraint(this, n));
        }

        /// <summary>
        /// Adds a constraint that the graph must have between min and max bridges between the two specified subgraphs.
        /// </summary>
        /// <param name="min">The minimum number of bridges to generate.</param>
        /// <param name="max">The maximum number of bridges to generate.</param>
        /// <param name="subgraphOne">The first subgraph.</param>
        /// <param name="subgraphTwo">The second subgraph.</param>
        public void AssertNBridges(int min, int max, Subgraph subgraphOne, Subgraph subgraphTwo)
        {
            Problem.Quantify(min, max, EdgesBetweenSubgraphs(subgraphOne, subgraphTwo));
        }
    }
}