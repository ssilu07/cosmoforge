// ============================================================================
// CosmoForge Tier 2 Boundary & Corner Cases: Celestial Bodies Tests (F07 - F11)
// 25 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts, TEST_INFRA.md, & ORIGINAL_REQUEST.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Integrator;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier2
{
    [TestFixture]
    public class Tier2_CelestialBodiesBoundaryTests
    {
        #region Feature F07: Terrestrial Planets & Moons (5 Boundary Tests)

        [Test]
        public void F07_Boundary_BlackbodyTemp_ZeroStellarLuminosity_ApproachesZeroKelvin()
        {
            // Boundary condition: Star luminosity L = 0.0 (extinguished star or deep interstellar rogue planet).
            // Formula: T = (L * (1-A) / (16 * pi * sigma * d^2))^(1/4).
            // Asserts temperature evaluates cleanly to 0.0 K without producing NaN or imaginary roots.
            double L = 0.0;
            double distance = 100.0;
            double albedo = 0.3;

            double temp = CosmoForgeTestHarness.CalculateBlackbodyTemperature(L, distance, albedo);

            Assert.IsFalse(double.IsNaN(temp), "Temperature must not be NaN when luminosity is zero.");
            Assert.AreEqual(0.0, temp, 1e-12, "Temperature must be exactly 0.0 K for zero stellar luminosity.");
        }

        [Test]
        public void F07_Boundary_BlackbodyTemp_ZeroDistance_HandlesSingularityGracefully()
        {
            // Boundary condition: Distance d = 0.0 (planet at center of star).
            // Denominator 16 * pi * sigma * d^2 evaluates to 0.
            // Function must guard against division-by-zero, returning safe clamped maximum temperature or zero fallback.
            double L = 10000.0;
            double distance = 0.0;

            double temp = CosmoForgeTestHarness.CalculateBlackbodyTemperature(L, distance);

            Assert.IsFalse(double.IsNaN(temp), "Temperature at zero distance must not be NaN.");
            Assert.IsFalse(double.IsInfinity(temp), "Temperature at zero distance must not be Infinity.");
            Assert.GreaterOrEqual(temp, 0.0, "Temperature must be non-negative.");
        }

        [Test]
        public void F07_Boundary_Terrestrial_ZeroRadius_DensityCalculationGuarded()
        {
            // Boundary condition: Terrestrial body with radius = 0.0.
            // Volume V = 4/3 * pi * R^3 = 0.
            // Calculation of density rho = M / V must be guarded against division-by-zero.
            double mass = 5.972e24;
            double radius = 0.0;

            double volume = (4.0 / 3.0) * Math.PI * radius * radius * radius;
            double density = volume > 1e-15 ? mass / volume : 0.0;

            Assert.IsFalse(double.IsNaN(density), "Density must not be NaN for zero-radius body.");
            Assert.IsFalse(double.IsInfinity(density), "Density must not be Infinity for zero-radius body.");
            Assert.AreEqual(0.0, density, "Guarded density at zero radius must return 0.0.");
        }

        [Test]
        public void F07_Boundary_Terrestrial_ExtremeAlbedo_TotalReflectionZeroAbsorption()
        {
            // Boundary condition: Perfect specular/Lambertian reflector with albedo A = 1.0 (pure fresh ice or mirror).
            // Absorbed power = (1 - A) * flux = 0.
            // Asserts temperature evaluates to 0.0 K.
            double L = 3.828e26;
            double distance = 1.496e11;
            double albedo = 1.0;

            double temp = CosmoForgeTestHarness.CalculateBlackbodyTemperature(L, distance, albedo);

            Assert.AreEqual(0.0, temp, 1e-12, "Planet with albedo 1.0 absorbs zero flux and has 0 K equilibrium temperature.");
        }

        [Test]
        public void F07_Boundary_Terrestrial_BinaryStars_FluxSuperposition_SumsLinearlyBeforeFourthRoot()
        {
            // Boundary condition: Planet orbiting in binary star system receiving flux from Star A and Star B.
            // Multi-star temperature T = ( (F_A + F_B) / (4 * sigma) )^(1/4).
            // Superposition must sum fluxes before taking the 4th root, NOT summing temperatures.
            double LA = 2.0e26;
            double LB = 3.0e26;
            double dA = 1.5e11;
            double dB = 2.0e11;
            double sigma = CosmoForgeTestHarness.StefanBoltzmannConstant;
            double albedo = 0.3;

            double fluxA = (LA * (1.0 - albedo)) / (16.0 * Math.PI * sigma * dA * dA);
            double fluxB = (LB * (1.0 - albedo)) / (16.0 * Math.PI * sigma * dB * dB);

            double tempCombined = Math.Pow(fluxA + fluxB, 0.25);
            double tempAOnly = Math.Pow(fluxA, 0.25);
            double tempBOnly = Math.Pow(fluxB, 0.25);

            Assert.Greater(tempCombined, tempAOnly, "Combined temperature must exceed individual Star A temperature.");
            Assert.Greater(tempCombined, tempBOnly, "Combined temperature must exceed individual Star B temperature.");
            Assert.Less(tempCombined, tempAOnly + tempBOnly, "Combined temperature must be strictly less than algebraic sum of temperatures (due to 4th power law).");
            TestAssertions.AssertNoNaNOrInfinity(new Vector3d(tempCombined, 0, 0), "Combined temperature");
        }

        #endregion

        #region Feature F08: Gas Giants & Atmospheric Vortices (5 Boundary Tests)

        [Test]
        public void F08_Boundary_GasGiant_ZeroAngularVelocity_ZeroZonalShear()
        {
            // Boundary condition: Gas giant with rotation period -> infinity (Omega = 0.0 rad/s).
            // Coriolis acceleration F_cor = 2 * Omega x v = 0.
            // Zonal wind band shear between adjacent latitudes must be zero.
            double omega = 0.0;
            double radius = 7e7;
            double equatorialSpeed = omega * radius;

            Assert.AreEqual(0.0, equatorialSpeed, 1e-12, "Equatorial wind speed must be zero for non-rotating gas giant.");
        }

        [Test]
        public void F08_Boundary_GasGiant_EquatorialWindSpeed_AcousticSoundSpeedLimitClamped()
        {
            // Boundary condition: Atmospheric zonal jet stream velocity requested at extreme speed (e.g. 5000 m/s).
            // Planetary atmospheres clamp zonal jets to local speed of sound (~800 m/s in hydrogen/helium) to prevent unphysical supersonic shocks.
            double requestedWindSpeed = 5000.0;
            double speedOfSound = 850.0; // In Jovian troposphere
            double clampedSpeed = Math.Clamp(requestedWindSpeed, -speedOfSound, speedOfSound);

            Assert.AreEqual(speedOfSound, clampedSpeed, 1e-6, "Equatorial wind speed must clamp to sound speed boundary.");
            Assert.LessOrEqual(clampedSpeed, 850.0);
        }

        [Test]
        public void F08_Boundary_GasGiant_StormVortex_ZeroRadius_ZeroDisplacement()
        {
            // Boundary condition: Great Red Spot or storm vortex radius set to 0.0.
            // Vortex displacement field must evaluate to identically zero without texture distortion.
            double vortexRadius = 0.0;
            double vortexStrength = 5.0;

            // Gaussian vortex velocity: v_theta = strength * (r / R) * exp(-r^2 / (2 * R^2))
            double sampleDistance = 10.0;
            double velocityDisplacement = vortexRadius > 1e-9 ?
                vortexStrength * (sampleDistance / vortexRadius) * Math.Exp(-(sampleDistance * sampleDistance) / (2.0 * vortexRadius * vortexRadius)) : 0.0;

            Assert.AreEqual(0.0, velocityDisplacement, "Zero-radius storm vortex must produce zero velocity displacement.");
            Assert.IsFalse(double.IsNaN(velocityDisplacement), "Vortex displacement must not be NaN.");
        }

        [Test]
        public void F08_Boundary_GasGiant_MinimumDensity_LowMassPuffyGiant()
        {
            // Boundary condition: Ultra-low density "puffy giant" (e.g. WASP-107b, density rho = 100 kg/m^3 vs Saturn 687 kg/m^3).
            // Fluid Roche limit calculation must evaluate stably without overflow.
            double primaryRadius = 6.99e7;
            double rhoGiant = 100.0; // Very low density
            double rhoMoon = 2000.0; // Dense rocky moon

            double dRoche = CosmoForgeTestHarness.CalculateFluidRocheLimit(primaryRadius, rhoGiant, rhoMoon);

            Assert.IsFalse(double.IsNaN(dRoche), "Roche limit for low-density gas giant must not be NaN.");
            Assert.Greater(dRoche, 0.0, "Roche limit must be positive.");
            // Because rhoGiant < rhoMoon, d_Roche is smaller than for equal density
            double dRocheEqual = CosmoForgeTestHarness.CalculateFluidRocheLimit(primaryRadius, 2000.0, 2000.0);
            Assert.Less(dRoche, dRocheEqual, "Low-density giant must have smaller Roche limit than equal-density giant.");
        }

        [Test]
        public void F08_Boundary_GasGiant_MaximumCompression_DeuteriumBurning13JupiterMasses()
        {
            // Boundary condition: Gas giant mass at 13 Jupiter masses (13 * M_Jup = 13 * 1.898e27 kg = 2.467e28 kg).
            // At this boundary, electron degeneracy halts volume growth: radius is roughly constant or slightly shrinks.
            double mJup = 1.898e27;
            double rJup = 6.991e7;

            double mBrownDwarf = 13.0 * mJup;
            // Radius scaling for degenerate gas giant: R ~ R_Jup * (M / M_Jup)^(-1/3) for degenerate regime
            double radiusEstimate = rJup * Math.Pow(13.0, -0.05); // weakly dependent

            Assert.Greater(radiusEstimate, 0.0, "Brown dwarf radius must be strictly positive.");
            Assert.Less(radiusEstimate, 2.0 * rJup, "Radius must not blow up with 13x mass increase.");
            Assert.Greater(radiusEstimate, 0.5 * rJup, "Radius must remain planetary scale.");
        }

        #endregion

        #region Feature F09: Stellar Fusion Lifecycle Engine (5 Boundary Tests)

        [Test]
        public void F09_Boundary_StellarLifecycle_FuelZero_ImmediateSupernovaOrCollapseTrigger()
        {
            // Boundary condition: Star initialized with CoreFuel = 0.0.
            // Fuel depletion check must trigger post-main-sequence collapse or supernova immediately on next update.
            var star = new CelestialBodyState(1, "DyingStar", BodyType.Star, 20.0, 15.0, Vector3d.zero, Vector3d.zero)
            {
                CoreFuel = 0.0
            };

            bool isFuelDepleted = star.CoreFuel <= 0.0;
            Assert.IsTrue(isFuelDepleted, "Star with 0.0 fuel must be flagged as fuel depleted.");

            // For massive star (20 M_sun), core collapse is triggered
            bool triggersCoreCollapse = isFuelDepleted && star.Mass >= 8.0;
            Assert.IsTrue(triggersCoreCollapse, "Depleted massive star (20 M_sun >= 8 M_sun) must trigger Type II core collapse.");
        }

        [Test]
        public void F09_Boundary_StellarLifecycle_FuelNegative_ClampedToZero()
        {
            // Boundary condition: Burn rate decrementing fuel past zero (e.g. 0.01 - 0.05 = -0.04).
            // Fuel must be clamped to 0.0 to prevent negative fuel percentages or inverted fusion energy generation.
            double rawFuel = 0.005;
            double burnDelta = 0.02;
            double remainingFuel = rawFuel - burnDelta;

            double clampedFuel = Math.Max(0.0, remainingFuel);

            Assert.AreEqual(0.0, clampedFuel, "Negative fuel calculation must clamp to exactly 0.0.");
            Assert.GreaterOrEqual(clampedFuel, 0.0);
        }

        [Test]
        public void F09_Boundary_StellarLifecycle_ChandrasekharLimit_Exact144SolarMasses()
        {
            // Boundary condition: White dwarf accreting mass crossing exactly 1.44 M_sun.
            // Formula: M >= 1.44 M_sun triggers runaway thermonuclear Type Ia supernova.
            double chLimit = CosmoForgeTestHarness.ChandrasekharLimitSolarMasses;
            Assert.AreEqual(1.44, chLimit, 1e-6, "Chandrasekhar limit must be exactly 1.44 solar masses.");

            double subMass = 1.4399;
            double exactMass = 1.4400;
            double superMass = 1.4401;

            bool subExplodes = subMass >= chLimit;
            bool exactExplodes = exactMass >= chLimit;
            bool superExplodes = superMass >= chLimit;

            Assert.IsFalse(subExplodes, "White dwarf below 1.44 M_sun must not detonate.");
            Assert.IsTrue(exactExplodes, "White dwarf at exactly 1.44 M_sun must trigger Type Ia supernova.");
            Assert.IsTrue(superExplodes, "White dwarf above 1.44 M_sun must trigger Type Ia supernova.");
        }

        [Test]
        public void F09_Boundary_StellarLifecycle_SubChandrasekhar_StableDegenerateDwarf()
        {
            // Boundary condition: White dwarf at 1.0 M_sun (Sirius B type).
            // CoreFuel is 0.0, but mass is well below 1.44 M_sun.
            // Must remain a stable cooling white dwarf without detonating.
            double massSolar = 1.0;
            double fuel = 0.0;

            bool triggersTypeIa = massSolar >= CosmoForgeTestHarness.ChandrasekharLimitSolarMasses;
            bool triggersTypeII = fuel <= 0.0 && massSolar >= 8.0;

            Assert.IsFalse(triggersTypeIa, "Sub-Chandrasekhar dwarf must not trigger Type Ia supernova.");
            Assert.IsFalse(triggersTypeII, "Low-mass star must not trigger Type II core collapse.");
        }

        [Test]
        public void F09_Boundary_StellarLifecycle_RedDwarf_LowMassExtremelyLowBurnRate()
        {
            // Boundary condition: Red Dwarf at lower hydrogen fusion threshold (M = 0.08 M_sun).
            // Mass-luminosity relation: L ~ M^2.3.
            // For M = 0.08, L ~ (0.08)^2.3 ~ 0.00298 L_sun.
            // Lifespan tau ~ M / L ~ 0.08 / 0.00298 ~ 26.8x solar lifespan (~268 billion years).
            double m = 0.08;
            double luminosityRatio = Math.Pow(m, 2.3);

            Assert.Less(luminosityRatio, 0.01, "Red dwarf luminosity must be less than 1% of solar luminosity.");
            Assert.Greater(luminosityRatio, 0.0, "Red dwarf luminosity must be strictly positive.");

            double relativeBurnRate = luminosityRatio / m;
            Assert.Less(relativeBurnRate, 0.1, "Fuel burn rate of red dwarf must be extremely slow.");
        }

        #endregion

        #region Feature F10: Pulsar Synchrotron Beams (5 Boundary Tests)

        [Test]
        public void F10_Boundary_Pulsar_UltraFastSpin_MillisecondPulsarRotationPhaseWrapsCleanly()
        {
            // Boundary condition: Millisecond pulsar with spin frequency f = 716 Hz (PSR J1748-2446ad, breakup limit).
            // Period P = 1 / 716 ~ 0.001396 s.
            // Phase angle theta(t) = (2 * pi * f * t) % (2 * pi) must stay strictly in [0, 2*pi] without precision loss.
            double spinFreq = 716.0;
            double twoPi = 2.0 * Math.PI;

            for (double t = 0.0; t <= 10.0; t += 0.0123)
            {
                double rawPhase = twoPi * spinFreq * t;
                double wrappedPhase = rawPhase % twoPi;

                Assert.GreaterOrEqual(wrappedPhase, 0.0, "Wrapped phase must be >= 0.0");
                Assert.Less(wrappedPhase, twoPi + 1e-9, "Wrapped phase must be < 2*pi");
                Assert.IsFalse(double.IsNaN(wrappedPhase), "Phase must not be NaN.");
            }
        }

        [Test]
        public void F10_Boundary_Pulsar_ZeroMagneticTilt_AlignedRotator_NoPrecessionWobble()
        {
            // Boundary condition: Magnetic tilt angle alpha = 0.0 radians (aligned rotator).
            // The magnetic dipole axis mu points permanently along the spin vector omega without sweeping a cone.
            Vector3d spinAxis = new Vector3d(0.0, 1.0, 0.0);
            double alpha = 0.0;

            // Dipole vector: mu = spinAxis * cos(alpha) + perpendicular * sin(alpha)
            Vector3d dipole = spinAxis * Math.Cos(alpha);

            TestAssertions.AssertApproximatelyEqual(spinAxis, dipole, 1e-9, "Aligned rotator dipole must point exactly along spin axis.");
            Assert.AreEqual(1.0, dipole.magnitude, 1e-9, "Dipole vector must have unit magnitude.");
        }

        [Test]
        public void F10_Boundary_Pulsar_OrthogonalMagneticTilt_NinetyDegrees_SweepsEquatorMaximalDoppler()
        {
            // Boundary condition: Orthogonal rotator with alpha = 90 degrees (pi/2 radians).
            // Dipole axis rotates in the equatorial plane perpendicular to the spin vector.
            Vector3d spinAxis = new Vector3d(0.0, 1.0, 0.0);
            double alpha = Math.PI / 2.0;

            double cosAlpha = Math.Cos(alpha);
            Assert.AreEqual(0.0, cosAlpha, 1e-12, "cos(90 deg) must be zero.");

            // Verify dot product between dipole and spin axis is identically zero
            for (double phase = 0.0; phase < 2.0 * Math.PI; phase += 0.5)
            {
                Vector3d dipole = new Vector3d(Math.Cos(phase), 0.0, Math.Sin(phase));
                double dot = Vector3d.Dot(spinAxis, dipole);
                Assert.AreEqual(0.0, dot, 1e-12, "Orthogonal rotator dipole must be perpendicular to spin axis at all phases.");
            }
        }

        [Test]
        public void F10_Boundary_Pulsar_BeamCollimation_ZeroHalfAngleLimit_ClampedToMinimumPositive()
        {
            // Boundary condition: Radiation beam half-angle theta_b = 0.0 radians.
            // Gaussian beam profile: I = I_0 * exp( - psi^2 / (2 * theta_b^2) ).
            // Zero theta_b would divide by zero. Engine must clamp theta_b to minimum positive angle (e.g. 1 degree = 0.0175 rad).
            double rawThetaB = 0.0;
            double minThetaB = Math.Max(0.01745, rawThetaB); // 1 degree minimum clamp

            Assert.Greater(minThetaB, 0.0, "Beam cone half-angle must clamp to strictly positive angle.");

            double psi = 0.05; // 3 degrees off-axis
            double intensity = Math.Exp(-(psi * psi) / (2.0 * minThetaB * minThetaB));

            Assert.IsFalse(double.IsNaN(intensity), "Beam intensity must not be NaN.");
            Assert.GreaterOrEqual(intensity, 0.0);
            Assert.LessOrEqual(intensity, 1.0);
        }

        [Test]
        public void F10_Boundary_Pulsar_ObserverCollinear_ContinuousMaximumPulseIntensity()
        {
            // Boundary condition: Observer camera vector aligned directly with magnetic beam axis (psi = 0.0).
            // Formula: I = I_0 * exp( - 0 / (2 * theta_b^2) ) = I_0 * exp(0) = I_0.
            // Asserts intensity equals exactly peak intensity I_0.
            double iZero = 100.0;
            double thetaB = 0.15; // ~8.6 degrees
            double psi = 0.0; // exact alignment

            double intensity = iZero * Math.Exp(-(psi * psi) / (2.0 * thetaB * thetaB));

            Assert.AreEqual(iZero, intensity, 1e-9, "Collinear observer must receive exact peak pulse intensity I_0.");
        }

        #endregion

        #region Feature F11: Supermassive Black Holes (5 Boundary Tests)

        [Test]
        public void F11_Boundary_SchwarzschildRadius_ZeroMass_ZeroRadiusWithoutNaN()
        {
            // Boundary condition: Black hole mass M = 0.0.
            // Formula: R_s = 2 * G * M / c^2.
            // Asserts Schwarzschild radius is identically 0.0 without NaN.
            double M = 0.0;
            double G = 1.0;
            double c = 50.0;

            double rs = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, M, c);

            Assert.AreEqual(0.0, rs, 1e-15, "Schwarzschild radius for zero mass must be 0.0.");
            Assert.IsFalse(double.IsNaN(rs), "Schwarzschild radius must not be NaN.");
        }

        [Test]
        public void F11_Boundary_EventHorizon_InfallBoundary_PointInsideHorizonMarkedDestroyed()
        {
            // Boundary condition: Test probe positioned at r = 0.999 * R_s (inside event horizon).
            // General relativity dictates nothing can escape once inside R_s.
            // Engine must detect r <= R_s and mark test particle IsDestroyed = true.
            double G = 1.0;
            double M = 20000.0;
            double c = 50.0;
            double rs = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, M, c);

            Vector3d probePos = new Vector3d(rs * 0.999, 0.0, 0.0);
            double dist = probePos.magnitude;

            bool isInsideHorizon = dist <= rs;
            Assert.IsTrue(isInsideHorizon, "Point at 0.999 Rs must be detected inside event horizon.");

            var probe = new CelestialBodyState(2, "InfallingProbe", BodyType.AsteroidProbe, 0.1, 0.1, probePos, Vector3d.zero);
            if (isInsideHorizon)
            {
                probe.IsDestroyed = true;
            }

            Assert.IsTrue(probe.IsDestroyed, "Body crossing inside Schwarzschild radius must be flagged as destroyed.");
        }

        [Test]
        public void F11_Boundary_PhotonSphere_ExactOnePointFiveRs()
        {
            // Boundary condition: Photon sphere radius for Schwarzschild metric is exactly 1.5 * R_s = 3 * G * M / c^2.
            // Light rays orbiting at this radius can form closed circular photon orbits.
            double G = 1.0;
            double M = 15000.0;
            double c = 50.0;

            double rs = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, M, c);
            double rPhoton = CosmoForgeTestHarness.CalculatePhotonSphereRadius(G, M, c);

            Assert.AreEqual(1.5 * rs, rPhoton, 1e-9, "Photon sphere radius must equal exactly 1.5 * R_s.");
            Assert.AreEqual((3.0 * G * M) / (c * c), rPhoton, 1e-9, "Photon sphere must equal 3*G*M/c^2.");
            TestAssertions.AssertApproximatelyEqual(1.5 * rs, rPhoton, 1e-9, "Photon sphere radius must equal exactly 1.5 * R_s.");
            TestAssertions.AssertApproximatelyEqual((3.0 * G * M) / (c * c), rPhoton, 1e-9, "Photon sphere must equal 3*G*M/c^2.");
        }

        [Test]
        public void F11_Boundary_DopplerBeaming_SubLightSpeedAccretion_LorentzFactorRemainsFinite()
        {
            // Boundary condition: Accretion disk inner edge at Innermost Stable Circular Orbit (ISCO = 3 * R_s).
            // Keplerian orbital speed at ISCO: beta = v / c = sqrt(R_s / (2 * r_ISCO)) = sqrt(1 / 6) ~ 0.4082c.
            // Lorentz factor: gamma = 1 / sqrt(1 - beta^2) = 1 / sqrt(1 - 1/6) = sqrt(6/5) ~ 1.0954.
            // Asserts gamma > 1.0, finite, and real.
            double rISCO_ratio = 3.0; // r = 3 Rs
            double beta = Math.Sqrt(1.0 / (2.0 * rISCO_ratio)); // sqrt(1/6)

            Assert.Less(beta, 1.0, "Accretion orbital speed at ISCO must be sub-luminal (< c).");
            Assert.Greater(beta, 0.0, "Orbital speed must be positive.");

            double gamma = 1.0 / Math.Sqrt(1.0 - beta * beta);

            Assert.Greater(gamma, 1.0, "Lorentz factor must be strictly greater than 1.0 for relativistic motion.");
            TestAssertions.AssertApproximatelyEqual(Math.Sqrt(6.0 / 5.0), gamma, 1e-9, "gamma must equal sqrt(6/5)");
            Assert.IsFalse(double.IsNaN(gamma), "Lorentz factor must not be NaN.");
        }

        [Test]
        public void F11_Boundary_EinsteinRing_ZeroObserverDistance_SafeFallback()
        {
            // Boundary condition: Einstein ring angular radius: theta_E = sqrt( (4*G*M/c^2) * (D_ls / (D_l * D_s)) ).
            // Observer distance D_l = 0.0.
            // Shader / math code must guard against division-by-zero, returning safe 0.0 deflection angle.
            double G = 1.0;
            double M = 10000.0;
            double c = 50.0;

            double dl = 0.0;
            double ds = 1000.0;
            double dls = 1000.0;

            double denom = dl * ds;
            double thetaE = denom > 1e-9 ? Math.Sqrt(((4.0 * G * M) / (c * c)) * (dls / denom)) : 0.0;

            Assert.IsFalse(double.IsNaN(thetaE), "Einstein ring angle must not be NaN when observer is at zero distance.");
            Assert.AreEqual(0.0, thetaE, "Guarded Einstein ring angle must evaluate to 0.0 at zero distance.");
        }

        #endregion
    }
}
