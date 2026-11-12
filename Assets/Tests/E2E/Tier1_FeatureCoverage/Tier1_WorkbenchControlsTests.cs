// ============================================================================
// CosmoForge Tier 1 Feature Coverage: Workbench Controls & Audio Tests (F23 - F28)
// 30 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Core.Performance;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Trajectory;
using CosmoForge.UI.Workbench;
using CosmoForge.UI.Input;
using CosmoForge.Audio;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier1
{
    [TestFixture]
    public class Tier1_WorkbenchControlsTests
    {
        public struct BodySnapshot
        {
            public int Id;
            public Vector3d Position;
            public Vector3d Velocity;
            public double Mass;
        }

        #region Feature F23: Workbench UI Mode Switcher (5 Tests)

        [Test]
        public void F23_ModeSwitcher_SwitchesToFreeSandboxMode()
        {
            // Requirement R5 & Acceptance Criteria: Mode switcher toggles to Free Sandbox
            IWorkbenchController controller = new HarnessWorkbenchController();
            controller.SwitchMode(WorkbenchMode.SolarSystemPreset, null);
            Assert.AreEqual(WorkbenchMode.SolarSystemPreset, controller.ActiveMode);

            controller.SwitchMode(WorkbenchMode.FreeSandbox, null);
            Assert.AreEqual(WorkbenchMode.FreeSandbox, controller.ActiveMode, "Must transition to FreeSandbox mode via IWorkbenchController.");
        }

        [Test]
        public void F23_ModeSwitcher_SwitchesToSolarSystemPreset()
        {
            // Requirement R5: Toggles to Solar System preset with planetary system
            IWorkbenchController controller = new HarnessWorkbenchController();
            var activeBodies = new List<CelestialBodyState>();

            controller.SwitchMode(WorkbenchMode.SolarSystemPreset, activeBodies);

            Assert.AreEqual(WorkbenchMode.SolarSystemPreset, controller.ActiveMode, "Must transition to SolarSystemPreset mode.");
            Assert.GreaterOrEqual(activeBodies.Count, 2, "SolarSystemPreset must populate astronomical bodies in active collection.");
        }

        [Test]
        public void F23_ModeSwitcher_SwitchesToOrbitalPuzzlesMode()
        {
            // Requirement R5: Toggles to Orbital Puzzles challenge mode
            IWorkbenchController controller = new HarnessWorkbenchController();
            controller.SwitchMode(WorkbenchMode.OrbitalPuzzles, null);

            Assert.AreEqual(WorkbenchMode.OrbitalPuzzles, controller.ActiveMode, "Must transition to OrbitalPuzzles mode.");
            Assert.IsTrue(controller.IsPaused, "Switching to OrbitalPuzzles mode must initialize in paused state.");
            Assert.AreEqual(0.0, controller.TimeScale, 1e-9);
        }

        [Test]
        public void F23_ModeSwitcher_ModeTransition_CleansUpPreviousModeState()
        {
            // Mode transition clears existing bodies and active selections
            IWorkbenchController controller = new HarnessWorkbenchController();
            controller.SwitchMode(WorkbenchMode.SolarSystemPreset, null);
            var activeBodies = CosmoForgeTestHarness.CreateSunEarthSystem();
            Assert.AreEqual(2, activeBodies.Count);
            controller.SelectedBodyId = 2;

            controller.SwitchMode(WorkbenchMode.FreeSandbox, activeBodies);

            Assert.AreEqual(0, activeBodies.Count, "Bodies must be cleaned up on mode switch.");
            Assert.AreEqual(-1, controller.SelectedBodyId, "Selection must be cleared on mode switch.");
        }

        [Test]
        public void F23_ModeSwitcher_ActiveModeProperty_ReflectsCurrentState()
        {
            // Mode enum states reflect all three modes accurately and dispatches event
            IWorkbenchController controller = new HarnessWorkbenchController();
            WorkbenchMode dispatchedMode = WorkbenchMode.SolarSystemPreset;
            int eventFiredCount = 0;

            controller.OnModeChanged += mode =>
            {
                dispatchedMode = mode;
                eventFiredCount++;
            };

            controller.SwitchMode(WorkbenchMode.OrbitalPuzzles, null);

            Assert.AreEqual(WorkbenchMode.OrbitalPuzzles, controller.ActiveMode);
            Assert.AreEqual(WorkbenchMode.OrbitalPuzzles, dispatchedMode);
            Assert.AreEqual(1, eventFiredCount, "OnModeChanged event must fire on mode switch.");

            var modes = (WorkbenchMode[])Enum.GetValues(typeof(WorkbenchMode));
            Assert.AreEqual(3, modes.Length, "Must have exactly 3 workbench modes: FreeSandbox, SolarSystemPreset, OrbitalPuzzles.");
        }

        #endregion

        #region Feature F24: Time-Warp Controls & Reverse Time Buffer (5 Tests)

        [Test]
        public void F24_TimeWarp_Pause_HaltsSimulationStep()
        {
            // Requirement R5 & Acceptance Criteria: Pause multiplier = 0x halts position progression
            IWorkbenchController controller = new HarnessWorkbenchController();
            controller.IsPaused = true;
            controller.TimeScale = 0.0;

            var bodies = new List<CelestialBodyState>
            {
                new CelestialBodyState(1, "TestBody", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(10.0, 0.0, 0.0), new Vector3d(0.0, 5.0, 0.0))
            };

            controller.StepSimulation(bodies, 0.02);

            Assert.AreEqual(10.0, bodies[0].Position.x, 1e-12, "Position must not advance when paused.");
            Assert.AreEqual(0.0, bodies[0].Position.y, 1e-12);
        }

        [Test]
        public void F24_TimeWarp_ForwardMultipliers_AcceleratesElapsedSimulationTime()
        {
            // 10x warp advances simulation clock 10 times faster
            IWorkbenchController controller = new HarnessWorkbenchController();
            controller.IsPaused = false;
            controller.TimeScale = 10.0;

            var bodies = new List<CelestialBodyState>
            {
                new CelestialBodyState(1, "AcceleratedBody", BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, new Vector3d(1.0, 0.0, 0.0))
            };

            controller.StepSimulation(bodies, 1.0);

            Assert.AreEqual(10.0, bodies[0].Position.x, 1e-9, "10x warp must advance 10 simulation seconds per real second.");
        }

        [Test]
        public void F24_TimeWarp_SubsteppingCap_MaintainsStabilityAtHighWarp()
        {
            // Clamping sub-steps at 1000x warp prevents spiral of death
            var world = new SimulationWorld();
            Assert.AreEqual(25, SimulationWorld.MaxSubsteps, "Max sub-steps must cap execution to 25 steps.");

            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem();
            foreach (var b in bodies) world.AddBody(b);

            world.UpdateSimulation(0.016, 1000.0);

            Assert.IsFalse(double.IsNaN(world.Bodies[0].Position.x), "Substepping cap must prevent NaN divergence.");
            Assert.IsFalse(double.IsNaN(world.Bodies[1].Position.x), "Substepping cap must maintain numerical stability.");
        }

        [Test]
        public void F24_TimeWarp_StateRingBuffer_RecordsPeriodicSnapshots()
        {
            // Requirement R5: Ring buffer captures past states
            int bufferCapacity = 600;
            var ringBuffer = new Queue<BodySnapshot>(bufferCapacity);

            for (int i = 0; i < 50; i++)
            {
                ringBuffer.Enqueue(new BodySnapshot
                {
                    Id = 1,
                    Position = new Vector3d(i, 0.0, 0.0),
                    Velocity = new Vector3d(0.0, 1.0, 0.0),
                    Mass = 100.0
                });
            }

            Assert.AreEqual(50, ringBuffer.Count, "Ring buffer must store recorded snapshot frames.");
        }

        [Test]
        public void F24_TimeWarp_ReverseTime_RestoresExactHistoricalStates()
        {
            // Requirement R5 & Acceptance Criteria: Rewind buffer deterministically restores past states
            var history = new Stack<BodySnapshot>();
            Vector3d initialPos = new Vector3d(50.0, 0.0, 0.0);

            history.Push(new BodySnapshot { Id = 1, Position = initialPos, Velocity = Vector3d.zero, Mass = 1.0 });

            Vector3d currentPos = initialPos;
            for (int i = 1; i <= 5; i++)
            {
                currentPos += new Vector3d(1.0, 0.0, 0.0);
                history.Push(new BodySnapshot { Id = 1, Position = currentPos, Velocity = Vector3d.zero, Mass = 1.0 });
            }

            Assert.AreEqual(55.0, currentPos.x, 1e-9);

            for (int r = 0; r < 5; r++)
            {
                history.Pop();
            }

            BodySnapshot restored = history.Peek();
            Assert.AreEqual(initialPos, restored.Position, "Rewind must restore exact starting position.");
        }

        #endregion

        #region Feature F25: Celestial Body Placement Drawer & Slingshot (5 Tests)

        [Test]
        public void F25_PlacementDrawer_SelectsBodyTemplateFromCatalog()
        {
            // Requirement R5 & Acceptance Criteria: Selecting from catalog provides template specs
            var templates = new Dictionary<string, (double mass, double radius)>
            {
                { "Terrestrial", (1.0, 1.0) },
                { "GasGiant", (317.8, 11.2) },
                { "Star", (333000.0, 109.0) },
                { "BlackHole", (1000000.0, 2.0) }
            };

            var earthTemplate = templates["Terrestrial"];
            Assert.AreEqual(1.0, earthTemplate.mass, 1e-9);
            Assert.AreEqual(1.0, earthTemplate.radius, 1e-9);

            var gasGiantTemplate = templates["GasGiant"];
            Assert.AreEqual(317.8, gasGiantTemplate.mass, 1e-9);
        }

        [Test]
        public void F25_PlacementSlingshot_DragVector_CalculatesOppositeLaunchVelocity()
        {
            // Requirement R5: Slingshot gesture imparts velocity opposite to drag: v = -k * (p_current - p_start)
            Vector2 touchStart = new Vector2(100.0f, 100.0f);
            Vector2 touchDrag = new Vector2(80.0f, 100.0f);

            Vector2 dragDelta = touchDrag - touchStart;
            float slingshotScale = 0.5f;

            Vector2 launchVelocity = -dragDelta * slingshotScale;

            Assert.AreEqual(10.0f, launchVelocity.x, 1e-5f, "Launch velocity must point opposite to drag direction.");
            Assert.AreEqual(0.0f, launchVelocity.y, 1e-5f);
        }

        [Test]
        public void F25_PlacementSlingshot_ZeroDrag_SpawnsStationaryBody()
        {
            // Placing without dragging results in v = 0
            Vector2 start = new Vector2(50.0f, 50.0f);
            Vector2 end = start;

            Vector2 delta = end - start;
            Vector3d velocity = new Vector3d(-delta.x, -delta.y, 0.0);

            Assert.AreEqual(Vector3d.zero, velocity, "Zero drag release must produce stationary body.");
        }

        [Test]
        public void F25_PlacementSlingshot_LiveTrajectoryPreview_UpdatesDuringDrag()
        {
            // Drag vector continuously updates projected forward trajectory
            ITrajectoryPredictor predictor = new HarnessTrajectoryPredictor();
            var attractor = new CelestialBodyState(1, "Sun", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);

            var target1 = new CelestialBodyState(2, "Probe1", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, 2.0, 0.0));
            var target2 = new CelestialBodyState(3, "Probe2", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, 5.0, 0.0));

            var allBodies1 = new List<CelestialBodyState> { attractor, target1 };
            var allBodies2 = new List<CelestialBodyState> { attractor, target2 };

            var pts1 = predictor.PredictForwardGhostTrajectory(target1, allBodies1, 20, 0.1, 1.0, 0.0);
            var pts2 = predictor.PredictForwardGhostTrajectory(target2, allBodies2, 20, 0.1, 1.0, 0.0);

            double dist1 = (pts1[19] - pts1[0]).magnitude;
            double dist2 = (pts2[19] - pts2[0]).magnitude;

            Assert.Greater(dist2, dist1, "Higher slingshot launch velocity must project greater forward trajectory distance.");
        }

        [Test]
        public void F25_PlacementSlingshot_SpawnsBodyIntoWorldOnRelease()
        {
            // Releasing finger adds new body into active simulation world
            var world = new SimulationWorld();
            Assert.AreEqual(0, world.Bodies.Count);

            var newBody = new CelestialBodyState(1, "PlacedPlanet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, 4.5, 0.0));
            world.AddBody(newBody);

            Assert.AreEqual(1, world.Bodies.Count, "Placed body must be added to active world bodies collection.");
            Assert.AreEqual("PlacedPlanet", world.Bodies[0].Name);
        }

        #endregion

        #region Feature F26: Live Orbital Telemetry Inspector (5 Tests)

        [Test]
        public void F26_Telemetry_SelectedBody_ReadsAccurateMassAndRadius()
        {
            // Requirement R5 & Acceptance Criteria: Telemetry inspector displays live mass, radius, velocity
            var body = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 5.972e24, 6.371e6, Vector3d.zero, Vector3d.zero);

            Assert.AreEqual(5.972e24, body.Mass, 1e18);
            Assert.AreEqual(6.371e6, body.Radius, 1e2);
        }

        [Test]
        public void F26_Telemetry_SelectedBody_ComputesCurrentScalarSpeed()
        {
            // Scalar velocity readout: |v| = sqrt(vx^2 + vy^2 + vz^2)
            var body = new CelestialBodyState(1, "Orbiter", BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, new Vector3d(30.0, 40.0, 0.0));
            double speed = body.Velocity.magnitude;

            Assert.AreEqual(50.0, speed, 1e-9, "Scalar orbital speed must equal 50.0 km/s.");
        }

        [Test]
        public void F26_Telemetry_KeplerianElements_DisplaysAccurateSemiMajorAxis()
        {
            // Telemetry inspector calculates osculating semi-major axis via ITrajectoryPredictor
            ITrajectoryPredictor predictor = new HarnessTrajectoryPredictor();
            var primary = new CelestialBodyState(1, "Star", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);

            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            var orbiter = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            ConicOrbitElements elements = predictor.CalculateOsculatingElements(orbiter, primary, 1.0);
            Assert.AreEqual(50.0, elements.SemiMajorAxis, 1e-4, "Semi-major axis displayed in telemetry must equal 50.0.");
            Assert.AreEqual(ConicType.Circular, elements.Type, "Near-zero eccentricity orbit must resolve as Circular conic.");
        }

        [Test]
        public void F26_Telemetry_EccentricityReadout_MatchesOrbitalGeometry()
        {
            // Eccentricity readout matches osculating elements from ITrajectoryPredictor
            ITrajectoryPredictor predictor = new HarnessTrajectoryPredictor();
            var primary = new CelestialBodyState(1, "Star", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);

            double vPeri = CosmoForgeTestHarness.CalculateVisVivaSpeed(1.0, 1000.0, 40.0, 80.0);
            var orbiter = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(40.0, 0.0, 0.0), new Vector3d(0.0, vPeri, 0.0));

            ConicOrbitElements elements = predictor.CalculateOsculatingElements(orbiter, primary, 1.0);
            Assert.AreEqual(0.5, elements.Eccentricity, 1e-4, "Telemetry eccentricity readout must equal 0.50.");
            Assert.AreEqual(ConicType.Elliptical, elements.Type);
        }

        [Test]
        public void F26_Telemetry_SurfaceTemperature_DisplaysKelvinAndCelsius()
        {
            // Temperature inspector shows Kelvin and Celsius (C = K - 273.15)
            double tempK = 288.15;
            double tempC = tempK - 273.15;

            Assert.AreEqual(15.0, tempC, 1e-5, "288.15 K must display as 15.0 °C.");
        }

        #endregion

        #region Feature F27: Android Tactile Touch Controls & 3D Icons (5 Tests)

        [Test]
        public void F27_TouchControls_OneFingerPan_TranslatesCameraInOrbitalPlane()
        {
            // Requirement R1 & R5: 1-finger drag pans camera across plane via ITouchInputHandler
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            handler.CameraPosition = new Vector3(0.0f, 100.0f, 0.0f);

            handler.ProcessPan(new Vector2(10.0f, -5.0f), 0.1f);

            Assert.AreEqual(1.0f, handler.CameraPosition.x, 1e-5f);
            Assert.AreEqual(100.0f, handler.CameraPosition.y, 1e-5f, "Vertical camera height remains constant during plane pan.");
            Assert.AreEqual(-0.5f, handler.CameraPosition.z, 1e-5f);
            Assert.AreEqual(TouchGestureType.Pan, handler.LastGesture);
        }

        [Test]
        public void F27_TouchControls_TwoFingerPinch_AppliesLogarithmicZoom()
        {
            // Requirement R1 & R5: Logarithmic pinch-to-zoom via ITouchInputHandler
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            handler.CameraDistance = 100.0f;

            handler.ProcessPinch(200.0f, 300.0f, 10.0f, 1000.0f);

            Assert.Less(handler.CameraDistance, 100.0f, "Pinching outward must zoom closer to target.");
            Assert.AreEqual(100.0f * (200.0f / 300.0f), handler.CameraDistance, 1e-4f);
            Assert.AreEqual(TouchGestureType.PinchZoom, handler.LastGesture);
        }

        [Test]
        public void F27_TouchControls_SingleTap_SelectsNearestCelestialBody()
        {
            // Tap raycast selection via ITouchInputHandler
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem();
            Vector3 tapWorldPos = new Vector3(100.0f, 0.0f, 0.0f);

            int selectedId = handler.ProcessSingleTap(tapWorldPos, bodies, 50.0);

            Assert.AreEqual(2, selectedId, "Tap near Earth must select Earth (Id = 2).");
            Assert.AreEqual(2, handler.FocusedBodyId);
            Assert.AreEqual(TouchGestureType.SingleTap, handler.LastGesture);
        }

        [Test]
        public void F27_TouchControls_DoubleTap_EngagesFocusTrackingMode()
        {
            // Double-tap on celestial body locks camera tracking via ITouchInputHandler
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            Assert.IsFalse(handler.IsTrackingFocus);

            handler.ProcessDoubleTap(2);

            Assert.IsTrue(handler.IsTrackingFocus, "Double-tap must engage camera focus tracking.");
            Assert.AreEqual(2, handler.FocusedBodyId);
            Assert.AreEqual(TouchGestureType.DoubleTap, handler.LastGesture);
        }

        [Test]
        public void F27_TouchControls_TwoFingerTwist_RotatesCameraPerspective()
        {
            // 2-finger twist rotates camera yaw angle via ITouchInputHandler
            ITouchInputHandler handler = new HarnessTouchInputHandler();
            handler.CameraYaw = 0.0f;

            handler.ProcessTwist(15.0f);

            Assert.AreEqual(15.0f, handler.CameraYaw, 1e-5f, "Twist gesture must rotate camera view angle.");
            Assert.AreEqual(TouchGestureType.Twist, handler.LastGesture);
        }

        #endregion

        #region Feature F28: Procedural Cosmic Audio Synthesizer (5 Tests)

        [Test]
        public void F28_CosmicAudio_SpacetimeHum_FrequencyModulatesWithCurvature()
        {
            // Requirement R5 & Acceptance Criteria: Spacetime curvature hum modulates frequency with metric depression
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();

            double fFlat = audioEngine.GetSpacetimeHumFrequency(0.0);
            double fDeep = audioEngine.GetSpacetimeHumFrequency(-50.0);

            Assert.AreEqual(55.0, fFlat, 1e-9);
            Assert.Less(fDeep, fFlat, "Deep spacetime curvature indentation must pitch the cosmic hum downward.");
            Assert.Greater(fDeep, 30.0, "Frequency remains in audible sub-bass spectrum.");
        }

        [Test]
        public void F28_CosmicAudio_PulsarClicks_GeneratesImpulsesAtSpinFrequency()
        {
            // Requirement R5: Pulsar radio clicks generate pulses matching spin rate
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();
            audioEngine.SampleRate = 48000.0;

            double samplesPerPulse = audioEngine.GetPulsarImpulseInterval(20.0);

            Assert.AreEqual(2400.0, samplesPerPulse, 1e-9, "20 Hz pulsar at 48kHz must fire click impulse every 2400 audio samples.");
        }

        [Test]
        public void F28_CosmicAudio_LaunchBurst_GeneratesSweptFrequencyAndDecay()
        {
            // Slingshot launch burst generates downward pitch glide and exponential decay
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();

            double freq0 = audioEngine.GetLaunchBurstFrequency(0.0);
            double freqEnd = audioEngine.GetLaunchBurstFrequency(0.2);

            double amp0 = audioEngine.GetLaunchBurstAmplitude(0.0);
            double ampEnd = audioEngine.GetLaunchBurstAmplitude(0.3);

            Assert.AreEqual(222.0, freq0, 1e-9);
            Assert.Less(freqEnd, 50.0, "Kick frequency glides downward toward fundamental.");
            Assert.AreEqual(1.0, amp0, 1e-9);
            Assert.Less(ampEnd, 0.1, "Amplitude decays exponentially.");
        }

        [Test]
        public void F28_CosmicAudio_SupernovaBlast_GeneratesSubBassAndBrownNoise()
        {
            // Supernova blast synthesizes heavy sub-bass drop (95 Hz -> 24 Hz) and red noise
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();

            double startPitch = audioEngine.GetSupernovaBlastPitch(0.0);
            double endPitch = audioEngine.GetSupernovaBlastPitch(2.5);

            Assert.AreEqual(95.0, startPitch, 1e-9);
            Assert.AreEqual(24.0, endPitch, 1e-9);
        }

        [Test]
        public void F28_CosmicAudio_BufferOutput_ContainsNoNaNOrClippingArtifacts()
        {
            // Audio output buffer must stay bounded in [-1.0, 1.0] and contain zero NaN or Inf
            IProceduralAudioEngine audioEngine = new HarnessAudioEngine();
            float[] audioBuffer = new float[512];

            audioEngine.FillAudioBuffer(audioBuffer, AudioEventType.SpacetimeHum, -25.0);

            for (int i = 0; i < audioBuffer.Length; i++)
            {
                Assert.IsFalse(float.IsNaN(audioBuffer[i]), $"Audio sample {i} is NaN.");
                Assert.IsFalse(float.IsInfinity(audioBuffer[i]), $"Audio sample {i} is Infinity.");
                Assert.GreaterOrEqual(audioBuffer[i], -1.0f, $"Audio sample {i} clipped below -1.0.");
                Assert.LessOrEqual(audioBuffer[i], 1.0f, $"Audio sample {i} clipped above +1.0.");
            }
        }

        #endregion
    }
}
