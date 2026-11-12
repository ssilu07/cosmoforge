// ============================================================================
// CosmoForge Test Framework: Physical Assertions & Conservation Invariant Checks
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using CosmoForge.Core.Math;
using CosmoForge.Celestial.Data;

namespace CosmoForge.Tests.Harness
{
    /// <summary>
    /// Specialized assertion library for astronomical N-body simulations,
    /// Hamiltonian conservation laws, relativistic bounds, and puzzle criteria.
    /// </summary>
    public static class TestAssertions
    {
        public const double DefaultDoubleTolerance = 1e-9;
        public const double DefaultEnergyDriftTolerance = 1e-4;
        public const double DefaultMomentumDriftTolerance = 1e-6;

        public static void AssertApproximatelyEqual(double expected, double actual, double tolerance = DefaultDoubleTolerance, string message = null)
        {
            Assert.IsFalse(double.IsNaN(actual), $"[NaN Error] Actual value is NaN. {message}");
            Assert.IsFalse(double.IsInfinity(actual), $"[Infinity Error] Actual value is Infinity. {message}");
            double diff = Math.Abs(expected - actual);
            Assert.LessOrEqual(diff, tolerance,
                $"[Precision Mismatch] Expected: {expected:E9}, Actual: {actual:E9}, Diff: {diff:E9} exceeds tolerance {tolerance:E9}. {message}");
        }

        public static void AssertApproximatelyEqual(Vector3d expected, Vector3d actual, double tolerance = DefaultDoubleTolerance, string message = null)
        {
            AssertNoNaNOrInfinity(actual, message);
            double diff = (expected - actual).magnitude;
            Assert.LessOrEqual(diff, tolerance,
                $"[Vector3d Mismatch] Expected: {expected}, Actual: {actual}, Magnitude Diff: {diff:E9} exceeds tolerance {tolerance:E9}. {message}");
        }

        public static void AssertEnergyConserved(double initialEnergy, double finalEnergy, double maxDriftFraction = DefaultEnergyDriftTolerance, string context = null)
        {
            Assert.IsFalse(double.IsNaN(initialEnergy), $"Initial energy is NaN. {context}");
            Assert.IsFalse(double.IsNaN(finalEnergy), $"Final energy is NaN. {context}");
            Assert.AreNotEqual(0.0, initialEnergy, $"Initial energy cannot be zero for relative drift computation. {context}");

            double relativeDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);
            Assert.LessOrEqual(relativeDrift, maxDriftFraction,
                $"[Secular Energy Drift Violation] Initial E: {initialEnergy:E8}, Final E: {finalEnergy:E8}, Drift: {relativeDrift:E6} exceeds allowed limit {maxDriftFraction:E6}. {context}");
        }

        public static void AssertLinearMomentumConserved(Vector3d initialP, Vector3d finalP, double maxDriftAbsolute = DefaultMomentumDriftTolerance, string context = null)
        {
            AssertNoNaNOrInfinity(initialP, "Initial linear momentum");
            AssertNoNaNOrInfinity(finalP, "Final linear momentum");

            double driftMag = (finalP - initialP).magnitude;
            Assert.LessOrEqual(driftMag, maxDriftAbsolute,
                $"[Linear Momentum Violation] Initial P: {initialP}, Final P: {finalP}, Drift: {driftMag:E6} exceeds allowed absolute limit {maxDriftAbsolute:E6}. {context}");
        }

        public static void AssertAngularMomentumConserved(Vector3d initialL, Vector3d finalL, double maxDriftFraction = DefaultMomentumDriftTolerance, string context = null)
        {
            AssertNoNaNOrInfinity(initialL, "Initial angular momentum");
            AssertNoNaNOrInfinity(finalL, "Final angular momentum");

            double initialMag = initialL.magnitude;
            double driftMag = (finalL - initialL).magnitude;

            if (initialMag > 1e-12)
            {
                double fractionalDrift = driftMag / initialMag;
                Assert.LessOrEqual(fractionalDrift, maxDriftFraction,
                    $"[Angular Momentum Violation] Initial L: {initialL} (|L|={initialMag:E6}), Final L: {finalL}, Fractional Drift: {fractionalDrift:E6} exceeds allowed limit {maxDriftFraction:E6}. {context}");
            }
            else
            {
                Assert.LessOrEqual(driftMag, maxDriftFraction,
                    $"[Angular Momentum Violation] Initial L is near zero. Absolute Drift: {driftMag:E6} exceeds allowed limit {maxDriftFraction:E6}. {context}");
            }
        }

        public static void AssertNoNaNOrInfinity(Vector3d v, string name = "Vector3d")
        {
            Assert.IsFalse(double.IsNaN(v.x), $"{name}.x is NaN");
            Assert.IsFalse(double.IsNaN(v.y), $"{name}.y is NaN");
            Assert.IsFalse(double.IsNaN(v.z), $"{name}.z is NaN");
            Assert.IsFalse(double.IsInfinity(v.x), $"{name}.x is Infinity");
            Assert.IsFalse(double.IsInfinity(v.y), $"{name}.y is Infinity");
            Assert.IsFalse(double.IsInfinity(v.z), $"{name}.z is Infinity");
        }

        public static void AssertNoNaNOrInfinity(CelestialBodyState body)
        {
            Assert.IsNotNull(body, "CelestialBodyState is null");
            Assert.IsFalse(double.IsNaN(body.Mass), $"Body '{body.Name}' (Id {body.Id}) Mass is NaN");
            Assert.IsFalse(double.IsNaN(body.Radius), $"Body '{body.Name}' (Id {body.Id}) Radius is NaN");
            Assert.IsFalse(double.IsNaN(body.SurfaceTemperature), $"Body '{body.Name}' (Id {body.Id}) Temperature is NaN");
            Assert.IsFalse(double.IsNaN(body.CoreFuel), $"Body '{body.Name}' (Id {body.Id}) CoreFuel is NaN");

            AssertNoNaNOrInfinity(body.Position, $"Body '{body.Name}' Position");
            AssertNoNaNOrInfinity(body.Velocity, $"Body '{body.Name}' Velocity");
            AssertNoNaNOrInfinity(body.Acceleration, $"Body '{body.Name}' Acceleration");
        }

        public static void AssertNoNaNOrInfinity(IEnumerable<CelestialBodyState> bodies)
        {
            Assert.IsNotNull(bodies, "Bodies collection is null");
            foreach (var body in bodies)
            {
                if (body != null && !body.IsDestroyed)
                {
                    AssertNoNaNOrInfinity(body);
                }
            }
        }

        public static void AssertOrbitalEccentricityInRange(double eccentricity, double min, double max, string context = null)
        {
            Assert.IsFalse(double.IsNaN(eccentricity), $"Eccentricity is NaN. {context}");
            Assert.GreaterOrEqual(eccentricity, min, $"Eccentricity {eccentricity:F4} is below minimum {min:F4}. {context}");
            Assert.LessOrEqual(eccentricity, max, $"Eccentricity {eccentricity:F4} exceeds maximum {max:F4}. {context}");
        }

        public static void AssertWithinRocheLimit(double currentDistance, double rocheLimit, string context = null)
        {
            Assert.LessOrEqual(currentDistance, rocheLimit,
                $"Distance {currentDistance:F2} is outside Roche limit {rocheLimit:F2}. {context}");
        }

        public static void AssertOutsideRocheLimit(double currentDistance, double rocheLimit, string context = null)
        {
            Assert.Greater(currentDistance, rocheLimit,
                $"Distance {currentDistance:F2} has penetrated Roche limit {rocheLimit:F2}. {context}");
        }

        public static void AssertTemperatureInRange(double temperatureK, double minK, double maxK, string context = null)
        {
            Assert.IsFalse(double.IsNaN(temperatureK), $"Temperature is NaN. {context}");
            Assert.GreaterOrEqual(temperatureK, minK, $"Temperature {temperatureK:F1} K is below {minK:F1} K. {context}");
            Assert.LessOrEqual(temperatureK, maxK, $"Temperature {temperatureK:F1} K is above {maxK:F1} K. {context}");
        }

        public static void AssertMomentumConservedInelasticMerger(CelestialBodyState b1, CelestialBodyState b2, CelestialBodyState merged, double tolerance = 1e-6)
        {
            Vector3d pInitial = b1.Velocity * b1.Mass + b2.Velocity * b2.Mass;
            Vector3d pFinal = merged.Velocity * merged.Mass;
            AssertLinearMomentumConserved(pInitial, pFinal, tolerance, "Inelastic merger momentum conservation");
        }

        public static void AssertTimeReversibility(Vector3d initialPos, Vector3d reversedPos, double tolerance = 1e-4, string context = null)
        {
            double diff = (reversedPos - initialPos).magnitude;
            Assert.LessOrEqual(diff, tolerance,
                $"[Symplectic Time-Reversibility Invariant Failed] Initial: {initialPos}, Reversed: {reversedPos}, Deviation: {diff:E6} exceeds tolerance {tolerance:E6}. {context}");
        }
    }
}
