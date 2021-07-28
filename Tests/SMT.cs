﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SMT.cs" company="Ian Horswill">
// Copyright (C) 2018 Ian Horswill
//  
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//  
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CatSAT;
using CatSAT.NonBoolean;
using CatSAT.NonBoolean.SMT.Float;
using static CatSAT.Language;
using Random = CatSAT.Random;

namespace Tests
{
    [TestClass]
    public class SMT
    {
        [TestMethod]
        public void ConstantBoundDependencyTest()
        {
            var p = new Problem(nameof(FloatTest));
            var dom = new FloatDomain("unit", 0, 1);
            var x = (FloatVariable) dom.Instantiate("x");
            var a = (Proposition) "a";
            p.Assert((x > 0.1f) > a);
            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Assert.IsTrue(x.Value(s) <= 0.1f || s[a]);
            }
        }

        [TestMethod]
        public void VariableBoundDependencyTest()
        {
            var p = new Problem(nameof(FloatTest));
            var dom = new FloatDomain("unit", 0, 1);
            var x = (FloatVariable) dom.Instantiate("x");
            var y = (FloatVariable) dom.Instantiate("x");
            var a = (Proposition) "a";
            p.Assert((x > y) > a);
            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Assert.IsTrue(x.Value(s) <= y.Value(s) || s[a]);
            }
        }

        [TestMethod]
        public void FloatTest()
        {
            var p = new Problem(nameof(FloatTest));
            var dom = new FloatDomain("unit", 0, 1);
            var x = (FloatVariable) dom.Instantiate("x");
            var y = (FloatVariable) dom.Instantiate("y");
            
            var a = (Proposition) "a";
            var b = (Proposition) "b";
            var c = (Proposition) "c";
            var d = (Proposition) "d";
            var e = (Proposition) "e";
            p.Assert(a > (x > .2f));
            p.Assert(b > (x > .3f));
            p.Assert(c > (x < .5f));
            p.Assert(d > (x < .8f));
            p.Assert(e > (x == y));
            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                var xVal = x.Value(s);
                var yVal = y.Value(s);
                if (s[a])
                    Assert.IsTrue(xVal >= .2f);
                if (s[b])
                    Assert.IsTrue(xVal >= .3f);
                if (s[c])
                    Assert.IsTrue(xVal <= .5f);
                if (s[d])
                    Assert.IsTrue(xVal <= .8f);
                if (s[e])
                    Assert.AreEqual(xVal, yVal);
            }
        }

        [TestMethod]
        // ReSharper disable once IdentifierTypo
        // ReSharper disable once InconsistentNaming
        public void PSMTNPCStatTest()
        {
            var p = new Problem("NPC stats");
            var str = new FloatVariable("str", 0, 10);
            var con = new FloatVariable("con", 0, 10);
            var dex = new FloatVariable("dex", 0, 10);
            var intel = new FloatVariable("int", 0, 10);
            var wis = new FloatVariable("wis", 0, 10);
            var charisma = new FloatVariable("char", 0, 10);
            var classDomain = new FDomain<string>("character class", 
                "fighter", "magic user", "cleric", "thief");
            var cClass = classDomain.Instantiate("class");
            p.Assert(
                (cClass == "fighter") > (str > intel),
                (cClass == "fighter") > (str > 5),
                (cClass == "fighter") > (con > 5),
                (cClass == "fighter") > (intel < 8),
                (cClass == "magic user") > (str < intel),
                (cClass == "magic user") > (intel > 5),
                (cClass == "magic user") > (str < 8),
                (cClass == "cleric") > (wis > 5),
                (cClass == "cleric") > (con < wis),
                (cClass == "thief") > (dex > 5),
                (cClass == "thief") >(charisma > 5),
                (cClass == "thief") > (wis < 5),
                (cClass == "thief") > (dex > str),
                (cClass == "thief") > (charisma > intel)
            );
            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                if (s[(cClass == "fighter")])
                    Assert.IsTrue(str.Value(s) >= intel.Value(s));
                if (s[(cClass == "fighter")])
                    Assert.IsTrue(str.Value(s) >=5);
                if (s[(cClass == "fighter")])
                    Assert.IsTrue(con.Value(s) >= 5);
                if (s[(cClass == "fighter")])
                    Assert.IsTrue(intel.Value(s) <= 8);

                if (s[(cClass == "magic user")])
                    Assert.IsTrue(str.Value(s) <= intel.Value(s));
                if (s[(cClass == "magic user")])
                    Assert.IsTrue(intel.Value(s) >= 5);
                if (s[(cClass == "magic user")])
                    Assert.IsTrue(str.Value(s) <= 8);

                if (s[(cClass == "cleric")])
                    Assert.IsTrue(wis.Value(s) >= 5);
                if (s[(cClass == "cleric")])
                    Assert.IsTrue(con.Value(s) <= wis.Value(s));

                if (s[(cClass == "thief")])
                    Assert.IsTrue(dex.Value(s) >= 5);
                if (s[(cClass == "thief")])
                    Assert.IsTrue(charisma.Value(s) >= 5);
                if (s[(cClass == "thief")])
                    Assert.IsTrue(wis.Value(s) <= 5);
                if (s[(cClass == "thief")])
                    Assert.IsTrue(dex.Value(s) >= str.Value(s));
                if (s[(cClass == "thief")])
                    Assert.IsTrue(charisma.Value(s) >= intel.Value(s));

                Console.WriteLine(s.Model);
            }
        }

        [TestMethod]
        public void FloatPredeteriminationTest()
        {
            var p = new Problem(nameof(GeneralSumConstraintTest));
            var dom = new FloatDomain("unit", -1, 1);
            var x = (FloatVariable) dom.Instantiate("x");
            var y = (FloatVariable) dom.Instantiate("y");
            y.PredeterminedValue = 1;
            var sum = x + y;
            int spuriousHitCount = 0;

            for (int i = 0; i < 100; i++)
            {
                var v = Random.Float(-1, 1);
                x.PredeterminedValue = v;
                var s = p.Solve();
                // Should always give us the predetermined value
                Assert.AreEqual(x.Value(s), v);
                Assert.AreEqual(sum.Value(s), v+1);
                if (i % 2 == 0)
                {
                    x.Reset();
                    s = p.Solve();
                    // Should almost never give us the formerly predetermined value
                    // ReSharper disable CompareOfFloatsByEqualityOperator
                    if (x.Value(s) == v || sum.Value(s) == v+1)
                        // ReSharper restore CompareOfFloatsByEqualityOperator
                        spuriousHitCount++;
                }
            }

            Assert.IsTrue(spuriousHitCount < 3);
        }

        [TestMethod]
        public void GeneralSumConstraintTest()
        {
            var p = new Problem(nameof(GeneralSumConstraintTest));
            var dom = new FloatDomain("unit", -1, 1);
            var vars = new FloatVariable[10];
            for (int i = 0 ; i < vars.Length; i++)
                vars[i] = (FloatVariable) dom.Instantiate("x"+i);
            var sum = FloatVariable.Sum(vars);

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                var realSum = vars.Select(v => v.Value(s)).Sum();
                Assert.IsTrue(Math.Abs(sum.Value(s) - realSum) < 0.00001f);
            }
        }

        [TestMethod]
        public void AverageConstraintTest()
        {
            var p = new Problem(nameof(AverageConstraintTest));
            var dom = new FloatDomain("unit", -1, 1);
            var vars = new FloatVariable[10];
            for (int i = 0 ; i < vars.Length; i++)
                vars[i] = (FloatVariable) dom.Instantiate("x"+i);
            var average = FloatVariable.Average(vars);

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                var avg = vars.Select(v => v.Value(s)).Average();
                Assert.IsTrue(Math.Abs(average.Value(s) - avg) < 0.00001f);
            }
        }

        [TestMethod]
        public void SumConstraintTest()
        {
            var p = new Problem(nameof(SumConstraintTest));
            var dom = new FloatDomain("unit", 0, 1);
            var x = (FloatVariable) dom.Instantiate("x");
            var y = (FloatVariable) dom.Instantiate("y");
            //p.Assert("foo");
            var sum = x + y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                Assert.IsTrue(Math.Abs(sum.Value(s) - (x.Value(s)+y.Value(s))) < 0.00001f);
            }
        }

        [TestMethod]
        public void ConstantProductConstraintTest()
        {
            for (int j = 0; j < 100; j++)
            {
                var p = new Problem(nameof(ConstantProductConstraintTest));
                var dom = new FloatDomain("signed unit", -1, 1);
                var x = (FloatVariable) dom.Instantiate("x");
                var c = Random.Float(-100, 100);

                var product = c * x;

                for (int i = 0; i < 100; i++)
                {
                    var s = p.Solve();
                    Console.WriteLine(s.Model);
                    Assert.IsTrue(Math.Abs(product.Value(s) / (x.Value(s)) - c) < 0.0001f);
                }
            }
        }

        [TestMethod]
        public void UnsignedProductTest()
        {
            var p = new Problem(nameof(UnsignedProductTest));
            var dom = new FloatDomain("unit", 0, 1);
            var x = (FloatVariable) dom.Instantiate("x");
            var y = (FloatVariable) dom.Instantiate("y");
            //p.Assert("foo");
            var product = x * y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                Assert.IsTrue(Math.Abs(product.Value(s) - x.Value(s)*y.Value(s)) < 0.00001f);
            }
        }

        [TestMethod]
        public void SignedProductTest()
        {
            var p = new Problem(nameof(SignedProductTest));
            var dom = new FloatDomain("signed unit", -1, 1);
            var x = (FloatVariable) dom.Instantiate("x");
            var y = (FloatVariable) dom.Instantiate("y");
            //p.Assert("foo");
            var product = x * y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                Assert.IsTrue(Math.Abs(product.Value(s) - x.Value(s)*y.Value(s)) < 0.00001f);
            }
        }


        [TestMethod]
        public void UnsignedDivisionConstraintTest()
        {
            var p = new Problem(nameof(UnsignedDivisionConstraintTest));
            var dom = new FloatDomain("unit", 1, 50);
            var dom2 = new FloatDomain("unit2", 1, 10);
            var x = (FloatVariable)dom.Instantiate("x");
            var y = (FloatVariable)dom2.Instantiate("y");
            var quotient = x / y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                Assert.IsTrue(Math.Abs(quotient.Value(s) - (x.Value(s) / y.Value(s))) < 0.00001f);
            }
        }

        [TestMethod]
        public void SignedDifferenceTest()
        {
            var p = new Problem(nameof(SignedDifferenceTest));
            var dom = new FloatDomain("signed unit", -1, 1);
            var x = (FloatVariable)dom.Instantiate("x");
            var y = (FloatVariable)dom.Instantiate("y");
            var diff = x - y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                Assert.IsTrue(Math.Abs(diff.Value(s) - (x.Value(s) - y.Value(s))) < 0.00001f);
            }
        }


        [TestMethod]
        public void SignedQuotientTest()
        {
            var p = new Problem(nameof(SignedQuotientTest));
            var dom = new FloatDomain("signed unit", 1, 10);
            var x = (FloatVariable)dom.Instantiate("x");
            var y = (FloatVariable)dom.Instantiate("y");
            //p.Assert("foo");
            var diff = x / y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                Assert.IsTrue(Math.Abs(diff.Value(s) - (x.Value(s) / y.Value(s))) < 0.00001f);
            }
        }

        [TestMethod]
        public void NaiveSquareTest()
        {
            var p = new Problem(nameof(NaiveSquareTest));
            var dom = new FloatDomain("signed unit", -1, 1);
            var x = (FloatVariable) dom.Instantiate("x");
            //p.Assert("foo");
            var square = x * x;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                Assert.IsTrue(Math.Abs(square.Value(s) - x.Value(s)*x.Value(s)) < 0.00001f);
            }
        }

        /// <summary>
        /// Test that FloatVariables that aren't supposed to be defined actually aren't defined.
        /// </summary>
        [TestMethod, ExpectedException(typeof(UndefinedVariableException))]
        public void UndefinedFloatVarTest()
        {
            var p = new Problem("test");
            var dom = new FloatDomain("signed unit", -1, 1);
            var prop = (Proposition) "prop";
            var x = new FloatVariable("x", dom, prop, p);
            p.Assert(Not(prop));
            Solution s = null;
            for (int i = 0; i < 100; i++)
            {
                s = p.Solve();
                Console.WriteLine(s.Model);
                Assert.IsFalse(s[prop]);
                Assert.IsFalse(x.IsDefinedInInternal(s));
            }
            Console.WriteLine(x.Value(s));
        }

        /// <summary>
        /// Check that if a defined var is equated to an undefined var, the equation has no effect
        /// </summary>
        [TestMethod]
        public void UndefinedFloatVarEquationHasNoEffectTest()
        {
            var p = new Problem("test");
            var dom = new FloatDomain("signed unit", -1, 1);
            var prop = (Proposition) "prop";
            var x = new FloatVariable("x", dom, prop, p);
            var y = (FloatVariable) dom.Instantiate("y");
            p.Assert(Not(prop));
            p.Assert(x == y);

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Assert.IsFalse(x.IsDefinedInInternal(s));
                Assert.IsTrue(y.IsDefinedInInternal(s));
                Assert.IsFalse(ReferenceEquals(x.Representative, y.Representative));
            }
        }

        /// <summary>
        /// Check that if a defined var is bounded by an undefined var, the bound has no effect
        /// </summary>
        [TestMethod]
        public void UndefinedFloatVarBoundHasNoEffectTest()
        {
            var p = new Problem("test");
            var dom = new FloatDomain("signed unit", -1, 1);
            var prop = (Proposition) "prop";
            var x = new FloatVariable("x", dom, prop, p);
            var y = (FloatVariable) dom.Instantiate("y");
            p.Assert(Not(prop));
            p.Assert(x > y);

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Assert.IsFalse(x.IsDefinedInInternal(s));
                Assert.IsTrue(y.IsDefinedInInternal(s));
                Assert.IsTrue(ReferenceEquals(y, y.Representative));
                Assert.IsTrue(y.UpperVariableBounds == null);
            }
        }

        /// <summary>
        /// Check that if a defined var is functionally constrained by an undefined var, the constraint has no effect
        /// </summary>
        [TestMethod]
        public void UndefinedFloatVarFunctionalConstraintHasNoEffectTest()
        {
            var p = new Problem("test");
            var dom = new FloatDomain("signed unit", -1, 1);
            var prop = (Proposition) "prop";
            var x = new FloatVariable("x", dom, prop, p);
            var y = (FloatVariable) dom.Instantiate("y");
            var sum = x + y;

            Assert.IsTrue(ReferenceEquals(prop, sum.Condition));

            p.Assert(Not(prop));
            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Assert.IsFalse(x.IsDefinedInInternal(s));
                Assert.IsTrue(y.IsDefinedInInternal(s));
                Assert.IsTrue(ReferenceEquals(y, y.Representative));
                Assert.IsFalse(sum.IsDefinedInInternal(s));
                Assert.IsTrue(y.ActiveFunctionalConstraints == null);
            }
        }

        /// <summary>
        /// Check that a sum is defined iff its inputs are defined, and that when it is defined it
        /// is in fact the sum.
        /// </summary>
        [TestMethod]
        public void SumConditionTest()
        {
            var problem = new Problem("test");
            var dom = new FloatDomain("signed unit", -1, 1);
            var p = (Proposition) "p";
            var x = (FloatVariable) dom.Instantiate("x", problem, p);
            var q = (Proposition) "q";
            var y = (FloatVariable) dom.Instantiate("y", problem, q);
            var sum = x + y;
            Assert.IsTrue(ReferenceEquals(sum.Condition, problem.Conjunction(p, q)));

            for (int i = 0; i < 100; i++)
            {
                var s = problem.Solve();
                Console.WriteLine(s.Model);
                Assert.AreEqual(sum.IsDefinedInInternal(s), x.IsDefinedInInternal(s) & y.IsDefinedInInternal(s));
                if (sum.IsDefinedInInternal(s))
                    Assert.IsTrue(Math.Abs(sum.Value(s) - (x.Value(s) + y.Value(s))) < 0.00001f);
            }
        }

        [TestMethod]
        public void SumTableTest()
        {
            var dom = new FloatDomain("unit", 0, 1);
            var x = (FloatVariable)dom.Instantiate("x");
            var y = (FloatVariable)dom.Instantiate("y");
            var z = (FloatVariable)dom.Instantiate("z");

            Assert.AreEqual(x + y, x + y);
            Assert.AreNotEqual(x + y, x + z);
        }

        [TestMethod]
        public void ProductTableTest()
        {
            var dom = new FloatDomain("unit", 0, 1);
            var x = (FloatVariable)dom.Instantiate("x");
            var y = (FloatVariable)dom.Instantiate("y");
            var z = (FloatVariable)dom.Instantiate("z");
            Assert.AreEqual(x * y, x * y);
            Assert.AreNotEqual(x * y, x * z);
        }

        [TestMethod][ExpectedException(typeof(ArgumentException))]
        public void QuantizationSumErrorTest()
        {
            var dom1 = new FloatDomain("unit1", 1, 2, 0.5f);
            var dom2 = new FloatDomain("unit2", 0, 1, 1.0f);

            var badSum = dom1 + dom2;
        }


        [TestMethod][ExpectedException(typeof(ArgumentException))]
        public void QuantizationProductErrorTest()
        {
            var dom1 = new FloatDomain("unit1", 1, 2, 0.5f);
            var dom2 = new FloatDomain("unit2", 0, 1, 1.0f);

            var badProduct = dom1 * dom2;
        }

        [TestMethod][ExpectedException(typeof(ArgumentException))]
        public void QuantizationDifferenceErrorTest()
        {
            var dom1 = new FloatDomain("unit1", 1, 2, 0.5f);
            var dom2 = new FloatDomain("unit2", 0, 1, 1.0f);

            var badDiff = dom1 - dom2;
        }

        [TestMethod]
        public void DomainSumTest()
        {
            var p = new Problem(nameof(DomainSumTest));
            var dom = new FloatDomain("unit", -2, 4, 0.5f);
            var dom2 = new FloatDomain("unit2", 0, 1, 0.5f);
            var newDom = dom + dom2;

            Assert.AreEqual(newDom.Bounds.Lower, -2);
            Assert.AreEqual(newDom.Bounds.Upper, 5);
            Assert.AreEqual(newDom.Quantization, 0.5f);
        }

        [TestMethod]
        public void DomainProductTest()
        {
            var p = new Problem(nameof(DomainProductTest));
            var dom = new FloatDomain("unit", -2, 4, 0.5f);
            var dom2 = new FloatDomain("unit2", 0, 1, 0.5f);
            var newDom = dom * dom2;

            Assert.AreEqual(newDom.Bounds.Lower, 0);
            Assert.AreEqual(newDom.Bounds.Upper, 4);
            Assert.AreEqual(newDom.Quantization, 0.5f);
        }


        [TestMethod]
        public void DomainDifferenceTest()
        {
            var p = new Problem(nameof(DomainDifferenceTest));
            var dom = new FloatDomain("unit", -2, 4, 0.5f);
            var dom2 = new FloatDomain("unit2", 0, 1, 0.5f);
            var newDom = dom - dom2;

            Assert.AreEqual(newDom.Bounds.Lower, -3);
            Assert.AreEqual(newDom.Bounds.Upper, 4);
            Assert.AreEqual(newDom.Quantization, 0.5f);
        }


        void CheckVariables(Solution s, params FloatVariable[] variables)
        {
            foreach (var v in variables)
                Assert.IsTrue(v.FloatDomain.Contains(v.Value(s)));
        }

        [TestMethod]
        public void QuantizedSumTest()
        {
            var p = new Problem(nameof(QuantizedSumTest));
            var dom = new FloatDomain("unit", -1, 2, 1);
            var dom2 = new FloatDomain("unit2", 0, 1, 1);
            var newDom = dom * dom2;

            var x = (FloatVariable)newDom.Instantiate("x");
            var y = (FloatVariable)newDom.Instantiate("y");

            var sum = x + y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                CheckVariables(s,x,y,sum);
                Assert.IsTrue(Math.Abs(sum.Value(s) - (x.Value(s) + y.Value(s))) < 0.00001f);
            }
        }

        [TestMethod]
        public void QuantizedBoundTest()
        {
            var p = new Problem(nameof(QuantizedBoundTest));
            var dom = new FloatDomain("unit", -1, 70, 7);

            var x = (FloatVariable)dom.Instantiate("x");

            //Assert.AreEqual(x.Bounds.Upper, 70);
            //Assert.AreEqual(x.Bounds.Lower, 0);

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                CheckVariables(s, x);
            }
        }


        [TestMethod]
        public void QuantizedBoundTest2()
        {
            var p = new Problem(nameof(QuantizedBoundTest2));
            var dom = new FloatDomain("unit", -14.3f, 97.1f, .7f);

            var x = (FloatVariable)dom.Instantiate("x");

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                CheckVariables(s, x);
            }
        }


        [TestMethod]
        public void QuantizedBoundTest3()
        {
            var p = new Problem(nameof(QuantizedBoundTest3));
            var dom = new FloatDomain("unit", 1, 11, .9f);

            var x = (FloatVariable)dom.Instantiate("x");

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                CheckVariables(s, x);
            }
        }

        [TestMethod]
        public void QuantizedSumConstraintTest()
        {
            var p = new Problem(nameof(QuantizedSumConstraintTest));
            var dom = new FloatDomain("unit", 0, 1, 0.5f);
            var dom2 = new FloatDomain("unit2", 0, 5, 0.5f);
            var x = (FloatVariable)dom.Instantiate("x");
            var y = (FloatVariable)dom2.Instantiate("y");
            //p.Assert("foo");
            var sum = x + y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                CheckVariables(s, sum, x, y);
                Assert.IsTrue(Math.Abs(sum.Value(s) - (x.Value(s) + y.Value(s))) < 0.00001f);
            }
        }

        [TestMethod]
        public void QuantizedProductTest()
        {
            var p = new Problem(nameof(QuantizedProductTest));
            var dom = new FloatDomain("unit", 0, 1, 0.5f);
            var dom2 = new FloatDomain("unit2", 0, 5, 0.5f);
            var x = (FloatVariable)dom.Instantiate("x");
            var y = (FloatVariable)dom2.Instantiate("y");
            var prod = x * y;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                CheckVariables(s, prod, x, y);
                Assert.IsTrue(Math.Abs(prod.Value(s) - (x.Value(s) * y.Value(s))) < 0.00001f);
            }
        }


        [TestMethod]
        public void QuantizedSelfDivTest()
        {
            var p = new Problem(nameof(QuantizedSelfDivTest));
            var dom = new FloatDomain("unit", 1, 10, 0.9f);
            var x = (FloatVariable)dom.Instantiate("x");
            var div = x / x;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                CheckVariables(s, div, x);
                Assert.IsTrue(Math.Abs(div.Value(s) - (x.Value(s) / x.Value(s))) < 0.00001f);
            }
        }

        [TestMethod]
        public void QuantizedSelfSubtractionTest()
        {
            var p = new Problem(nameof(QuantizedSelfSubtractionTest));
            var dom = new FloatDomain("unit", 1, 10, 0.9f);
            var x = (FloatVariable)dom.Instantiate("x");
            var diff = x - x;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                CheckVariables(s, diff, x);
                Assert.IsTrue(Math.Abs(diff.Value(s) - (x.Value(s) - x.Value(s))) < 0.00001f);
            }
        }

        [TestMethod]
        public void QuantizedNaiveSquareTest()
        {
            var p = new Problem(nameof(QuantizedNaiveSquareTest));
            var dom = new FloatDomain("signed unit", -1, 1, 0.5f);
            var x = (FloatVariable)dom.Instantiate("x");
            //p.Assert("foo");
            var square = x * x;

            for (int i = 0; i < 100; i++)
            {
                var s = p.Solve();
                Console.WriteLine(s.Model);
                CheckVariables(s, square, x);
                Assert.IsTrue(Math.Abs(square.Value(s) - x.Value(s) * x.Value(s)) < 0.00001f);
            }
        }

        [TestMethod]
        public void FloatPredeterminationTest()
        {
            var problem = new Problem("test");
            var dom = new FloatDomain("signed unit", -1, 1);
            var x = (FloatVariable) dom.Instantiate("x", problem);
            var y = (FloatVariable) dom.Instantiate("y", problem);
            var z = (FloatVariable) dom.Instantiate("z", problem);
            var bigThresh = 1.5f;
            var smallThresh = -2.3f;
            var xBig = x > bigThresh;
            var ySmall = y < smallThresh;
            var zBig = z > bigThresh;
            var zSmall = z < smallThresh;
            var xLTy = x < y;
            var xGTz = x > z;
            var yLTz = y < z;

            for (var i = 0; i < 100; i++)
            {
                var xVal = Random.Float(-1, 1);
                var yVal = Random.Float(-1, 1);
                x.PredeterminedValue = xVal;
                y.PredeterminedValue = yVal;
                var s = problem.Solve();
                Assert.AreEqual(xVal, x.Value(s));
                Assert.AreEqual(yVal, y.Value(s));
                
                Assert.IsTrue(problem.IsPredetermined(xBig));
                Assert.AreEqual(s[xBig], xVal >= bigThresh);

                Assert.IsTrue(problem.IsPredetermined(ySmall));
                Assert.AreEqual(s[ySmall], yVal <= smallThresh);

                Assert.IsTrue(problem.IsPredetermined(xLTy));
                Assert.AreEqual(s[xLTy], xVal <= yVal);

                Assert.IsFalse(problem.IsPredetermined(zBig));
                Assert.IsFalse(problem.IsPredetermined(zSmall));

                Assert.IsFalse(problem.IsPredetermined(xGTz));
                Assert.IsFalse(problem.IsPredetermined(yLTz));
            }
        }
    }
}
