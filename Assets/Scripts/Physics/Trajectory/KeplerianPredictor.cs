// ============================================================================
// CosmoForge: Analytical Keplerian Conic Solver & Numerical Ghost Predictor
// Conforms to PROJECT.md § Interface Contracts & Requirements F04, F05
// ============================================================================

namespace CosmoForge.Physics.Trajectory
{
    using System;
    using System.Collections.Generic;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Production implementation of ITrajectoryPredictor.
    /// Provides analytical two-body Keplerian osculating orbit elements and
    /// forward numerical multi-body trajectory prediction up to 500 steps.
    /// </summary>
    public class KeplerianPredictor : ITrajectoryPredictor
    {
        /// <summary>
        /// Calculates analytical osculating Keplerian orbital elements (semi-major axis,
        /// eccentricity, periapsis, apoapsis, orbital period, specific energy, and conic type).
        /// </summary>
        public ConicOrbitElements CalculateOsculatingElements(CelestialBodyState body, CelestialBodyState dominantAttractor, double G)
        {
            if (body == null || dominantAttractor == null || dominantAttractor.Mass <= 0.0)
            {
                return new ConicOrbitElements
                {
                    Type = ConicType.Hyperbolic,
                    SemiMajorAxis = double.PositiveInfinity,
                    Eccentricity = 1.0,
                    OrbitalPeriod = double.PositiveInfinity
                };
            }

            Vector3d r = body.Position - dominantAttractor.Position;
            Vector3d v = body.Velocity - dominantAttractor.Velocity;
            double rMag = r.magnitude;
            double vMag = v.magnitude;
            double mu = G * (dominantAttractor.Mass + body.Mass);

            if (rMag < 1e-9 || mu < 1e-15)
            {
                return new ConicOrbitElements
                {
                    Type = ConicType.Circular,
                    SemiMajorAxis = rMag,
                    Eccentricity = 0.0,
                    PeriapsisDistance = rMag,
                    ApoapsisDistance = rMag,
                    OrbitalPeriod = 0.0,
                    SpecificEnergy = 0.0
                };
            }

            // Specific orbital energy: E = v^2 / 2 - mu / r
            double specificEnergy = (0.5 * vMag * vMag) - (mu / rMag);

            // Specific relative angular momentum vector: h = r x v
            Vector3d h = Vector3d.Cross(r, v);
            double hSqr = h.sqrMagnitude;

            // Eccentricity vector: e = (v x h) / mu - (r / rMag)
            Vector3d vCrossH = Vector3d.Cross(v, h);
            Vector3d eVec = (vCrossH / mu) - (r / rMag);
            double ecc = eVec.magnitude;

            // Semi-major axis: a = -mu / (2 * E)
            double a;
            double period;
            double rPeri;
            double rApo;
            ConicType type;

            if (ecc < 1e-4)
            {
                type = ConicType.Circular;
                a = rMag;
                rPeri = rMag;
                rApo = rMag;
                period = 2.0 * Math.PI * Math.Sqrt((a * a * a) / mu);
            }
            else if (ecc < 1.0 - 1e-4)
            {
                type = ConicType.Elliptical;
                a = -mu / (2.0 * specificEnergy);
                rPeri = a * (1.0 - ecc);
                rApo = a * (1.0 + ecc);
                period = (a > 0.0) ? 2.0 * Math.PI * Math.Sqrt((a * a * a) / mu) : double.PositiveInfinity;
            }
            else if (Math.Abs(ecc - 1.0) <= 1e-4)
            {
                type = ConicType.Parabolic;
                rPeri = hSqr / (2.0 * mu);
                a = double.PositiveInfinity;
                rApo = double.PositiveInfinity;
                period = double.PositiveInfinity;
            }
            else
            {
                type = ConicType.Hyperbolic;
                a = specificEnergy > 1e-9 ? -mu / (2.0 * specificEnergy) : double.PositiveInfinity;
                rPeri = Math.Abs(a) * (ecc - 1.0);
                rApo = double.PositiveInfinity;
                period = double.PositiveInfinity;
            }

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

        /// <summary>
        /// Predicts forward ghost trajectory coordinates (up to maxSteps) for a given target body
        /// under the gravitational influence of all active bodies without mutating live world states.
        /// </summary>
        public Vector3d[] PredictForwardGhostTrajectory(
            CelestialBodyState target,
            List<CelestialBodyState> allBodies,
            int maxSteps,
            double stepDeltaTime,
            double G,
            double softening)
        {
            if (target == null || allBodies == null || maxSteps <= 0 || stepDeltaTime <= 0.0)
            {
                return new Vector3d[0];
            }

            var ghostPath = new Vector3d[maxSteps];
            Vector3d simPos = target.Position;
            Vector3d simVel = target.Velocity;
            double epsSqr = softening * softening;

            for (int s = 0; s < maxSteps; s++)
            {
                Vector3d accel = Vector3d.zero;

                for (int i = 0; i < allBodies.Count; i++)
                {
                    var other = allBodies[i];
                    if (other == null || other.Id == target.Id || other.IsDestroyed) continue;

                    Vector3d r = other.Position - simPos;
                    double r2 = r.sqrMagnitude + epsSqr;
                    double invDist3 = 1.0 / (r2 * Math.Sqrt(r2));
                    accel += r * (G * other.Mass * invDist3);
                }

                // Semi-implicit Euler / Leapfrog step for forward prediction
                simVel += accel * stepDeltaTime;
                simPos += simVel * stepDeltaTime;
                ghostPath[s] = simPos;
            }

            return ghostPath;
        }
    }
}
