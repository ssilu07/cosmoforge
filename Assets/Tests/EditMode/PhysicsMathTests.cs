// ============================================================================
// CosmoForge: EditMode Unit Tests for Double-Precision Vector Math & Floating Origin
// Conforms to PROJECT.md § Architecture, Features F01 & F03
// ============================================================================

namespace CosmoForge.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Core.Performance;

    [TestFixture]
    public class PhysicsMathTests
    {
        private const double Epsilon = 1e-12;

        [TearDown]
        public void TearDown()
        {
            FloatingOrigin.ResetOrigin();
        }

        [Test]
        public void Vector3d_ConstructorAndComponents_InitializeAccurately()
        {
            var v = new Vector3d(12.345, -67.89, 101.112);
            Assert.AreEqual(12.345, v.x, Epsilon);
            Assert.AreEqual(-67.89, v.y, Epsilon);
            Assert.AreEqual(101.112, v.z, Epsilon);
        }

        [Test]
        public void Vector3d_AdditionAndSubtraction_CalculatesAccurately()
        {
            var a = new Vector3d(10.0, -5.0, 3.5);
            var b = new Vector3d(2.5, 5.0, -1.5);

            var sum = a + b;
            Assert.AreEqual(12.5, sum.x, Epsilon);
            Assert.AreEqual(0.0, sum.y, Epsilon);
            Assert.AreEqual(2.0, sum.z, Epsilon);

            var diff = a - b;
            Assert.AreEqual(7.5, diff.x, Epsilon);
            Assert.AreEqual(-10.0, diff.y, Epsilon);
            Assert.AreEqual(5.0, diff.z, Epsilon);

            var zeroDiff = a - a;
            Assert.AreEqual(0.0, zeroDiff.sqrMagnitude, Epsilon);
        }

        [Test]
        public void Vector3d_ScalarMultiplicationAndDivision_BehavesLinearly()
        {
            var v = new Vector3d(2.0, -4.0, 6.0);

            var rightMult = v * 2.5;
            var leftMult = 2.5 * v;
            Assert.AreEqual(5.0, rightMult.x, Epsilon);
            Assert.AreEqual(-10.0, rightMult.y, Epsilon);
            Assert.AreEqual(15.0, rightMult.z, Epsilon);
            Assert.AreEqual(rightMult.x, leftMult.x, Epsilon);

            var div = v / 2.0;
            Assert.AreEqual(1.0, div.x, Epsilon);
            Assert.AreEqual(-2.0, div.y, Epsilon);
            Assert.AreEqual(3.0, div.z, Epsilon);

            var neg = -v;
            Assert.AreEqual(-2.0, neg.x, Epsilon);
            Assert.AreEqual(4.0, neg.y, Epsilon);
            Assert.AreEqual(-6.0, neg.z, Epsilon);
        }

        [Test]
        public void Vector3d_MagnitudeAndSqrMagnitude_CalculatesExactEuclideanNorm()
        {
            // Pythagorean quadruple: 3^2 + 4^2 + 12^2 = 9 + 16 + 144 = 169 = 13^2
            var v = new Vector3d(3.0, 4.0, 12.0);
            Assert.AreEqual(169.0, v.sqrMagnitude, Epsilon);
            Assert.AreEqual(13.0, v.magnitude, Epsilon);

            var zero = Vector3d.zero;
            Assert.AreEqual(0.0, zero.sqrMagnitude, Epsilon);
            Assert.AreEqual(0.0, zero.magnitude, Epsilon);
        }

        [Test]
        public void Vector3d_DotProduct_SatisfiesGeometricProperties()
        {
            var a = new Vector3d(1.0, 0.0, 0.0);
            var b = new Vector3d(0.0, 1.0, 0.0);
            var c = new Vector3d(2.0, 0.0, 0.0);
            var d = new Vector3d(-3.0, 0.0, 0.0);

            // Orthogonal
            Assert.AreEqual(0.0, Vector3d.Dot(a, b), Epsilon);

            // Parallel
            Assert.AreEqual(2.0, Vector3d.Dot(a, c), Epsilon);

            // Anti-parallel
            Assert.AreEqual(-3.0, Vector3d.Dot(a, d), Epsilon);

            // Commutativity
            Assert.AreEqual(Vector3d.Dot(a, c), Vector3d.Dot(c, a), Epsilon);

            // Dot with self equals sqrMagnitude
            var v = new Vector3d(3.0, -4.0, 5.0);
            Assert.AreEqual(v.sqrMagnitude, Vector3d.Dot(v, v), Epsilon);
        }

        [Test]
        public void Vector3d_CrossProduct_FollowsRightHandRuleAndAntiSymmetry()
        {
            var i = new Vector3d(1.0, 0.0, 0.0);
            var j = new Vector3d(0.0, 1.0, 0.0);
            var k = new Vector3d(0.0, 0.0, 1.0);

            var crossIJ = Vector3d.Cross(i, j);
            Assert.AreEqual(k.x, crossIJ.x, Epsilon);
            Assert.AreEqual(k.y, crossIJ.y, Epsilon);
            Assert.AreEqual(k.z, crossIJ.z, Epsilon);

            var crossJK = Vector3d.Cross(j, k);
            Assert.AreEqual(i.x, crossJK.x, Epsilon);
            Assert.AreEqual(i.y, crossJK.y, Epsilon);
            Assert.AreEqual(i.z, crossJK.z, Epsilon);

            // Anti-symmetry: a x b = -(b x a)
            var crossJI = Vector3d.Cross(j, i);
            Assert.AreEqual(-k.x, crossJI.x, Epsilon);
            Assert.AreEqual(-k.y, crossJI.y, Epsilon);
            Assert.AreEqual(-k.z, crossJI.z, Epsilon);

            // Cross with self is zero
            var crossII = Vector3d.Cross(i, i);
            Assert.AreEqual(0.0, crossII.sqrMagnitude, Epsilon);

            // Result is orthogonal to both operands
            var a = new Vector3d(1.2, -3.4, 5.6);
            var b = new Vector3d(-7.8, 9.0, -1.2);
            var axb = Vector3d.Cross(a, b);
            Assert.AreEqual(0.0, Vector3d.Dot(axb, a), Epsilon);
            Assert.AreEqual(0.0, Vector3d.Dot(axb, b), Epsilon);
        }

        [Test]
        public void Vector3d_Normalization_HandlesNormalAndNearZeroRobustly()
        {
            var v = new Vector3d(0.0, 3.0, 4.0);
            var norm = v.normalized;
            Assert.AreEqual(0.0, norm.x, Epsilon);
            Assert.AreEqual(0.6, norm.y, Epsilon);
            Assert.AreEqual(0.8, norm.z, Epsilon);
            Assert.AreEqual(1.0, norm.magnitude, Epsilon);

            // Exact zero vector
            var zeroNorm = Vector3d.zero.normalized;
            Assert.AreEqual(0.0, zeroNorm.x);
            Assert.AreEqual(0.0, zeroNorm.y);
            Assert.AreEqual(0.0, zeroNorm.z);
            Assert.IsFalse(double.IsNaN(zeroNorm.x));
            Assert.IsFalse(double.IsInfinity(zeroNorm.x));

            // Near-zero vector below threshold (1e-16)
            var tiny = new Vector3d(1e-16, 0.0, 0.0);
            var tinyNorm = tiny.normalized;
            Assert.AreEqual(0.0, tinyNorm.x);
            Assert.AreEqual(0.0, tinyNorm.y);
            Assert.AreEqual(0.0, tinyNorm.z);
            Assert.IsFalse(double.IsNaN(tinyNorm.x));
        }

        [Test]
        public void Vector3d_PrecisionRetention_RetainsMillimeterPrecisionAtAstronomicalScales()
        {
            // Astronomical scale: 1 AU = 1.496e11 meters
            double astronomicalScale = 1.496e11;
            double millimeterDisplacement = 0.001; // 1 mm

            var vDouble = new Vector3d(astronomicalScale, 0, 0);
            var vShifted = vDouble + new Vector3d(millimeterDisplacement, 0, 0);
            double deltaDouble = (vShifted - vDouble).x;

            Assert.AreEqual(millimeterDisplacement, deltaDouble, 2e-5,
                "Vector3d must retain millimeter displacements at astronomical AU distances.");

            // Contrast with float failure
            float floatScale = (float)astronomicalScale;
            float floatDisp = (float)millimeterDisplacement;
            float[] mem = new float[] { floatScale + floatDisp };
            float deltaFloat = mem[0] - floatScale;
            Assert.AreEqual(0.0f, deltaFloat,
                "Float must lose precision (proving why Vector3d is mandatory).");
        }

        [Test]
        public void Vector3d_ConversionToAndFromVector3_PreservesCoordinates()
        {
            var original = new Vector3d(12.345, -67.89, 101.112);
            Vector3 v3 = original.ToVector3();
            Assert.AreEqual(12.345f, v3.x, 1e-4f);
            Assert.AreEqual(-67.89f, v3.y, 1e-4f);
            Assert.AreEqual(101.112f, v3.z, 1e-4f);

            Vector3d roundTrip = Vector3d.FromVector3(v3);
            Assert.AreEqual(v3.x, (float)roundTrip.x);
            Assert.AreEqual(v3.y, (float)roundTrip.y);
            Assert.AreEqual(v3.z, (float)roundTrip.z);
        }

        [Test]
        public void FloatingOrigin_ShiftOrigin_OffsetsCurrentOriginAndFiresEvent()
        {
            Vector3d detectedShift = Vector3d.zero;
            System.Action<Vector3d> handler = shift => detectedShift = shift;

            FloatingOrigin.OnOriginShift += handler;
            try
            {
                var shiftDelta = new Vector3d(5000.0, -2500.0, 1000.0);
                FloatingOrigin.ShiftOrigin(shiftDelta);

                Assert.AreEqual(shiftDelta.x, FloatingOrigin.CurrentOrigin.x, Epsilon);
                Assert.AreEqual(shiftDelta.y, FloatingOrigin.CurrentOrigin.y, Epsilon);
                Assert.AreEqual(shiftDelta.z, FloatingOrigin.CurrentOrigin.z, Epsilon);
                Assert.AreEqual(shiftDelta.x, detectedShift.x, Epsilon);
            }
            finally
            {
                FloatingOrigin.OnOriginShift -= handler;
            }
        }

        [Test]
        public void FloatingOrigin_AbsoluteToRender_And_RenderToAbsolute_InverseCorrect()
        {
            var originOffset = new Vector3d(10000.0, 20000.0, 30000.0);
            FloatingOrigin.ShiftOrigin(originOffset);

            var absolutePos = new Vector3d(10100.5, 20200.25, 30300.75);
            Vector3 renderPos = FloatingOrigin.AbsoluteToRender(absolutePos);

            Assert.AreEqual(100.5f, renderPos.x, 1e-4f);
            Assert.AreEqual(200.25f, renderPos.y, 1e-4f);
            Assert.AreEqual(300.75f, renderPos.z, 1e-4f);

            Vector3d roundTripAbs = FloatingOrigin.RenderToAbsolute(renderPos);
            Assert.AreEqual(absolutePos.x, roundTripAbs.x, 1e-3);
            Assert.AreEqual(absolutePos.y, roundTripAbs.y, 1e-3);
            Assert.AreEqual(absolutePos.z, roundTripAbs.z, 1e-3);
        }
    }
}
