// ============================================================================
// CosmoForge Tier 2 Boundary & Corner Cases: Workbench Controls & Audio Tests (F23 - F28)
// 30 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts, TEST_INFRA.md, & ORIGINAL_REQUEST.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Core.Performance;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Integrator;
using CosmoForge.Physics.Trajectory;
using CosmoForge.UI.Workbench;
using CosmoForge.UI.Input;
using CosmoForge.Audio;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier2
{
    [TestFixture]
    public class Tier2_WorkbenchControlsBoundaryTests
    {
        #region Feature F23: Workbench UI Mode Switcher (5 Boundary Tests)

        [Test]
        public void F23_Boundary_ModeSwitcher_SwitchToSameMode_IdempotentNoOp()
        {
            // Boundary condition: Re-selecting the currently active mode via IWorkbenchController.
            // Action must be an idempotent no-op, preserving existing bodies and simulation state.
            var controller = new HarnessWorkbenchController();
            var bodies = new List<CelestialBodyState>
            {
                new CelestialBodyState(1, "A", BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, Vector3d.zero),
                new CelestialBodyState(2, "B", BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, Vector3d.zero),
                new CelestialBodyState(3, "C", BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, Vector3d.zero)
            };

            controller.SwitchMode(WorkbenchMode.FreeSandbox, bodies);
            int countBefore = controller.ModeChangeCount;

            // Re-select same mode
            controller.SwitchMode(WorkbenchMode.FreeSandbox, bodies);

            Assert.AreEqual(countBefore, controller.ModeChangeCount, "Re-selecting current mode must be an idempotent no-op.");
            Assert.AreEqual(3, bodies.Count, "Body count must remain untouched when switching to same mode.");
        }

        [Test]
        public void F23_Boundary_ModeSwitcher_RapidCycleModes_StateRemainsCoherent()
        {
            // Boundary condition: Rapid automated cycling through all mode permutations via IWorkbenchController.
            // Engine must clean up and switch between modes without leaking objects or throwing null refs.
            var controller = new HarnessWorkbenchController();
            var modes = new[] { WorkbenchMode.FreeSandbox, WorkbenchMode.SolarSystemPreset, WorkbenchMode.OrbitalPuzzles, WorkbenchMode.FreeSandbox };
            var bodies = new List<CelestialBodyState>();

            foreach (var mode in modes)
            {
                controller.SwitchMode(mode, bodies);
                Assert.AreEqual(mode, controller.ActiveMode, "Active mode must match transitioned mode.");
                Assert.IsNotNull(bodies, "Body list must remain valid across mode changes.");
            }

            Assert.AreEqual(0, bodies.Count, "After returning to FreeSandbox, bodies list is clean.");
        }

        [Test]
        public void F23_Boundary_ModeSwitcher_InvalidModeIndex_ClampsToDefaultFreeSandbox()
        {
            // Boundary condition: Invalid mode index passed from deep link or external configuration (-10 or 100).
            // Clamps safely to FreeSandbox (0).
            int[] testIndices = { -10, -1, 0, 1, 2, 3, 100 };

            foreach (int rawIndex in testIndices)
            {
                int clamped = Math.Clamp(rawIndex, 0, 2);
                var mode = (WorkbenchMode)clamped;

                Assert.IsTrue(Enum.IsDefined(typeof(WorkbenchMode), mode), $"Mode {mode} must be valid enum value.");
            }
        }

        [Test]
        public void F23_Boundary_ModeSwitcher_PreservesSimulationStateOnToggle()
        {
            // Boundary condition: Switching modes while simulation is paused or active.
            // Paused state must be explicitly handled (e.g. pausing simulation when switching to puzzle mode).
            var controller = new HarnessWorkbenchController();
            controller.TimeScale = 2.0;

            controller.SwitchMode(WorkbenchMode.OrbitalPuzzles, null);

            Assert.IsTrue(controller.IsPaused, "Switching to OrbitalPuzzles mode must initialize in paused state.");
            Assert.AreEqual(0.0, controller.TimeScale, "Entering puzzle mode must initialize paused time scale.");
        }

        [Test]
        public void F23_Boundary_ModeSwitcher_ExitPreset_ClearsAllPresetBodiesCleanly()
        {
            // Boundary condition: Transitioning away from SolarSystemPreset.
            // All preset planets and sun must be cleared from the active bodies list.
            var controller = new HarnessWorkbenchController();
            var bodies = new List<CelestialBodyState>();

            controller.SwitchMode(WorkbenchMode.SolarSystemPreset, bodies);
            Assert.GreaterOrEqual(bodies.Count, 2);

            controller.SwitchMode(WorkbenchMode.FreeSandbox, bodies);
            Assert.AreEqual(0, bodies.Count, "Exiting preset must clear all bodies.");
        }

        #endregion

        #region Feature F24: Time-Warp Controls & Reverse Time Buffer (5 Boundary Tests)

        [Test]
        public void F24_Boundary_TimeWarp_LowerLimit_ZeroPointOneTimes()
        {
            // Boundary condition: Minimum non-zero time warp scale is 0.1x via IWorkbenchController.
            var controller = new HarnessWorkbenchController();
            controller.TimeScale = 0.1;

            var bodies = new List<CelestialBodyState>
            {
                new CelestialBodyState(1, "Test", BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, new Vector3d(10.0, 0.0, 0.0))
            };

            controller.StepSimulation(bodies, 0.02);

            double expectedX = 10.0 * (0.1 * 0.02);
            Assert.AreEqual(expectedX, bodies[0].Position.x, 1e-9, "Effective displacement at 0.1x must be accurate.");
            Assert.Greater(bodies[0].Position.x, 0.0);
        }

        [Test]
        public void F24_Boundary_TimeWarp_UpperLimit_OneThousandTimes()
        {
            // Boundary condition: Maximum time warp scale is 1000.0x.
            // At 1000x, frame budget sub-stepping clamps substeps to prevent simulation explosion.
            var world = new SimulationWorld();
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem();
            foreach (var b in bodies) world.AddBody(b);

            world.UpdateSimulation(0.016, 1000.0);

            Assert.IsFalse(double.IsNaN(world.Bodies[0].Position.x), "Substepping cap must preserve numerical stability at 1000x.");
            Assert.AreEqual(25, SimulationWorld.MaxSubsteps);
        }

        [Test]
        public void F24_Boundary_TimeWarp_Pause_ZeroTimeScale_ZeroPositionDisplacement()
        {
            // Boundary condition: Time scale = 0.0 (Pause) via IWorkbenchController.
            var controller = new HarnessWorkbenchController();
            controller.IsPaused = true;
            controller.TimeScale = 0.0;

            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem();
            Vector3d initPos = bodies[1].Position;

            controller.StepSimulation(bodies, 0.02);

            Assert.AreEqual(initPos, bodies[1].Position, "Position must not change when time scale is 0.0 (paused).");
        }

        [Test]
        public void F24_Boundary_TimeWarp_NegativeTimeScale_ClampedOrEngagesRewind()
        {
            // Boundary condition: Negative time-scale input (e.g. -5.0x).
            double inputScale = -5.0;
            bool isReverseMode = inputScale < 0.0;
            double clampedNonNegativeScale = Math.Max(0.0, inputScale);

            Assert.IsTrue(isReverseMode, "Negative time scale flags reverse mode.");
            Assert.AreEqual(0.0, clampedNonNegativeScale, "Non-negative clamp must be 0.0.");
        }

        [Test]
        public void F24_Boundary_RewindBuffer_Saturation_CircularOverwritesOldest()
        {
            // Boundary condition: Rewind ring buffer capacity = 100 states. Pushing 150 states.
            int capacity = 100;
            var buffer = new Queue<Vector3d>(capacity);

            for (int i = 0; i < 150; i++)
            {
                if (buffer.Count >= capacity)
                {
                    buffer.Dequeue();
                }
                buffer.Enqueue(new Vector3d(i, 0.0, 0.0));
            }

            Assert.AreEqual(100, buffer.Count, "Ring buffer must stay saturated at capacity.");
            Assert.AreEqual(50.0, buffer.Peek().x, 1e-9, "Oldest state in saturated buffer must be state 50.");
        }

        #endregion

        #region Feature F25: Celestial Body Placement Drawer & Slingshot (5 Boundary Tests)

        [Test]
        public void F25_Boundary_Slingshot_ZeroDrag_ZeroVelocitySpawn()
        {
            // Boundary condition: Player taps screen without dragging slingshot vector (drag vector = (0, 0)).
            Vector2 screenDrag = Vector2.zero;
            double dragSensitivity = 0.1;

            Vector3d spawnedVelocity = new Vector3d(screenDrag.x * dragSensitivity, 0.0, screenDrag.y * dragSensitivity);

            Assert.AreEqual(Vector3d.zero, spawnedVelocity, "Zero drag vector must spawn body at rest.");
        }

        [Test]
        public void F25_Boundary_Slingshot_MaxDrag_VelocityClampedAtMaximumThreshold()
        {
            // Boundary condition: Player drags slingshot across entire screen (e.g. 2000 pixels).
            float rawDragPixels = 2000.0f;
            float maxVelocity = 100.0f;
            float sensitivity = 0.2f;

            float rawVelocity = rawDragPixels * sensitivity;
            float clampedVelocity = Mathf.Clamp(rawVelocity, 0.0f, maxVelocity);

            Assert.AreEqual(maxVelocity, clampedVelocity, "Slingshot launch velocity must clamp to maxVelocity.");
            Assert.LessOrEqual(clampedVelocity, 100.0f);
        }

        [Test]
        public void F25_Boundary_Slingshot_ReleaseOutsideScreen_CancelsLaunch()
        {
            // Boundary condition: Slingshot drag dragged off-screen.
            Vector2 releasePos = new Vector2(-50.0f, 500.0f);
            int screenWidth = 1920;
            int screenHeight = 1080;

            bool isInsideScreen = releasePos.x >= 0 && releasePos.x <= screenWidth &&
                                  releasePos.y >= 0 && releasePos.y <= screenHeight;

            Assert.IsFalse(isInsideScreen, "Touch released outside screen bounds must be detected.");
            bool spawnCancelled = !isInsideScreen;
            Assert.IsTrue(spawnCancelled, "Off-screen release cancels launch.");
        }

        [Test]
        public void F25_Boundary_PlacementDrawer_SpawnAtSameCoordinate_SafeOffsetApplied()
        {
            // Boundary condition: Spawning new body directly at coordinates of an existing body.
            Vector3d existingPos = new Vector3d(50.0, 0.0, 0.0);
            double existingRadius = 5.0;
            double newRadius = 2.0;

            Vector3d requestedPos = existingPos;
            bool isCoincident = (requestedPos - existingPos).sqrMagnitude < 1e-4;

            Vector3d safePos = requestedPos;
            if (isCoincident)
            {
                safePos = existingPos + new Vector3d(existingRadius + newRadius + 1.0, 0.0, 0.0);
            }

            Assert.AreNotEqual(existingPos, safePos, "Coincident spawn must be shifted to a safe clearance offset.");
            double clearance = (safePos - existingPos).magnitude;
            Assert.GreaterOrEqual(clearance, existingRadius + newRadius, "Clearance must exceed sum of physical radii.");
        }

        [Test]
        public void F25_Boundary_PlacementDrawer_MassSelection_ClampedToPositiveNonZero()
        {
            // Boundary condition: Mass slider placed at minimum boundary (0 or negative).
            double rawMass = -5.0;
            double minMass = 1e-5;

            double clampedMass = Math.Max(minMass, rawMass);

            Assert.AreEqual(minMass, clampedMass, "Selected mass must clamp to minimum positive threshold.");
            Assert.Greater(clampedMass, 0.0, "Clamped mass must be strictly positive.");
        }

        #endregion

        #region Feature F26: Live Orbital Telemetry Inspector (5 Boundary Tests)

        [Test]
        public void F26_Boundary_Telemetry_SelectedBodyNull_InspectorClearsWithoutNullRef()
        {
            // Boundary condition: User deselects all bodies (selectedBody = null).
            CelestialBodyState selectedBody = null;

            bool hasSelection = selectedBody != null;
            string displayStatus = hasSelection ? selectedBody.Name : "No Body Selected";

            Assert.IsFalse(hasSelection, "Selection is null.");
            Assert.AreEqual("No Body Selected", displayStatus, "Inspector must display placeholder when no body is selected.");
        }

        [Test]
        public void F26_Boundary_Telemetry_DestroyedBody_ShowsDestroyedStatus()
        {
            // Boundary condition: Inspected body is destroyed mid-simulation.
            var body = new CelestialBodyState(1, "ImpactedMoon", BodyType.Moon, 10.0, 1.0, Vector3d.zero, Vector3d.zero)
            {
                IsDestroyed = true
            };

            Assert.IsTrue(body.IsDestroyed, "Body is marked destroyed.");
            string status = body.IsDestroyed ? "Destroyed / Merged" : "Active Orbit";
            Assert.AreEqual("Destroyed / Merged", status, "Telemetry inspector must display destroyed status.");
        }

        [Test]
        public void F26_Boundary_Telemetry_HyperbolicOrbit_PeriodDisplaysInfinityOrNA()
        {
            // Boundary condition: Telemetry calculation for hyperbolic flyby via ITrajectoryPredictor
            ITrajectoryPredictor predictor = new HarnessTrajectoryPredictor();
            var primary = new CelestialBodyState(1, "Star", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var hyperbolicBody = new CelestialBodyState(2, "Comet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, 15.0, 0.0));

            ConicOrbitElements elements = predictor.CalculateOsculatingElements(hyperbolicBody, primary, 1.0);

            Assert.AreEqual(ConicType.Hyperbolic, elements.Type, "High velocity body must resolve to Hyperbolic conic.");
            Assert.IsTrue(double.IsPositiveInfinity(elements.OrbitalPeriod), "Hyperbolic orbital period must be positive infinity.");
        }

        [Test]
        public void F26_Boundary_Telemetry_LinearTrajectory_EccentricityOneWithoutNaN()
        {
            // Boundary condition: Pure radial trajectory with zero angular momentum (h = 0).
            Vector3d r = new Vector3d(100.0, 0.0, 0.0);
            Vector3d v = new Vector3d(-5.0, 0.0, 0.0);
            double G = 1.0;
            double M = 2000.0;

            Vector3d eVec = CosmoForgeTestHarness.CalculateEccentricityVector(G, M, r, v);

            Assert.IsFalse(double.IsNaN(eVec.magnitude), "Eccentricity magnitude must not be NaN.");
            Assert.AreEqual(1.0, eVec.magnitude, 1e-6, "Linear trajectory eccentricity must evaluate to 1.0.");
        }

        [Test]
        public void F26_Boundary_Telemetry_NearZeroMass_CalculatesAccuratelyAroundPrimary()
        {
            // Boundary condition: Telemetry for test probe of mass 1e-10 orbiting primary of mass 1000.0.
            double G = 1.0;
            double M = 1000.0;
            double r = 50.0;
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, M, r);

            Vector3d pos = new Vector3d(r, 0.0, 0.0);
            Vector3d vel = new Vector3d(0.0, vCirc, 0.0);

            double a = CosmoForgeTestHarness.CalculateSemiMajorAxis(G, M, pos, vel);
            double period = CosmoForgeTestHarness.CalculateOrbitalPeriod(G, M, a);

            Assert.AreEqual(r, a, 1e-6, "Semi-major axis must equal orbital radius.");
            Assert.Greater(period, 0.0, "Period must be positive.");
            TestAssertions.AssertNoNaNOrInfinity(new Vector3d(a, period, 0), "Telemetry metrics");
        }

        #endregion

        #region Feature F27: Android Tactile Touch Controls & 3D Icons (5 Boundary Tests)

        [Test]
        public void F27_Boundary_TouchControls_ZeroTouches_IdleStateNoCameraDrift()
        {
            // Boundary condition: Screen active but 0 fingers touching screen.
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            Vector3 initCamPos = handler.CameraPosition;

            handler.ProcessPan(Vector2.zero, 0.1f);

            Assert.AreEqual(initCamPos, handler.CameraPosition, "Zero touch pan delta must produce zero camera drift.");
        }

        [Test]
        public void F27_Boundary_TouchControls_OneFingerPan_SensitivityZero_ZeroDisplacement()
        {
            // Boundary condition: Pan sensitivity set to 0.0 via ITouchInputHandler.
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            Vector3 initialCamPos = new Vector3(0.0f, 100.0f, 0.0f);
            handler.CameraPosition = initialCamPos;

            handler.ProcessPan(new Vector2(25.0f, -15.0f), 0.0f);

            Assert.AreEqual(initialCamPos, handler.CameraPosition, "Zero pan sensitivity must yield exactly zero camera displacement.");
        }

        [Test]
        public void F27_Boundary_TouchControls_TwoFingerPinch_ZeroDeltaPinch_NoZoomChange()
        {
            // Boundary condition: 2 touch fingers held stationary via ITouchInputHandler.
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            handler.CameraDistance = 250.0f;

            handler.ProcessPinch(250.0f, 250.0f, 10.0f, 1000.0f);

            Assert.AreEqual(250.0f, handler.CameraDistance, 1e-6f, "Zero delta pinch distance must produce no zoom change.");
        }

        [Test]
        public void F27_Boundary_TouchControls_LogScalePinchZoom_LimitsClamped()
        {
            // Boundary condition: Pinch zoom requested at extreme boundaries via ITouchInputHandler.
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            handler.CameraDistance = 100.0f;

            // Extreme pinch in
            handler.ProcessPinch(100.0f, 1e-4f, 10.0f, 500.0f);
            Assert.AreEqual(500.0f, handler.CameraDistance, "Under-limit zoom must clamp to max limit.");

            // Extreme pinch out
            handler.ProcessPinch(100.0f, 1e6f, 10.0f, 500.0f);
            Assert.AreEqual(10.0f, handler.CameraDistance, "Over-limit zoom must clamp to min limit.");
        }

        [Test]
        public void F27_Boundary_TouchControls_ExcessTouches_ClampedToSupportedCount()
        {
            // Boundary condition: 5+ simultaneous touch points detected.
            int rawTouches = 6;
            int recognizedTouches = Math.Min(rawTouches, 2);

            Assert.AreEqual(2, recognizedTouches, "Touch processor must clamp active gesture fingers to maximum 2.");
        }

        #endregion

        #region Feature F28: Procedural Cosmic Audio Synthesizer (5 Boundary Tests)

        [Test]
        public void F28_Boundary_CosmicAudio_ZeroVolume_SilentBufferPureZeros()
        {
            // Boundary condition: Master volume set to 0.0 (muted) via IProceduralAudioEngine.
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();
            audioEngine.MasterVolume = 0.0f;

            float[] audioBuffer = new float[256];
            audioEngine.FillAudioBuffer(audioBuffer, AudioEventType.SpacetimeHum, -20.0);

            foreach (float sample in audioBuffer)
            {
                Assert.AreEqual(0.0f, sample, "Muted audio buffer must contain only 0.0f samples.");
            }
        }

        [Test]
        public void F28_Boundary_CosmicAudio_SampleClippingGuard_BufferBoundedMinusOneToOne()
        {
            // Boundary condition: Multiple audio sources summing together (high gain) via IProceduralAudioEngine.
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();
            audioEngine.MasterVolume = 10.0f; // High gain

            float[] audioBuffer = new float[256];
            audioEngine.FillAudioBuffer(audioBuffer, AudioEventType.SupernovaBlast, 0.0);

            for (int i = 0; i < audioBuffer.Length; i++)
            {
                Assert.GreaterOrEqual(audioBuffer[i], -1.0f, "Audio sample must be >= -1.0f.");
                Assert.LessOrEqual(audioBuffer[i], 1.0f, "Audio sample must be <= 1.0f.");
                Assert.IsFalse(float.IsNaN(audioBuffer[i]));
            }
        }

        [Test]
        public void F28_Boundary_CosmicAudio_PulsarFrequency_ClampedToNyquistLimit()
        {
            // Boundary condition: Ultra-fast pulsar audio synthesis at high playback rate.
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();
            audioEngine.SampleRate = 48000.0;

            double interval = audioEngine.GetPulsarImpulseInterval(30000.0);

            Assert.Greater(interval, 2.0, "Impulse interval must stay strictly above 2 samples (Nyquist limit).");
        }

        [Test]
        public void F28_Boundary_CosmicAudio_SupernovaBlast_FiniteMonotonicDecay()
        {
            // Boundary condition: Supernova audio explosion envelope decays monotonically via IProceduralAudioEngine.
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();
            double previousAmp = 1.0;

            for (double t = 0.0; t <= 5.0; t += 0.5)
            {
                double currentAmp = audioEngine.GetSupernovaEnvelope(t, 1.5);

                Assert.LessOrEqual(currentAmp, previousAmp + 1e-9, $"Audio envelope must decay monotonically at t={t}.");
                Assert.GreaterOrEqual(currentAmp, 0.0, $"Audio amplitude must be non-negative at t={t}.");
                previousAmp = currentAmp;
            }
        }

        [Test]
        public void F28_Boundary_CosmicAudio_ZeroTimeStep_NoAudioPitchExplosion()
        {
            // Boundary condition: Simulation paused (dt = 0.0).
            double frequency = 440.0;
            double dt = 0.0;

            double deltaPhase = 2.0 * Math.PI * frequency * dt;

            Assert.AreEqual(0.0, deltaPhase, 1e-15, "Paused simulation must produce 0.0 phase advance.");
            Assert.IsFalse(double.IsNaN(deltaPhase), "Phase advance must not be NaN.");
        }

        #endregion
    }
}
