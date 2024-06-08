using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CatSAT
{
    [DebuggerDisplay("{" + nameof(DebugName) + "}")]
    public abstract class Constraint
    {
        internal virtual string DebugName
        {
            get
            {
                var b = new StringBuilder();
                var firstOne = true;
                b.Append("<");
                foreach (var d in Disjuncts)
                {
                    if (firstOne)
                        firstOne = false;
                    else
                        b.Append(" ");
                    b.Append(d);
                }
                b.Append(">");
                return b.ToString();
            }
        }

        /// <summary>
        /// The literals of the constraint
        /// </summary>
        internal readonly short[] Disjuncts;

        internal virtual IEnumerable<short> Literals => Disjuncts;

        /// <summary>
        /// Any variables that require special handling when flipped
        /// </summary>
        internal virtual IEnumerable<ushort> SpecialHandlingVariables()
        {
            yield break;
        }

        /// <summary>
        /// The not predeteremined disjuncts of the constraint
        /// </summary>
        internal List<short> UnPredeterminedDisjuncts = new List<short>();

        public readonly int Hash;

        /// <summary>
        /// Position in the Problem's Constraints list.
        /// </summary>
        internal ushort Index;

        /// <summary>
        /// True if this is a plain old boring disjunction
        /// </summary>
        public readonly bool IsNormalDisjunction;

        /// <summary>
        /// Minimum number of disjuncts that must be true in order for the constraint
        /// to be satisfied, minus one.  For a normal clause, this is 0 (i.e. the real min number is 1)
        /// </summary>
        public readonly short MinDisjunctsMinusOne;

        protected Constraint(bool isDisjunction, ushort min, short[] disjuncts, int extraHash)
        {
            IsNormalDisjunction = isDisjunction;
            Disjuncts = disjuncts.Distinct().ToArray();
            MinDisjunctsMinusOne = (short)(min - 1);
            Hash = ComputeHash(Disjuncts) ^ extraHash;
            if ((min != 1) && disjuncts.Length != Disjuncts.Length)
                throw new ArgumentException("Nonstandard clause has non-unique disjuncts");
        }

        private static int ComputeHash(short[] disjuncts)
        {
            var hash = 0;
            foreach (var disjunct in disjuncts)
            {
                var rotHash = hash << 1 | ((hash >> 31) & 1);
                hash = rotHash ^ disjunct;
            }

            return hash;
        }

        /// <summary>
        /// Return the number of disjuncts that are satisfied in the specified solution (i.e. model).
        /// </summary>
        /// <param name="solution">Solution to test against</param>
        /// <returns>Number of satisfied disjuncts</returns>
        public virtual ushort CountDisjuncts(Solution solution)
        {
            ushort count = 0;
            foreach (var d in Disjuncts)
                if (solution.IsTrue(d))
                    count++;
            return count;
        }
        
        /// <summary>
        /// Is this constraint satisfied if the specified number of disjuncts is satisfied?
        /// </summary>
        /// <param name="satisfiedDisjuncts">Number of satisfied disjuncts</param>
        /// <returns>Whether the constraint is satisfied.</returns>
        public abstract bool IsSatisfied(ushort satisfiedDisjuncts);

        /// <summary>
        /// ThreatCountDelta when current clause is getting one more true disjunct.
        /// </summary>
        public abstract int ThreatCountDeltaIncreasing(ushort count);

        /// <summary>
        /// ThreatCountDelta when current clause is getting one less true disjunct.
        /// </summary>
        public abstract int ThreatCountDeltaDecreasing(ushort count);

        ///<summary>
        /// transit prop appears as a negative literal in clause from false -> true,
        /// OR prop appears as a positive literal in clause from true -> false
        /// </summary>
        public abstract void UpdateTruePositiveAndFalseNegative(BooleanSolver b);

        ///<summary>
        /// transit prop appears as a negative literal in clause from true -> false,
        /// OR prop appears as a positive literal in clause from false -> true
        /// </summary>
        public abstract void UpdateTrueNegativeAndFalsePositive(BooleanSolver b);

        /// <summary>
        /// Find the proposition from the specified clause that will do the least damage to the clauses that are already satisfied.
        /// </summary>
        /// <param name="b">Current BooleanSolver</param>
        /// <returns>Index of the prop to flip</returns>
        public virtual ushort GreedyFlip(BooleanSolver b)
        {
            // If true, the clause has too few disjuncts true
            bool increaseTrueDisjuncts = IsNormalDisjunction ? b.TrueDisjunctCount[Index] <= 0 : b.TrueDisjunctCount[Index] <= MinDisjunctsMinusOne;
            //Signed indices of the disjuncts of the clause
            List<short> disjuncts = UnPredeterminedDisjuncts;
            //Variable that was last chosen for flipping in this clause
            ushort lastFlipOfThisClause = b.LastFlip[Index];

            var bestCount = int.MaxValue;
            var best = 0;

            //Walk disjuncts in a reasonably random order
            var dCount = (uint)disjuncts.Count;
            var index = Random.InRange(dCount);
            var prime = Random.Prime();
            // do prime = Random.Prime(); while (prime <= dCount);
            for (var i = 0; i < dCount; i++)
            {
                var value = disjuncts[(int)index];
                index = (index + prime) % dCount;
                var selectedVar = (ushort)Math.Abs(value);
                var truth = b.Propositions[selectedVar];
                if (value < 0) truth = !truth;
                if (truth == increaseTrueDisjuncts)
                    // This is already the right polarity
                    continue;
                if (selectedVar == lastFlipOfThisClause)
                    continue;
                var threatCount = b.UnsatisfiedClauseDelta(selectedVar);
                if (threatCount <= 0)
                    // Fast path - we've found an improvement; take it
                    // Real WalkSAT would continue searching for the best possible choice, but this
                    // gives better performance in my tests
                    // TODO - see if a faster way of computing ThreatenedClauseCount would improve things.
                    return selectedVar;

                if (threatCount < bestCount)
                {
                    best = selectedVar;
                    bestCount = threatCount;
                }
            }

            if (best == 0)
                return (ushort)Math.Abs(disjuncts.RandomElement());
            return (ushort)best;
        }

        public virtual void SpecialVariableFlipped(BooleanSolver b, ushort var, bool newValue)
        {
            throw new NotImplementedException();
        }

        internal abstract void Decompile(Problem p, StringBuilder b);

        /// <summary>
        /// Generate a textual representation of the constraint for debugging purposes
        /// </summary>
        internal string Decompile(Problem p)
        {
            var b = new StringBuilder();
            Decompile(p, b);
            return b.ToString();
        }

        /// <summary>
        /// Check if this constraint is just a copy of (or identical to) the specified constraint
        /// </summary>
        internal abstract bool EquivalentTo(Constraint c);


        /// <summary>
        /// Check if a constraint's condition literal is satisfied.
        /// Conditional PBC only. Default true for all other types of constraint.
        /// </summary>
        public virtual bool IsEnabled(Solution s) => true;


        /// <summary>
        /// Return the max number of false literals in a constraint.
        /// </summary>
        public abstract bool MaxFalseLiterals(int falseLiterals);
        /// <summary>
        /// Return the max number of true literals in a constraint.
        /// </summary>
        public abstract bool MaxTrueLiterals(int trueLiterals);

        /// <summary>
        /// Updates the list of unsatisfied clauses in the solver when the SAT variable with the specified index is flipped to the specified new value. 
        /// </summary>
        /// <param name="b">The current boolean solver.</param>
        /// <param name="pIndex">The index of the SAT variable that was flipped.</param>
        /// <param name="newValue">The new value of the SAT variable that was flipped.</param>
        public abstract void UpdateCustomConstraint(BooleanSolver b, ushort pIndex, bool newValue);
        
        /// <summary>
        /// Returns the risk/cost associated with flipping the specified SAT variable index to the new specified value.
        /// </summary>
        /// <param name="index">The index corresponding to the SAT variable.</param>
        /// <param name="newValue">The new value of the proposition being flipped.</param>
        /// <returns>An integer, either negative, positive, or zero. Zero corresponds to no risk nor reward. Negative
        /// corresponds to favorable, and positive corresponds to unfavorable.</returns>
        public abstract int CustomFlipRisk(ushort index, bool newValue);

        /// <summary>
        /// Called at the beginning of solving.
        /// </summary>
        public virtual void Reset()
        {
        }
    }
}