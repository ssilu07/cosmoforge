// ============================================================================
// CosmoForge Test Framework: Standard Astronomical Fixtures & Physics Oracles
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Integrator;
using CosmoForge.Physics.Trajectory;
using CosmoForge.Physics.Cataclysms;
using CosmoForge.Puzzles.Core;
using CosmoForge.Core.Performance;
using CosmoForge.UI.Workbench;
using CosmoForge.UI.Input;
using CosmoForge.Audio;

namespace CosmoForge.Tests.Harness
{
    /// <summary>
    /// Comprehensive test harness providing standardized astronomical fixtures,
    /// simulation steppers, and analytical physics oracles for automated verification.
    /// </summary>
    public static class CosmoForgeTestHarness
    {
        // Standard physical & simulation constants
        public const double DefaultG = 1.0;
        public const double DefaultC = 50.0;
        public const double StefanBoltzmannConstant = 5.670374419e-8;
        public const double StandardSolarMass = 1.989e30;
        public const double ChandrasekharLimitSolarMasses = 1.44;

        #region Standard Astronomical Fixtures

        /// <summary>
        /// Creates a two-body Sun-Earth system in circular Keplerian orbit.
        /// Sun mass = 10000.0, Earth mass = 1.0, orbital radius = 100.0.
        /// </summary>
        public static List<CelestialBodyState> CreateSunEarthSystem(double G = DefaultG, double softening = 0.0)
        {
            double mSun = 10000.0;
            double mEarth = 1.0;
            double totalMass = mSun + mEarth;
            double r = 100.0;

            // Barycentric coordinates
            double rSun = -(mEarth / totalMass) * r;
            double rEarth = (mSun / totalMass) * r;

            // Circular velocity: v_rel = sqrt(G * M_tot / r)
            double vRel = Math.Sqrt(G * totalMass / r);
            double vSun = -(mEarth / totalMass) * vRel;
            double vEarth = (mSun / totalMass) * vRel;

            var sun = new CelestialBodyState(
                id: 1,
                name: "Sun",
                type: BodyType.Star,
                mass: mSun,
                radius: 10.0,
                position: new Vector3d(rSun, 0.0, 0.0),
                velocity: new Vector3d(0.0, vSun, 0.0)
            )
            {
                SurfaceTemperature = 5778.0,
                CoreFuel = 1.0
            };

            var earth = new CelestialBodyState(
                id: 2,
                name: "Earth",
                type: BodyType.Terrestrial,
                mass: mEarth,
                radius: 1.0,
                position: new Vector3d(rEarth, 0.0, 0.0),
                velocity: new Vector3d(0.0, vEarth, 0.0)
            )
            {
                SurfaceTemperature = 288.0,
                CoreFuel = 0.0
            };

            return new List<CelestialBodyState> { sun, earth };
        }

        /// <summary>
        /// Creates a three-body Sun-Earth-Moon system.
        /// </summary>
        public static List<CelestialBodyState> CreateSunEarthMoonSystem(double G = DefaultG)
        {
            var system = CreateSunEarthSystem(G);
            CelestialBodyState earth = system[1];

            double mMoon = 0.0123; // Moon-to-Earth mass ratio
            double rMoonRel = 3.0; // Scaled lunar distance
            double vMoonRel = Math.Sqrt(G * earth.Mass / rMoonRel);

            var moon = new CelestialBodyState(
                id: 3,
                name: "Moon",
                type: BodyType.Moon,
                mass: mMoon,
                radius: 0.27,
                position: earth.Position + new Vector3d(rMoonRel, 0.0, 0.0),
                velocity: earth.Velocity + new Vector3d(0.0, vMoonRel, 0.0)
            )
            {
                SurfaceTemperature = 250.0,
                CoreFuel = 0.0
            };

            system.Add(moon);
            return system;
        }

        /// <summary>
        /// Creates an isolated Earth-Moon system for restricted three-body / Lagrange point testing.
        /// </summary>
        public static List<CelestialBodyState> CreateEarthMoonSystem(double G = DefaultG, double separation = 384.4)
        {
            double mEarth = 597.2;
            double mMoon = 7.34;
            double totalMass = mEarth + mMoon;

            double rEarth = -(mMoon / totalMass) * separation;
            double rMoon = (mEarth / totalMass) * separation;

            double vRel = Math.Sqrt(G * totalMass / separation);
            double vEarth = -(mMoon / totalMass) * vRel;
            double vMoon = (mEarth / totalMass) * vRel;

            var earth = new CelestialBodyState(
                id: 1,
                name: "Earth",
                type: BodyType.Terrestrial,
                mass: mEarth,
                radius: 6.371,
                position: new Vector3d(rEarth, 0.0, 0.0),
                velocity: new Vector3d(0.0, vEarth, 0.0)
            );

            var moon = new CelestialBodyState(
                id: 2,
                name: "Moon",
                type: BodyType.Moon,
                mass: mMoon,
                radius: 1.737,
                position: new Vector3d(rMoon, 0.0, 0.0),
                velocity: new Vector3d(0.0, vMoon, 0.0)
            );

            return new List<CelestialBodyState> { earth, moon };
        }

        /// <summary>
        /// Creates an equal-mass binary star system in mutual circular orbit.
        /// </summary>
        public static List<CelestialBodyState> CreateEqualMassBinarySystem(double separation = 50.0, double mass = 500.0, double G = DefaultG)
        {
            double vRel = Math.Sqrt(G * (2.0 * mass) / separation);
            double v = vRel * 0.5;
            double r = separation * 0.5;

            var starA = new CelestialBodyState(
                id: 1,
                name: "StarA",
                type: BodyType.Star,
                mass: mass,
                radius: 4.0,
                position: new Vector3d(-r, 0.0, 0.0),
                velocity: new Vector3d(0.0, -v, 0.0)
            )
            {
                SurfaceTemperature = 6000.0,
                CoreFuel = 1.0
            };

            var starB = new CelestialBodyState(
                id: 2,
                name: "StarB",
                type: BodyType.Star,
                mass: mass,
                radius: 4.0,
                position: new Vector3d(r, 0.0, 0.0),
                velocity: new Vector3d(0.0, v, 0.0)
            )
            {
                SurfaceTemperature = 6000.0,
                CoreFuel = 1.0
            };

            return new List<CelestialBodyState> { starA, starB };
        }

        /// <summary>
        /// Creates a circumbinary system with two central stars and a distant habitable planet.
        /// </summary>
        public static List<CelestialBodyState> CreateCircumbinarySystem(double binarySeparation = 20.0, double G = DefaultG)
        {
            double mA = 600.0;
            double mB = 400.0;
            double mTot = mA + mB;

            double rA = -(mB / mTot) * binarySeparation;
            double rB = (mA / mTot) * binarySeparation;

            double vBinRel = Math.Sqrt(G * mTot / binarySeparation);
            double vA = -(mB / mTot) * vBinRel;
            double vB = (mA / mTot) * vBinRel;

            var starA = new CelestialBodyState(1, "StarA", BodyType.Star, mA, 3.5, new Vector3d(rA, 0.0, 0.0), new Vector3d(0.0, vA, 0.0))
            {
                SurfaceTemperature = 6500.0,
                CoreFuel = 1.0
            };

            var starB = new CelestialBodyState(2, "StarB", BodyType.Star, mB, 2.5, new Vector3d(rB, 0.0, 0.0), new Vector3d(0.0, vB, 0.0))
            {
                SurfaceTemperature = 4500.0,
                CoreFuel = 1.0
            };

            // Critical stability radius for circumbinary planet: Holman-Wiegert approx ~ 2.5 * binarySeparation
            double rPlanet = binarySeparation * 3.5;
            double vPlanet = Math.Sqrt(G * mTot / rPlanet);

            var planet = new CelestialBodyState(3, "Tatooine", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(rPlanet, 0.0, 0.0), new Vector3d(0.0, vPlanet, 0.0))
            {
                SurfaceTemperature = 295.0,
                CoreFuel = 0.0
            };

            return new List<CelestialBodyState> { starA, starB, planet };
        }

        /// <summary>
        /// Creates a Supermassive Black Hole system with an orbiting test body.
        /// </summary>
        public static List<CelestialBodyState> CreateBlackHoleSystem(double mass = 50000.0, double G = DefaultG, double c = DefaultC)
        {
            double rs = CalculateSchwarzschildRadius(G, mass, c);

            var blackHole = new CelestialBodyState(
                id: 1,
                name: "Gargantua",
                type: BodyType.BlackHole,
                mass: mass,
                radius: rs,
                position: Vector3d.zero,
                velocity: Vector3d.zero
            )
            {
                SurfaceTemperature = 0.0,
                CoreFuel = 0.0
            };

            double rOrbit = rs * 5.0; // Safe distance beyond ISCO (3 Rs)
            double vOrbit = Math.Sqrt(G * mass / rOrbit);

            var testProbe = new CelestialBodyState(
                id: 2,
                name: "ScienceProbe",
                type: BodyType.AsteroidProbe,
                mass: 0.01,
                radius: 0.1,
                position: new Vector3d(rOrbit, 0.0, 0.0),
                velocity: new Vector3d(0.0, vOrbit, 0.0)
            );

            return new List<CelestialBodyState> { blackHole, testProbe };
        }

        /// <summary>
        /// Creates a high-spin pulsar system.
        /// </summary>
        public static List<CelestialBodyState> CreatePulsarSystem(double mass = 1400.0, double spinFrequency = 30.0)
        {
            var pulsar = new CelestialBodyState(
                id: 1,
                name: "CrabPulsar",
                type: BodyType.Pulsar,
                mass: mass,
                radius: 0.5,
                position: Vector3d.zero,
                velocity: Vector3d.zero
            )
            {
                SurfaceTemperature = 1e6,
                CoreFuel = 0.0
            };

            return new List<CelestialBodyState> { pulsar };
        }

        /// <summary>
        /// Creates a Gas Giant and icy moon setup inside/near fluid Roche limit.
        /// </summary>
        public static List<CelestialBodyState> CreateGasGiantWithMoon(double G = DefaultG, bool placeInsideRoche = true)
        {
            double mGiant = 10000.0;
            double rGiant = 20.0;
            double rhoGiant = 1.33; // bulk density
            double rhoMoon = 0.92;  // icy moon density

            double dRoche = CalculateFluidRocheLimit(rGiant, rhoGiant, rhoMoon);
            double moonDistance = placeInsideRoche ? dRoche * 0.85 : dRoche * 1.5;
            double vMoon = Math.Sqrt(G * mGiant / moonDistance);

            var giant = new CelestialBodyState(
                id: 1,
                name: "Cronus",
                type: BodyType.GasGiant,
                mass: mGiant,
                radius: rGiant,
                position: Vector3d.zero,
                velocity: Vector3d.zero
            );

            var moon = new CelestialBodyState(
                id: 2,
                name: "Glacies",
                type: BodyType.Moon,
                mass: 0.1,
                radius: 0.8,
                position: new Vector3d(moonDistance, 0.0, 0.0),
                velocity: new Vector3d(0.0, vMoon, 0.0)
            );

            return new List<CelestialBodyState> { giant, moon };
        }

        /// <summary>
        /// Creates an inbound asteroid collision course towards Earth.
        /// </summary>
        public static List<CelestialBodyState> CreateAsteroidCollisionCourse(double G = DefaultG)
        {
            double mEarth = 5000.0;
            double rEarth = 10.0;

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, mEarth, rEarth, Vector3d.zero, Vector3d.zero);

            // Asteroid on hyperbolic/direct impact trajectory
            double startDistance = 200.0;
            double approachSpeed = 4.0;

            var asteroid = new CelestialBodyState(
                id: 2,
                name: "Apophis",
                type: BodyType.AsteroidProbe,
                mass: 0.05,
                radius: 0.5,
                position: new Vector3d(startDistance, 2.0, 0.0), // small impact parameter (will hit Earth)
                velocity: new Vector3d(-approachSpeed, 0.0, 0.0)
            );

            return new List<CelestialBodyState> { earth, asteroid };
        }

        #endregion

        #region Physics Simulation Steppers

        /// <summary>
        /// Executes a fixed number of integration steps with the given integrator.
        /// </summary>
        public static void StepSimulation(IGravityIntegrator integrator, List<CelestialBodyState> bodies, double dt, int steps)
        {
            if (integrator == null || bodies == null) return;
            integrator.InitializeAccelerations(bodies);
            for (int i = 0; i < steps; i++)
            {
                integrator.Step(bodies, dt);
            }
        }

        #endregion

        #region Analytical Physics Oracles

        /// <summary>
        /// Circular Keplerian speed: v = sqrt(G * M / r)
        /// </summary>
        public static double CalculateKeplerianCircularSpeed(double G, double M, double r)
        {
            return Math.Sqrt(G * M / r);
        }

        /// <summary>
        /// Orbital period for two-body Keplerian orbit: T = 2 * pi * sqrt(a^3 / (G * M))
        /// </summary>
        public static double CalculateOrbitalPeriod(double G, double M, double a)
        {
            return 2.0 * Math.PI * Math.Sqrt((a * a * a) / (G * M));
        }

        /// <summary>
        /// Vis-Viva equation: v^2 = G * M * (2/r - 1/a)
        /// </summary>
        public static double CalculateVisVivaSpeed(double G, double M, double r, double a)
        {
            return Math.Sqrt(G * M * (2.0 / r - 1.0 / a));
        }

        /// <summary>
        /// Escape speed: v_esc = sqrt(2 * G * M / r)
        /// </summary>
        public static double CalculateEscapeSpeed(double G, double M, double r)
        {
            return Math.Sqrt(2.0 * G * M / r);
        }

        /// <summary>
        /// Specific orbital energy: epsilon = v^2 / 2 - G * M / r
        /// </summary>
        public static double CalculateSpecificOrbitalEnergy(double G, double M, Vector3d r, Vector3d v)
        {
            double rMag = r.magnitude;
            if (rMag <= 1e-15) return double.NegativeInfinity;
            return (v.sqrMagnitude * 0.5) - (G * M / rMag);
        }

        /// <summary>
        /// Laplace-Runge-Lenz eccentricity vector: e = (v x h) / (G * M) - r / |r|
        /// </summary>
        public static Vector3d CalculateEccentricityVector(double G, double M, Vector3d r, Vector3d v)
        {
            double mu = G * M;
            Vector3d h = Vector3d.Cross(r, v);
            Vector3d vCrossH = Vector3d.Cross(v, h);
            double rMag = r.magnitude;
            if (rMag <= 1e-15 || mu <= 1e-15) return Vector3d.zero;
            return (vCrossH / mu) - (r / rMag);
        }

        /// <summary>
        /// Semi-major axis: a = -G * M / (2 * epsilon)
        /// </summary>
        public static double CalculateSemiMajorAxis(double G, double M, Vector3d r, Vector3d v)
        {
            double energy = CalculateSpecificOrbitalEnergy(G, M, r, v);
            if (Math.Abs(energy) <= 1e-15) return double.PositiveInfinity;
            return -(G * M) / (2.0 * energy);
        }

        /// <summary>
        /// Schwarzschild radius: Rs = 2 * G * M / c^2
        /// </summary>
        public static double CalculateSchwarzschildRadius(double G, double M, double c)
        {
            return (2.0 * G * M) / (c * c);
        }

        /// <summary>
        /// Black hole photon sphere radius: R_photon = 1.5 * Rs = 3 * G * M / c^2
        /// </summary>
        public static double CalculatePhotonSphereRadius(double G, double M, double c)
        {
            return 1.5 * CalculateSchwarzschildRadius(G, M, c);
        }

        /// <summary>
        /// Apparent black hole shadow radius: R_shadow = sqrt(27) * G * M / c^2
        /// </summary>
        public static double CalculateBlackHoleShadowRadius(double G, double M, double c)
        {
            return Math.Sqrt(27.0) * (G * M) / (c * c);
        }

        /// <summary>
        /// Fluid Roche limit: d_Roche = 2.44 * R_M * (rho_M / rho_m)^(1/3)
        /// </summary>
        public static double CalculateFluidRocheLimit(double primaryRadius, double primaryDensity, double secondaryDensity)
        {
            return 2.44 * primaryRadius * Math.Cbrt(primaryDensity / secondaryDensity);
        }

        /// <summary>
        /// Stefan-Boltzmann blackbody equilibrium surface temperature:
        /// T = (L * (1 - A) / (16 * pi * sigma * d^2))^(1/4)
        /// </summary>
        public static double CalculateBlackbodyTemperature(double luminosity, double distance, double albedo = 0.3, double sigma = StefanBoltzmannConstant)
        {
            double denom = 16.0 * Math.PI * sigma * distance * distance;
            if (denom <= 1e-30) return 0.0;
            double flux = (luminosity * (1.0 - albedo)) / denom;
            return Math.Pow(flux, 0.25);
        }

        /// <summary>
        /// Inelastic merger conserved velocity: v_merged = (m1 * v1 + m2 * v2) / (m1 + m2)
        /// </summary>
        public static Vector3d CalculateInelasticMergerVelocity(double m1, Vector3d v1, double m2, Vector3d v2)
        {
            double totalMass = m1 + m2;
            if (totalMass <= 1e-15) return Vector3d.zero;
            return (v1 * m1 + v2 * m2) / totalMass;
        }

        /// <summary>
        /// Holman-Wiegert critical stability radius for circumbinary orbits (Holman & Wiegert 1999).
        /// a_c = a_b * (1.60 + 5.10*e_b - 2.22*e_b^2 + 4.12*mu - 4.27*e_b*mu - 5.09*mu^2 + 4.61*e_b^2*mu^2)
        /// </summary>
        public static double CalculateHolmanWiegertCriticalRadius(double binarySemiMajorAxis, double binaryEccentricity, double massRatio)
        {
            double eb = binaryEccentricity;
            double mu = massRatio;
            double factor = 1.60 + 5.10 * eb - 2.22 * eb * eb
                            + 4.12 * mu - 4.27 * eb * mu
                            - 5.09 * mu * mu + 4.61 * eb * eb * mu * mu;
            return binarySemiMajorAxis * factor;
        }

        /// <summary>
        /// Peters' formula for binary gravitational wave orbital decay rate:
        /// da/dt = - (64/5) * G^3 * m1 * m2 * (m1 + m2) / (c^5 * a^3)
        /// </summary>
        public static double CalculatePetersOrbitalDecayRate(double G, double c, double m1, double m2, double a)
        {
            double c5 = Math.Pow(c, 5.0);
            double a3 = Math.Pow(a, 3.0);
            double g3 = Math.Pow(G, 3.0);
            return -(64.0 / 5.0) * (g3 * m1 * m2 * (m1 + m2)) / (c5 * a3);
        }

        /// <summary>
        /// Gravitational wave frequency for circular binary: f_GW = 2 * f_orb = (1 / pi) * sqrt(G * M_tot / a^3)
        /// </summary>
        public static double CalculateGravitationalWaveFrequency(double G, double totalMass, double a)
        {
            return (1.0 / Math.PI) * Math.Sqrt(G * totalMass / (a * a * a));
        }

        /// <summary>
        /// Spacetime metric depression formula:
        /// z_warp(x, y) = - sum_i (G * m_i / (sqrt((x-xi)^2 + (y-yi)^2) + c^2))
        /// </summary>
        public static double CalculateSpacetimeCurvatureSample(double x, double z, IList<CelestialBodyState> bodies, double G = DefaultG, double cSquared = DefaultC * DefaultC)
        {
            if (bodies == null) return 0.0;
            double totalDepression = 0.0;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b.IsDestroyed) continue;
                double dx = x - b.Position.x;
                double dz = z - b.Position.z;
                double dist = Math.Sqrt(dx * dx + dz * dz);
                totalDepression -= (G * b.Mass) / (dist + cSquared);
            }
            return totalDepression;
        }

        /// <summary>
        /// Classifies the planetary surface biome based on equilibrium surface temperature.
        /// </summary>
        public static BiomeType ClassifySurfaceBiome(double surfaceTemperatureKelvin)
        {
            if (surfaceTemperatureKelvin < 273.15)
                return BiomeType.GlacialIce;
            if (surfaceTemperatureKelvin <= 373.15)
                return BiomeType.TemperateOceans;
            return BiomeType.MagmaFissures;
        }

        /// <summary>
        /// Calculates the exact 1:1 synchronous rotational period for tidal locking.
        /// </summary>
        public static double CalculateSynchronousRotationPeriod(double G, double primaryMass, double orbitalDistance)
        {
            return CalculateOrbitalPeriod(G, primaryMass, orbitalDistance);
        }

        /// <summary>
        /// Evaluates whether a body is tidally locked to its parent orbital period.
        /// </summary>
        public static bool IsTidallyLocked(double rotationPeriod, double orbitalPeriod, double tolerance = 1e-4)
        {
            if (orbitalPeriod <= 0 || rotationPeriod <= 0) return false;
            double ratio = rotationPeriod / orbitalPeriod;
            return Math.Abs(ratio - 1.0) <= tolerance;
        }

        /// <summary>
        /// Calculates alternating counter-rotating zonal wind velocity as a function of planetary latitude.
        /// </summary>
        public static double CalculateZonalWindVelocity(double latitudeRadians, double peakWindSpeedKmH = 150.0, int bandCount = 8)
        {
            return Math.Sin(latitudeRadians * bandCount) * peakWindSpeedKmH;
        }

        /// <summary>
        /// Simulates stellar core fuel consumption over a specified delta time.
        /// </summary>
        public static void BurnStellarFuel(CelestialBodyState star, double deltaTime, double burnRatePerSec = 0.001)
        {
            if (star == null) return;
            double fuelConsumed = burnRatePerSec * deltaTime;
            star.CoreFuel = Math.Max(0.0, star.CoreFuel - fuelConsumed);
        }

        /// <summary>
        /// Calculates the expected pulse count emitted by a rotating pulsar over a given observation duration.
        /// </summary>
        public static int CalculatePulsarPulses(CelestialBodyState pulsar, double durationSeconds, double defaultSpinFrequencyHz = 25.0)
        {
            if (pulsar == null || durationSeconds <= 0.0) return 0;
            return (int)Math.Floor(defaultSpinFrequencyHz * durationSeconds);
        }

        /// <summary>
        /// Resolves a supernova explosion event, destroying the progenitor and computing remnant mass.
        /// </summary>
        public static bool ResolveSupernova(CelestialBodyState progenitor, out double remnantMass, bool isTypeIa = true)
        {
            if (progenitor == null)
            {
                remnantMass = 0.0;
                return false;
            }

            progenitor.IsDestroyed = true;
            if (isTypeIa)
            {
                // Type Ia thermonuclear supernova completely disrupts progenitor white dwarf; zero remnant mass
                remnantMass = 0.0;
                return true;
            }
            else
            {
                // Core-collapse supernova: yields neutron star or black hole remnant
                remnantMass = progenitor.Mass >= 20.0 ? progenitor.Mass * 0.15 : Math.Min(2.1, Math.Max(1.4, progenitor.Mass * 0.1));
                return true;
            }
        }

        /// <summary>
        /// Generates an isotropic collision debris cloud strictly conserving total impact linear momentum.
        /// </summary>
        public static List<CelestialBodyState> GenerateCollisionDebrisCloud(Vector3d impactVelocity, Vector3d impactPoint, double totalEjectedMass, int debrisParticleCount, double expansionSpeed = 5.0)
        {
            var debris = new List<CelestialBodyState>(debrisParticleCount);
            if (debrisParticleCount <= 0 || totalEjectedMass <= 0.0) return debris;

            double particleMass = totalEjectedMass / debrisParticleCount;
            for (int i = 0; i < debrisParticleCount; i++)
            {
                double phi = 2.0 * Math.PI * i / debrisParticleCount;
                double theta = Math.PI * (i + 0.5) / debrisParticleCount;
                Vector3d radialDir = new Vector3d(
                    Math.Sin(theta) * Math.Cos(phi),
                    Math.Sin(theta) * Math.Sin(phi),
                    Math.Cos(theta)
                ).normalized;

                Vector3d velocity = impactVelocity + radialDir * expansionSpeed;
                var p = new CelestialBodyState(1000 + i, $"Debris_{i}", BodyType.DebrisParticle, particleMass, 0.1, impactPoint, velocity);
                debris.Add(p);
            }

            // Adjust velocities slightly so total linear momentum exactly equals totalEjectedMass * impactVelocity
            Vector3d totalP = Vector3d.zero;
            for (int i = 0; i < debris.Count; i++)
            {
                totalP += debris[i].Velocity * debris[i].Mass;
            }
            Vector3d expectedP = impactVelocity * totalEjectedMass;
            Vector3d deltaV = (expectedP - totalP) / totalEjectedMass;
            for (int i = 0; i < debris.Count; i++)
            {
                debris[i].Velocity += deltaV;
            }

            return debris;
        }

        #endregion
    }

    /// <summary>
    /// Planetary surface biome classifications.
    /// </summary>
    public enum BiomeType
    {
        GlacialIce,
        TemperateOceans,
        MagmaFissures
    }

    #region Standard Reference & Mock Implementations for Interface Contracts

    /// <summary>
    /// Reference implementation of ICollisionEngine that resolves inelastic collisions
    /// with momentum and mass conservation, calculates energy dissipation, and evaluates Roche limits.
    /// </summary>
    public class HarnessCollisionEngine : ICollisionEngine
    {
        public double GravitationalConstant { get; set; } = 1.0;
        public int CollisionCount { get; private set; }
        public int RocheDisruptionCount { get; private set; }
        public List<CollisionEvent> RecordedCollisions { get; } = new List<CollisionEvent>();
        public List<RocheDisruptionEvent> RecordedRocheEvents { get; } = new List<RocheDisruptionEvent>();

        public void CheckAndResolveCollisions(List<CelestialBodyState> bodies, double deltaTime, Action<CollisionEvent> onMerged)
        {
            if (bodies == null || bodies.Count < 2) return;

            for (int i = 0; i < bodies.Count; i++)
            {
                var b1 = bodies[i];
                if (b1 == null || b1.IsDestroyed) continue;

                for (int j = i + 1; j < bodies.Count; j++)
                {
                    var b2 = bodies[j];
                    if (b2 == null || b2.IsDestroyed) continue;

                    double dist = (b1.Position - b2.Position).magnitude;
                    double sumRadii = b1.Radius + b2.Radius;

                    if (dist <= sumRadii)
                    {
                        CelestialBodyState primary = b1.Mass >= b2.Mass ? b1 : b2;
                        CelestialBodyState secondary = b1.Mass >= b2.Mass ? b2 : b1;

                        double m1 = primary.Mass;
                        double m2 = secondary.Mass;
                        double totalMass = m1 + m2;
                        Vector3d v1 = primary.Velocity;
                        Vector3d v2 = secondary.Velocity;

                        Vector3d vMerged = totalMass > 1e-15 ? (v1 * m1 + v2 * m2) / totalMass : Vector3d.zero;
                        double mu = totalMass > 1e-15 ? (m1 * m2) / totalMass : 0.0;
                        Vector3d vRel = v1 - v2;
                        double dissipatedKinetic = 0.5 * mu * vRel.sqrMagnitude;

                        double bindingEnergy = primary.Radius > 1e-15 ? (3.0 / 5.0) * (GravitationalConstant * m1 * m1) / primary.Radius : 1.0;
                        bool catastrophic = dissipatedKinetic > 5.0 * bindingEnergy;

                        double mergedRadius = Math.Cbrt((primary.Radius * primary.Radius * primary.Radius) + (secondary.Radius * secondary.Radius * secondary.Radius));

                        primary.Mass = totalMass;
                        primary.Velocity = vMerged;
                        primary.Radius = mergedRadius;
                        secondary.IsDestroyed = true;

                        CollisionCount++;
                        var evt = new CollisionEvent
                        {
                            Primary = primary,
                            Secondary = secondary,
                            ImpactPoint = (primary.Position + secondary.Position) * 0.5,
                            MergedVelocity = vMerged,
                            ConservedMass = totalMass,
                            DissipatedKineticEnergy = dissipatedKinetic,
                            MergedRadius = mergedRadius,
                            CatastrophicFragmentation = catastrophic
                        };

                        RecordedCollisions.Add(evt);
                        onMerged?.Invoke(evt);
                    }
                }
            }
        }

        public void EvaluateRocheLimits(List<CelestialBodyState> bodies, Action<RocheDisruptionEvent> onDisrupted)
        {
            if (bodies == null || bodies.Count < 2) return;

            for (int i = 0; i < bodies.Count; i++)
            {
                var primary = bodies[i];
                if (primary == null || primary.IsDestroyed) continue;

                for (int j = 0; j < bodies.Count; j++)
                {
                    if (i == j) continue;
                    var moon = bodies[j];
                    if (moon == null || moon.IsDestroyed) continue;

                    double volP = (4.0 / 3.0) * Math.PI * Math.Pow(primary.Radius, 3.0);
                    double volM = (4.0 / 3.0) * Math.PI * Math.Pow(moon.Radius, 3.0);
                    if (volP <= 1e-15 || volM <= 1e-15) continue;

                    double rhoP = primary.Mass / volP;
                    double rhoM = moon.Mass / volM;
                    if (rhoM <= 1e-15) continue;

                    double dRoche = 2.44 * primary.Radius * Math.Cbrt(rhoP / rhoM);
                    double currentDist = (moon.Position - primary.Position).magnitude;

                    if (currentDist < dRoche && currentDist > primary.Radius)
                    {
                        moon.IsDestroyed = true;
                        RocheDisruptionCount++;
                        int spawnedParticles = (int)Math.Clamp(moon.Mass * 10.0, 50.0, 500.0);

                        var evt = new RocheDisruptionEvent
                        {
                            Primary = primary,
                            DisruptedMoon = moon,
                            RocheDistance = dRoche,
                            SpawnedRingParticles = spawnedParticles
                        };

                        RecordedRocheEvents.Add(evt);
                        onDisrupted?.Invoke(evt);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reference implementation of IPuzzleMission that evaluates orbital requirements,
    /// thresholds, progress, and victory star conditions.
    /// </summary>
    public class HarnessPuzzleMission : IPuzzleMission
    {
        public string MissionId { get; set; } = "mission_generic";
        public string DisplayTitle { get; set; } = "Generic Orbital Mission";
        public string Description { get; set; } = "Harness reference orbital mission.";

        public double TargetEccentricityThreshold { get; set; } = 0.05;
        public double TargetMinDistance { get; set; } = 10.0;
        public double TargetMaxDistance { get; set; } = 150.0;
        public double TargetParticleRetentionRatio { get; set; } = 0.80;
        public double RequiredSurvivingTime { get; set; } = 10.0;
        public double ElapsedEvaluationTime { get; private set; } = 0.0;
        public int EvaluatedStars { get; private set; } = 0;

        public Action OnVictoryCelebration { get; set; }
        public List<CelestialBodyState> InitialBodiesTemplate { get; set; } = new List<CelestialBodyState>();

        public HarnessPuzzleMission(string missionId, string title, string description)
        {
            MissionId = missionId;
            DisplayTitle = title;
            Description = description;
        }

        public void Initialize(SimulationWorld world)
        {
            ElapsedEvaluationTime = 0.0;
            EvaluatedStars = 0;
            world.ClearBodies();
            if (InitialBodiesTemplate != null)
            {
                for (int i = 0; i < InitialBodiesTemplate.Count; i++)
                {
                    var b = InitialBodiesTemplate[i];
                    world.AddBody(new CelestialBodyState(b.Id, b.Name, b.Type, b.Mass, b.Radius, b.Position, b.Velocity)
                    {
                        SurfaceTemperature = b.SurfaceTemperature,
                        CoreFuel = b.CoreFuel
                    });
                }
            }
        }

        public PuzzleEvaluationResult EvaluateTick(SimulationWorld world, double deltaTime)
        {
            ElapsedEvaluationTime += deltaTime;
            float progress = Mathf.Clamp01((float)(ElapsedEvaluationTime / Math.Max(0.001, RequiredSurvivingTime)));

            if (world.Bodies == null || world.Bodies.Count == 0)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, progress, "No bodies in simulation.");
            }

            for (int i = 0; i < world.Bodies.Count; i++)
            {
                var b = world.Bodies[i];
                if (b.IsDestroyed)
                {
                    return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, progress, $"Body {b.Name} was destroyed.");
                }
            }

            if (world.Bodies.Count >= 2)
            {
                var attractor = world.Bodies[0];
                var probe = world.Bodies[1];
                Vector3d r = probe.Position - attractor.Position;
                double dist = r.magnitude;

                if (dist <= attractor.Radius + probe.Radius)
                {
                    return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, progress, "Probe crashed into primary.");
                }

                if (dist > TargetMaxDistance * 2.0)
                {
                    return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, progress, "Probe escaped gravitational well.");
                }

                Vector3d eVec = CosmoForgeTestHarness.CalculateEccentricityVector(1.0, attractor.Mass, r, probe.Velocity - attractor.Velocity);
                double ecc = eVec.magnitude;

                if (ElapsedEvaluationTime >= RequiredSurvivingTime)
                {
                    int stars = 1;
                    if (ecc <= TargetEccentricityThreshold) stars = 3;
                    else if (ecc <= TargetEccentricityThreshold * 2.4) stars = 2;

                    EvaluatedStars = stars;
                    OnVictoryCelebration?.Invoke();
                    return new PuzzleEvaluationResult(PuzzleStatus.Victory, stars, 1.0f, "Mission Objective Completed!");
                }
            }

            return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, progress, "Orbiting...");
        }

        public void ResetMission(SimulationWorld world)
        {
            Initialize(world);
        }
    }

    /// <summary>
    /// Reference implementation of IWorkbenchController that manages workbench mode,
    /// time scale, pause states, selection, and dispatches mode transition events.
    /// </summary>
    public class HarnessWorkbenchController : IWorkbenchController
    {
        public WorkbenchMode ActiveMode { get; private set; } = WorkbenchMode.FreeSandbox;
        public bool IsPaused { get; set; } = false;
        public double TimeScale { get; set; } = 1.0;
        public int SelectedBodyId { get; set; } = -1;
        public int ModeChangeCount { get; private set; } = 0;

        public event Action<WorkbenchMode> OnModeChanged;

        public void SwitchMode(WorkbenchMode newMode, List<CelestialBodyState> activeBodies)
        {
            if (newMode == ActiveMode)
            {
                return; // Idempotent
            }

            ActiveMode = newMode;
            SelectedBodyId = -1;
            ModeChangeCount++;

            if (activeBodies != null)
            {
                activeBodies.Clear();
                if (newMode == WorkbenchMode.SolarSystemPreset)
                {
                    activeBodies.AddRange(CosmoForgeTestHarness.CreateSunEarthSystem());
                }
            }

            if (newMode == WorkbenchMode.OrbitalPuzzles)
            {
                IsPaused = true;
                TimeScale = 0.0;
            }

            OnModeChanged?.Invoke(newMode);
        }

        public void StepSimulation(List<CelestialBodyState> bodies, double dt)
        {
            if (IsPaused || TimeScale == 0.0 || bodies == null) return;
            double effectiveDt = dt * TimeScale;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b.IsDestroyed) continue;
                b.Position += b.Velocity * effectiveDt;
            }
        }
    }

    /// <summary>
    /// Reference implementation of ITouchInputHandler that processes camera pan,
    /// log-scale pinch zoom, twist rotation, single-tap selection, and double-tap focus tracking.
    /// </summary>
    public class HarnessTouchInputHandler : ITouchInputHandler
    {
        public TouchGestureType LastGesture { get; set; } = TouchGestureType.None;
        public Vector3 CameraPosition { get; set; } = new Vector3(0f, 100f, 0f);
        public float CameraDistance { get; set; } = 100f;
        public float CameraYaw { get; set; } = 0f;
        public int FocusedBodyId { get; set; } = -1;
        public bool IsTrackingFocus { get; set; } = false;

        public void ProcessPan(Vector2 deltaScreen, float sensitivity)
        {
            CameraPosition += new Vector3(deltaScreen.x * sensitivity, 0f, deltaScreen.y * sensitivity);
            LastGesture = TouchGestureType.Pan;
        }

        public void ProcessPinch(float initialDistance, float currentDistance, float minZoom, float maxZoom)
        {
            float safeCurrent = Mathf.Max(1e-5f, currentDistance);
            float ratio = initialDistance / safeCurrent;
            CameraDistance = Mathf.Clamp(CameraDistance * ratio, minZoom, maxZoom);
            LastGesture = TouchGestureType.PinchZoom;
        }

        public void ProcessTwist(float angleDeltaDegrees)
        {
            CameraYaw += angleDeltaDegrees;
            LastGesture = TouchGestureType.Twist;
        }

        public int ProcessSingleTap(Vector3 tapWorldPos, IList<CelestialBodyState> bodies, double maxSelectionDistance)
        {
            LastGesture = TouchGestureType.SingleTap;
            int bestId = -1;
            double minDist = maxSelectionDistance;

            if (bodies != null)
            {
                for (int i = 0; i < bodies.Count; i++)
                {
                    var b = bodies[i];
                    if (b.IsDestroyed) continue;
                    double d = (b.Position.ToVector3() - tapWorldPos).magnitude;
                    if (d < minDist)
                    {
                        minDist = d;
                        bestId = b.Id;
                    }
                }
            }

            FocusedBodyId = bestId;
            return bestId;
        }

        public void ProcessDoubleTap(int bodyId)
        {
            LastGesture = TouchGestureType.DoubleTap;
            FocusedBodyId = bodyId;
            IsTrackingFocus = true;
        }
    }

    /// <summary>
    /// Reference implementation of IProceduralAudioEngine that computes procedural frequency
    /// modulation, envelope decay, impulse intervals, and safe soft-clipped audio buffer generation.
    /// </summary>
    public class HarnessAudioEngine : IProceduralAudioEngine
    {
        public float MasterVolume { get; set; } = 1.0f;
        public double SampleRate { get; set; } = 48000.0;

        public double GetSpacetimeHumFrequency(double metricWarp)
        {
            double baseFreq = 55.0;
            return baseFreq * (1.0 - 0.35 * Math.Tanh(Math.Abs(metricWarp) / 20.0));
        }

        public double GetPulsarImpulseInterval(double spinFrequencyHz)
        {
            double nyquist = SampleRate * 0.5;
            double clamped = Math.Min(spinFrequencyHz, nyquist - 100.0);
            return SampleRate / Math.Max(1.0, clamped);
        }

        public double GetLaunchBurstFrequency(double elapsedSeconds)
        {
            return 180.0 * Math.Exp(-elapsedSeconds / 0.045) + 42.0;
        }

        public double GetLaunchBurstAmplitude(double elapsedSeconds)
        {
            return Math.Exp(-elapsedSeconds / 0.12);
        }

        public double GetSupernovaBlastPitch(double elapsedSeconds)
        {
            return Math.Max(24.0, 95.0 - (71.0 * (elapsedSeconds / 2.5)));
        }

        public double GetSupernovaEnvelope(double elapsedSeconds, double tau)
        {
            return Math.Exp(-elapsedSeconds / tau);
        }

        public void FillAudioBuffer(float[] buffer, AudioEventType eventType, double parameter)
        {
            if (buffer == null || buffer.Length == 0) return;

            if (MasterVolume <= 0.0f)
            {
                Array.Clear(buffer, 0, buffer.Length);
                return;
            }

            float phase = 0f;
            float phaseIncrement = (float)((2.0 * Math.PI * 440.0) / SampleRate);

            switch (eventType)
            {
                case AudioEventType.SpacetimeHum:
                    double humFreq = GetSpacetimeHumFrequency(parameter);
                    phaseIncrement = (float)((2.0 * Math.PI * humFreq) / SampleRate);
                    break;
                case AudioEventType.SupernovaBlast:
                    double snPitch = GetSupernovaBlastPitch(parameter);
                    phaseIncrement = (float)((2.0 * Math.PI * snPitch) / SampleRate);
                    break;
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                float raw = Mathf.Sin(phase) * 1.5f;
                float clipped = (2.0f / Mathf.PI) * Mathf.Atan(raw) * MasterVolume;
                buffer[i] = Mathf.Clamp(clipped, -1.0f, 1.0f);
                phase += phaseIncrement;
            }
        }
    }

    /// <summary>
    /// Reference implementation of ITrajectoryPredictor that computes osculating orbital elements
    /// and predicts multi-body forward ghost trajectories.
    /// </summary>
    public class HarnessTrajectoryPredictor : ITrajectoryPredictor
    {
        public ConicOrbitElements CalculateOsculatingElements(CelestialBodyState body, CelestialBodyState dominantAttractor, double G)
        {
            Vector3d r = body.Position - dominantAttractor.Position;
            Vector3d v = body.Velocity - dominantAttractor.Velocity;
            double rMag = r.magnitude;
            double vMag = v.magnitude;
            double mu = G * dominantAttractor.Mass;

            double specificEnergy = (0.5 * vMag * vMag) - (mu / rMag);
            Vector3d eVec = CosmoForgeTestHarness.CalculateEccentricityVector(G, dominantAttractor.Mass, r, v);
            double ecc = eVec.magnitude;

            double a = specificEnergy < -1e-9 ? -mu / (2.0 * specificEnergy) : double.PositiveInfinity;
            double rPeri = a * (1.0 - ecc);
            double rApo = ecc < 1.0 ? a * (1.0 + ecc) : double.PositiveInfinity;
            double period = ecc < 1.0 && a > 0 ? 2.0 * Math.PI * Math.Sqrt((a * a * a) / mu) : double.PositiveInfinity;

            ConicType type = ConicType.Elliptical;
            if (ecc < 1e-4) type = ConicType.Circular;
            else if (Math.Abs(ecc - 1.0) < 1e-4) type = ConicType.Parabolic;
            else if (ecc > 1.0) type = ConicType.Hyperbolic;

            return new ConicOrbitElements
            {
                SemiMajorAxis = a,
                Eccentricity = ecc,
                PeriapsisDistance = rPeri,
                ApoapsisDistance = rApo,
                OrbitalPeriod = period,
                SpecificEnergy = specificEnergy,
                Type = type
            };
        }

        public Vector3d[] PredictForwardGhostTrajectory(CelestialBodyState target, List<CelestialBodyState> allBodies, int maxSteps, double stepDeltaTime, double G, double softening)
        {
            var points = new Vector3d[maxSteps];
            Vector3d currentPos = target.Position;
            Vector3d currentVel = target.Velocity;

            for (int s = 0; s < maxSteps; s++)
            {
                Vector3d accel = Vector3d.zero;
                for (int b = 0; b < allBodies.Count; b++)
                {
                    var other = allBodies[b];
                    if (other == target || other.IsDestroyed) continue;
                    Vector3d r = other.Position - currentPos;
                    double r2 = r.sqrMagnitude + (softening * softening);
                    double invDist3 = 1.0 / (r2 * Math.Sqrt(r2));
                    accel += r * (G * other.Mass * invDist3);
                }
                currentVel += accel * stepDeltaTime;
                currentPos += currentVel * stepDeltaTime;
                points[s] = currentPos;
            }

            return points;
        }
    }

    #endregion
}
