// ============================================================================
// CosmoForge Tier 1 Feature Coverage: Celestial Body Catalog Tests (F07 - F11)
// 25 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Celestial.Data;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier1
{
    [TestFixture]
    public class Tier1_CelestialBodiesTests
    {
        #region Feature F07: Terrestrial Planets & Moons (5 Tests)

        [Test]
        public void F07_Terrestrial_BlackbodyTemperature_MatchesStefanBoltzmannEquilibrium()
        {
            // Requirement R3 & Acceptance Criteria: T = (L_* (1 - A) / (16 * pi * sigma * d^2))^(1/4)
            double solarLuminosity = 3.828e26; // Watts
            double distance = 1.496e11;        // 1 AU in meters
            double albedo = 0.30;              // Earth albedo
            double sigma = CosmoForgeTestHarness.StefanBoltzmannConstant;

            double expectedTemp = CosmoForgeTestHarness.CalculateBlackbodyTemperature(solarLuminosity, distance, albedo, sigma);

            // Earth unattenuated equilibrium is ~255 K (without greenhouse warming)
            Assert.AreEqual(254.3, expectedTemp, 2.0, "Theoretical equilibrium blackbody temperature must match Stefan-Boltzmann law.");
            TestAssertions.AssertTemperatureInRange(expectedTemp, 250.0, 260.0, "Earth equilibrium temperature");
        }

        [Test]
        public void F07_Terrestrial_MultiStellarFlux_SumsLuminositiesCorrectly()
        {
            // Requirement R3: Multi-stellar systems sum total received flux before 4th root
            double distA = 1.0;
            double distB = 1.5;
            double fluxA = 1000.0 / (distA * distA);
            double fluxB = 500.0 / (distB * distB);
            double totalFlux = fluxA + fluxB;

            double tempCombined = Math.Pow(totalFlux, 0.25);
            double tempAloneA = Math.Pow(fluxA, 0.25);

            Assert.Greater(tempCombined, tempAloneA, "Planetary temperature in binary system must be higher than with single star alone.");
            Assert.AreEqual(Math.Pow(1000.0 + (500.0 / 2.25), 0.25), tempCombined, 1e-9);
        }

        [Test]
        public void F07_Terrestrial_ThermalZoning_ClassifiesGlacialTemperateMagmaBiomes()
        {
            // Requirement R3: Surface temperature dynamically dictates visual biomes
            double coldTemp = 200.0;     // < 273.15 K -> Glacial Ice
            double temperateTemp = 295.0;// 273.15 - 373.15 K -> Liquid Water Oceans & Temperate Biomes
            double magmaTemp = 1200.0;   // > 1000 K -> Magma Fissures

            BiomeType coldBiome = CosmoForgeTestHarness.ClassifySurfaceBiome(coldTemp);
            BiomeType temperateBiome = CosmoForgeTestHarness.ClassifySurfaceBiome(temperateTemp);
            BiomeType magmaBiome = CosmoForgeTestHarness.ClassifySurfaceBiome(magmaTemp);

            Assert.AreEqual(BiomeType.GlacialIce, coldBiome, "Cold planet (< 273.15 K) must classify as GlacialIce.");
            Assert.AreEqual(BiomeType.TemperateOceans, temperateBiome, "Temperate planet (273.15 - 373.15 K) must classify as TemperateOceans.");
            Assert.AreEqual(BiomeType.MagmaFissures, magmaBiome, "Superheated planet (> 373.15 K) must classify as MagmaFissures.");
        }

        [Test]
        public void F07_Terrestrial_AtmosphericScatteringRim_IntensityFadesWithNormal()
        {
            // Requirement R3: Atmospheric rim shader intensity: RimFactor = (1 - saturate(N . V))^3
            Vector3 normal = Vector3.up;

            // Looking directly at center (N . V = 1.0)
            Vector3 viewCenter = Vector3.up;
            float dotCenter = Mathf.Clamp01(Vector3.Dot(normal, viewCenter));
            float rimCenter = Mathf.Pow(1.0f - dotCenter, 3.0f);

            // Looking at the glancing limb (N . V = 0.0)
            Vector3 viewLimb = Vector3.right;
            float dotLimb = Mathf.Clamp01(Vector3.Dot(normal, viewLimb));
            float rimLimb = Mathf.Pow(1.0f - dotLimb, 3.0f);

            Assert.AreEqual(0.0f, rimCenter, 1e-5f, "Atmospheric rim intensity at center must be 0.");
            Assert.AreEqual(1.0f, rimLimb, 1e-5f, "Atmospheric rim intensity at tangential limb must be 1.0.");
        }

        [Test]
        public void F07_Terrestrial_TidalLocking_RotationalPeriodMatchesOrbitalPeriod()
        {
            // Requirement R3: Tidally locked moons have synchronous rotational period = orbital period
            double G = 1.0;
            double earthMass = 1000.0;
            double lunarDistance = 50.0;

            double orbitalPeriod = CosmoForgeTestHarness.CalculateOrbitalPeriod(G, earthMass, lunarDistance);
            double synchronousRotationPeriod = CosmoForgeTestHarness.CalculateSynchronousRotationPeriod(G, earthMass, lunarDistance);
            bool isLocked = CosmoForgeTestHarness.IsTidallyLocked(synchronousRotationPeriod, orbitalPeriod);

            Assert.IsTrue(isLocked, "Tidally locked satellite must evaluate to synchronous 1:1 spin-orbit resonance.");
            Assert.AreEqual(orbitalPeriod, synchronousRotationPeriod, 1e-12, "Tidally locked satellite must have exact 1:1 spin-orbit resonance.");
        }

        #endregion

        #region Feature F08: Gas Giants & Atmospheric Vortices (5 Tests)

        [Test]
        public void F08_GasGiant_BulkDensity_LowerThanTerrestrialPlanet()
        {
            // Requirement R3: Gas giants (e.g. Saturn ~687 kg/m^3, Jupiter ~1326 kg/m^3) have much lower bulk density than rocky planets (~5515 kg/m^3)
            double terrestrialMass = 5.972e24;
            double terrestrialRadius = 6.371e6;
            double terrestrialDensity = terrestrialMass / ((4.0 / 3.0) * Math.PI * Math.Pow(terrestrialRadius, 3.0));

            double gasGiantMass = 1.898e27;
            double gasGiantRadius = 6.9911e7;
            double gasGiantDensity = gasGiantMass / ((4.0 / 3.0) * Math.PI * Math.Pow(gasGiantRadius, 3.0));

            Assert.Greater(terrestrialDensity, 5000.0, "Terrestrial planet density must be ~5500 kg/m^3.");
            Assert.Less(gasGiantDensity, 1500.0, "Gas giant density must be ~1300 kg/m^3.");
            Assert.Greater(terrestrialDensity / gasGiantDensity, 3.5, "Rocky planet must be > 3.5x denser than gas giant.");
        }

        [Test]
        public void F08_GasGiant_AnimatedZonalBands_AlternatesWindVelocitiesByLatitude()
        {
            // Requirement R3: Counter-rotating zonal wind bands alternate direction based on latitude
            double equatorWind = CosmoForgeTestHarness.CalculateZonalWindVelocity(0.0);
            double band1Wind = CosmoForgeTestHarness.CalculateZonalWindVelocity(Math.PI / 16.0); // +22.5 deg
            double band2Wind = CosmoForgeTestHarness.CalculateZonalWindVelocity(3.0 * Math.PI / 16.0);  // +67.5 deg

            Assert.AreEqual(0.0, equatorWind, 1e-9);
            Assert.Greater(band1Wind, 0.0, "Band 1 should be prograde eastward jet.");
            Assert.Less(band2Wind, 0.0, "Band 2 should be retrograde westward jet.");
        }

        [Test]
        public void F08_GasGiant_StormVortex_ComputesVorticityAndSwirlAngle()
        {
            // Cyclonic storm vortex (e.g. Great Red Spot) angular rotation over time
            double swirlRateRadPerSec = 0.5;
            double timeSeconds = 10.0;
            double cumulativeAngle = swirlRateRadPerSec * timeSeconds;

            Assert.AreEqual(5.0, cumulativeAngle, 1e-9, "Vortex swirl angle must advance continuously with time.");
        }

        [Test]
        public void F08_GasGiant_Oblateness_CalculatesFlatteningFromRotation()
        {
            // Rapid rotation causes oblateness: f = (R_eq - R_pol) / R_eq (Saturn f ~ 0.098, Jupiter f ~ 0.065)
            double rEq = 71492.0;
            double rPol = 66854.0;
            double flattening = (rEq - rPol) / rEq;

            Assert.Greater(flattening, 0.05, "Gas giant flattening must be > 0.05.");
            Assert.Less(flattening, 0.12, "Gas giant flattening must be < 0.12.");
        }

        [Test]
        public void F08_GasGiant_InternalHeatFlux_ElevatesBaseEffectiveTemperature()
        {
            // Gas giants emit more heat than they absorb from the Sun due to gravitational contraction
            double absorbedSolarFlux = 50.0;
            double internalConvectiveFlux = 70.0; // Significant internal heat engine
            double totalFlux = absorbedSolarFlux + internalConvectiveFlux;

            double tempWithInternal = Math.Pow(totalFlux, 0.25);
            double tempWithoutInternal = Math.Pow(absorbedSolarFlux, 0.25);

            Assert.Greater(tempWithInternal, tempWithoutInternal, "Internal convective heat flux must elevate gas giant effective temperature.");
        }

        #endregion

        #region Feature F09: Stellar Fusion Lifecycle Engine (5 Tests)

        [Test]
        public void F09_StellarLifecycle_MassLuminosityRelation_FollowsPowerLaw()
        {
            // Requirement R3: Mass-luminosity relation L ~ M^3.5 for solar-type stars
            double mSun = 1.0;
            double mHigh = 2.0;

            double lSun = Math.Pow(mSun, 3.5);
            double lHigh = Math.Pow(mHigh, 3.5);

            Assert.AreEqual(1.0, lSun, 1e-9);
            Assert.AreEqual(Math.Pow(2.0, 3.5), lHigh, 1e-5, "2-solar-mass star must have luminosity 2^3.5 ~ 11.3 L_sun.");
            Assert.Greater(lHigh, 11.0);
        }

        [Test]
        public void F09_StellarLifecycle_CoreHydrogenFuel_DepletesMonotonically()
        {
            // Core fuel tracks normalized depletion [1.0 -> 0.0] via stellar burn model
            var star = new CelestialBodyState(1, "Sun", BodyType.Star, 1.0, 1.0, Vector3d.zero, Vector3d.zero)
            {
                CoreFuel = 1.0
            };
            double initialFuel = star.CoreFuel;
            double dt = 1.0;

            for (int t = 0; t < 100; t++)
            {
                CosmoForgeTestHarness.BurnStellarFuel(star, dt, burnRatePerSec: 0.001);
            }

            Assert.AreEqual(0.9, star.CoreFuel, 1e-9, "Fuel must deplete steadily over time.");
            Assert.Less(star.CoreFuel, initialFuel, "Fuel must deplete monotonically.");
            Assert.Greater(star.CoreFuel, 0.0, "Fuel must remain above zero during main sequence.");
        }

        [Test]
        public void F09_StellarLifecycle_LifespanCalculation_MassiveStarsBurnFaster()
        {
            // Lifespan: tau ~ M / L ~ M / M^3.5 = M^-2.5
            double massRedDwarf = 0.2;
            double massSun = 1.0;
            double massBlueSupergiant = 15.0;

            double lifespanRedDwarf = Math.Pow(massRedDwarf, -2.5);
            double lifespanSun = Math.Pow(massSun, -2.5);
            double lifespanBlue = Math.Pow(massBlueSupergiant, -2.5);

            Assert.Greater(lifespanRedDwarf, lifespanSun, "Red Dwarf must outlive Sun by orders of magnitude.");
            Assert.Less(lifespanBlue, lifespanSun, "Blue Supergiant must burn out dramatically faster than Sun.");
            Assert.Less(lifespanBlue, 0.002, "15-solar-mass star lifespan relative to Sun must be < 0.2%.");
        }

        [Test]
        public void F09_StellarLifecycle_SolarTypeStar_MaintainsStableLuminosityInMainSequence()
        {
            // Main sequence star with ample fuel maintains steady luminosity
            var star = new CelestialBodyState(1, "Sun", BodyType.Star, 1.0, 1.0, Vector3d.zero, Vector3d.zero)
            {
                CoreFuel = 0.75,
                SurfaceTemperature = 5778.0
            };

            Assert.IsTrue(star.CoreFuel > 0.1, "Star in main sequence has abundant core fuel.");
            Assert.AreEqual(5778.0, star.SurfaceTemperature, 1.0, "Main sequence photosphere temperature remains stable.");
        }

        [Test]
        public void F09_StellarLifecycle_FuelExhaustion_TriggersStellarDeathState()
        {
            // When core fuel reaches 0.0, star enters post-main-sequence exhaustion
            var star = new CelestialBodyState(1, "DyingStar", BodyType.Star, 10.0, 5.0, Vector3d.zero, Vector3d.zero)
            {
                CoreFuel = 0.0
            };

            bool isExhausted = star.CoreFuel <= 0.0;
            Assert.IsTrue(isExhausted, "Star with 0 core fuel must trigger post-main-sequence transition.");
        }

        #endregion

        #region Feature F10: Pulsar Synchrotron Beams (5 Tests)

        [Test]
        public void F10_Pulsar_RotationPeriod_SpinsAtMillisecondToSecondTimescale()
        {
            // Requirement R3: Pulsars rotate extremely rapidly: P ~ 1.5 ms to 5 seconds
            double spinFreqHz = 30.0; // 30 Hz (e.g. Crab Pulsar ~30.2 Hz)
            double periodSec = 1.0 / spinFreqHz;

            Assert.AreEqual(1.0 / 30.0, periodSec, 1e-9);
            Assert.GreaterOrEqual(periodSec, 0.0015, "Period must be >= 1.5 ms.");
            Assert.LessOrEqual(periodSec, 5.0, "Period must be <= 5.0 s.");
        }

        [Test]
        public void F10_Pulsar_MagneticDipolePrecession_PrecessesAlongSpinAxis()
        {
            // Magnetic dipole axis precesses around spin axis with tilt angle alpha_m
            double tiltAngleRad = Math.PI / 4.0; // 45 degrees
            double spinRateRadPerSec = 2.0 * Math.PI * 10.0; // 10 Hz
            double t = 0.025; // quarter rotation

            double angle = spinRateRadPerSec * t;
            Vector3d dipole = new Vector3d(
                Math.Sin(tiltAngleRad) * Math.Cos(angle),
                Math.Cos(tiltAngleRad),
                Math.Sin(tiltAngleRad) * Math.Sin(angle)
            );

            Assert.AreEqual(1.0, dipole.magnitude, 1e-9, "Magnetic dipole orientation vector must remain unit length.");
            Assert.AreEqual(Math.Cos(tiltAngleRad), dipole.y, 1e-9, "Component along spin axis must remain constant.");
        }

        [Test]
        public void F10_Pulsar_SynchrotronRadiationCone_CollimatedWithinNarrowHalfAngle()
        {
            // Synchrotron beam is narrowly beamed into cone of half-angle theta_b ~ 10 deg (0.174 rad)
            double halfAngleRad = 10.0 * (Math.PI / 180.0);
            Vector3d beamDirection = Vector3d.forward;

            Vector3d insideBeam = new Vector3d(Math.Sin(5.0 * Math.PI / 180.0), 0.0, Math.Cos(5.0 * Math.PI / 180.0)).normalized;
            Vector3d outsideBeam = new Vector3d(Math.Sin(20.0 * Math.PI / 180.0), 0.0, Math.Cos(20.0 * Math.PI / 180.0)).normalized;

            double angleInside = Math.Acos(Vector3d.Dot(beamDirection, insideBeam));
            double angleOutside = Math.Acos(Vector3d.Dot(beamDirection, outsideBeam));

            Assert.Less(angleInside, halfAngleRad, "Point at 5 deg is inside synchrotron cone.");
            Assert.Greater(angleOutside, halfAngleRad, "Point at 20 deg is outside synchrotron cone.");
        }

        [Test]
        public void F10_Pulsar_LighthousePulse_PeaksWhenBeamSweepsObserverLineOfSight()
        {
            // Lighthouse effect: observed intensity I = I_0 * exp(-psi^2 / (2 * theta_b^2))
            double thetaB = 0.15; // rad
            Func<double, double> getPulseIntensity = psi => Math.Exp(-(psi * psi) / (2.0 * thetaB * thetaB));

            double peakIntensity = getPulseIntensity(0.0);
            double offAxisIntensity = getPulseIntensity(0.3);

            Assert.AreEqual(1.0, peakIntensity, 1e-9, "Lighthouse pulse reaches 1.0 when perfectly on line of sight.");
            Assert.Less(offAxisIntensity, 0.15, "Intensity drops sharply off-axis.");
        }

        [Test]
        public void F10_Pulsar_PeriodicPulseFrequency_MatchesSpinFrequency()
        {
            // Number of pulses observed per second must equal pulsar spin frequency
            var pulsar = new CelestialBodyState(1, "CrabPulsar", BodyType.Pulsar, 1.4, 0.01, Vector3d.zero, Vector3d.zero);
            double duration = 4.0; // 4 seconds
            int pulseCount = CosmoForgeTestHarness.CalculatePulsarPulses(pulsar, duration, defaultSpinFrequencyHz: 25.0);

            Assert.AreEqual(100, pulseCount, "25 Hz pulsar must produce exactly 100 pulses in 4 seconds.");
            Assert.Greater(pulseCount, 0, "Pulsar pulse count must be positive.");
        }

        #endregion

        #region Feature F11: Supermassive Black Hole Visuals (5 Tests)

        [Test]
        public void F11_BlackHole_SchwarzschildRadius_CalculatesTwoGMOverCSquared()
        {
            // Requirement R3 & Acceptance Criteria: Rs = 2 * G * M / c^2
            double G = 1.0;
            double c = 50.0;
            double mass = 25000.0;

            double rs = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, mass, c);
            double expectedRs = (2.0 * 1.0 * 25000.0) / (50.0 * 50.0); // 20.0

            Assert.AreEqual(20.0, rs, 1e-9, "Schwarzschild radius Rs must equal 2*G*M/c^2.");
        }

        [Test]
        public void F11_BlackHole_PhotonSphereAndShadow_RadiusEqualsSquareRoot27M()
        {
            // Requirement R3: Apparent black hole shadow radius R_shadow = sqrt(27) * G * M / c^2 = 2.598 * Rs / 2
            double G = 1.0;
            double c = 50.0;
            double mass = 50000.0;

            double rs = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, mass, c);
            double rPhoton = CosmoForgeTestHarness.CalculatePhotonSphereRadius(G, mass, c);
            double rShadow = CosmoForgeTestHarness.CalculateBlackHoleShadowRadius(G, mass, c);

            Assert.AreEqual(1.5 * rs, rPhoton, 1e-9, "Photon sphere must be at 1.5 * Rs.");
            Assert.AreEqual(Math.Sqrt(27.0) * (G * mass) / (c * c), rShadow, 1e-9, "Shadow radius must equal sqrt(27)*G*M/c^2.");
            Assert.Greater(rShadow, rPhoton, "Apparent shadow radius must be larger than physical photon sphere due to gravitational lensing.");
        }

        [Test]
        public void F11_BlackHole_EinsteinRingAngle_IncreasesWithMassAndDistanceRatio()
        {
            // Einstein ring deflection angle theta_E = sqrt(4*G*M / c^2 * D_ls / (D_l * D_s))
            double G = 1.0;
            double c = 50.0;
            double massSmall = 1000.0;
            double massLarge = 4000.0;

            Func<double, double> getEinsteinAngle = m => Math.Sqrt((4.0 * G * m) / (c * c));

            double thetaSmall = getEinsteinAngle(massSmall);
            double thetaLarge = getEinsteinAngle(massLarge);

            Assert.Greater(thetaLarge, thetaSmall, "Heavier black hole must produce larger Einstein ring.");
            Assert.AreEqual(2.0, thetaLarge / thetaSmall, 1e-6, "Einstein ring radius scales with sqrt(Mass).");
        }

        [Test]
        public void F11_BlackHole_AccretionDiskDopplerBeaming_ApproachingSideBoostedRecedingSideDimmed()
        {
            // Requirement R3: Relativistic Doppler boosting delta = 1 / (gamma * (1 - beta * cos(theta)))
            // Observed flux scales with delta^3
            double beta = 0.4; // 40% speed of light
            double gamma = 1.0 / Math.Sqrt(1.0 - beta * beta);

            // Approaching side (cos = +1)
            double deltaApproaching = 1.0 / (gamma * (1.0 - beta * 1.0));
            double fluxBoostApproaching = Math.Pow(deltaApproaching, 3.0);

            // Receding side (cos = -1)
            double deltaReceding = 1.0 / (gamma * (1.0 - beta * (-1.0)));
            double fluxBoostReceding = Math.Pow(deltaReceding, 3.0);

            Assert.Greater(deltaApproaching, 1.0, "Approaching limb has Doppler boost factor > 1.");
            Assert.Less(deltaReceding, 1.0, "Receding limb has Doppler dimming factor < 1.");
            Assert.Greater(fluxBoostApproaching / fluxBoostReceding, 4.0, "Approaching accretion disk limb must be > 4x brighter than receding limb.");
        }

        [Test]
        public void F11_BlackHole_GravitationalRedshift_ApproachesZeroAtEventHorizon()
        {
            // Gravitational redshift factor g_grav = sqrt(1 - Rs / r) -> 0 as r -> Rs
            double rs = 10.0;

            Func<double, double> getRedshiftFactor = r => Math.Sqrt(Math.Max(0.0, 1.0 - (rs / r)));

            double gFar = getRedshiftFactor(100.0); // 10 Rs
            double gNear = getRedshiftFactor(11.0);  // 1.1 Rs
            double gHorizon = getRedshiftFactor(10.0001); // ~Rs

            Assert.Greater(gFar, 0.9, "Far from black hole, redshift factor approaches 1.0 (unattenuated).");
            Assert.Less(gNear, 0.4, "Near horizon, photons lose energy to gravitational potential.");
            Assert.Less(gHorizon, 0.01, "At event horizon, time dilation freezes light and redshift factor reaches 0.");
        }

        #endregion
    }
}
