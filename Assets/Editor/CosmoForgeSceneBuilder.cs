// ============================================================================
// CosmoForge: Automated Scene Builder & Wiring Tool
// Conforms to PROJECT.md § Scene & Android Build Specifications
// ============================================================================

namespace CosmoForge.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Rendering.Universal;
    using CosmoForge.Core;
    using CosmoForge.Core.Performance;
    using CosmoForge.Physics.Trajectory;
    using CosmoForge.Physics.Grid;
    using CosmoForge.Puzzles.Core;
    using CosmoForge.UI.Workbench;
    using CosmoForge.UI.Input;
    using CosmoForge.UI.Telemetry;
    using CosmoForge.UI.Feedback;
    using CosmoForge.Audio;

    public static class CosmoForgeSceneBuilder
    {
        [MenuItem("CosmoForge/Build Main Sandbox Scene")]
        public static void BuildMainSandboxScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light (Deep Cosmic Starlight)
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.95f, 0.95f, 1.0f);
            light.intensity = 0.8f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Main Camera with FloatingOrigin & Touch controls
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.04f, 1f); // Deep Interstellar Void
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 120f, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var floatingOrigin = camGo.AddComponent<FloatingOrigin>();
            var touchHandler = camGo.AddComponent<TouchGestureHandler>();

            // 3. Spacetime Grid
            var gridGo = new GameObject("SpacetimeCurvatureGrid");
            var gridVisualizer = gridGo.AddComponent<SpacetimeCurvatureGrid>();
            Shader gridShader = Shader.Find("CosmoForge/SpacetimeCurvatureGrid");
            if (gridShader != null)
            {
                var gridMat = new Material(gridShader);
                gridGo.GetComponent<MeshRenderer>().sharedMaterial = gridMat;
            }

            // 4. Trajectory Visualizer
            var trajGo = new GameObject("TrajectoryRenderer");
            var lineRenderer = trajGo.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.5f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineRenderer.material.color = new Color(0f, 0.94f, 1f, 0.9f);
            var trajRenderer = trajGo.AddComponent<TrajectoryRenderer>();

            // 5. Slingshot Aim Controller
            var slingGo = new GameObject("SlingshotController");
            var slingLine = slingGo.AddComponent<LineRenderer>();
            slingLine.useWorldSpace = true;
            slingLine.startWidth = 0.4f;
            slingLine.endWidth = 0.05f;
            slingLine.enabled = false;
            var slingshot = slingGo.AddComponent<SlingshotController>();
            SetPrivateField(slingshot, "aimLine", slingLine);

            // 6. Procedural Audio Synthesizer
            var audioGo = new GameObject("ProceduralAudioSynthesizer");
            var audioSource = audioGo.AddComponent<AudioSource>();
            audioSource.playOnAwake = true;
            audioSource.loop = true;
            var audioSynth = audioGo.AddComponent<ProceduralAudioSynthesizer>();

            // 7. Puzzle Engine
            var puzzleGo = new GameObject("PuzzleEngine");
            var puzzleEngine = puzzleGo.AddComponent<PuzzleEngine>();

            // 8. Confetti Celebration FX
            var confettiGo = new GameObject("ConfettiCelebration");
            var ps = confettiGo.AddComponent<ParticleSystem>();
            var mainPs = ps.main;
            mainPs.playOnAwake = false;
            mainPs.loop = false;
            mainPs.startLifetime = 3f;
            mainPs.startSpeed = 15f;
            var celebration = confettiGo.AddComponent<ConfettiCelebration>();

            // 9. UI Canvas & HUD
            var canvasGo = new GameObject("CosmicHUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Telemetry Inspector Panel
            var telemPanelGo = new GameObject("TelemetryPanel");
            telemPanelGo.transform.SetParent(canvasGo.transform, false);
            var telemRect = telemPanelGo.AddComponent<RectTransform>();
            telemRect.anchorMin = new Vector2(1f, 0.5f);
            telemRect.anchorMax = new Vector2(1f, 0.5f);
            telemRect.pivot = new Vector2(1f, 0.5f);
            telemRect.sizeDelta = new Vector2(240f, 320f);
            telemRect.anchoredPosition = new Vector2(-15f, 0f);

            var telemetryInspector = telemPanelGo.AddComponent<TelemetryInspector>();

            // Workbench Controller
            var workbenchGo = new GameObject("WorkbenchController");
            var workbench = workbenchGo.AddComponent<WorkbenchController>();

            // 10. Master App Coordinator
            var appGo = new GameObject("CosmoForge_App");
            var app = appGo.AddComponent<CosmoForgeApp>();

            SetPrivateField(app, "floatingOrigin", floatingOrigin);
            SetPrivateField(app, "spacetimeGrid", gridVisualizer);
            SetPrivateField(app, "trajectoryRenderer", trajRenderer);
            SetPrivateField(app, "workbench", workbench);
            SetPrivateField(app, "touchHandler", touchHandler);
            SetPrivateField(app, "slingshot", slingshot);
            SetPrivateField(app, "telemetryInspector", telemetryInspector);
            SetPrivateField(app, "puzzleEngine", puzzleEngine);
            SetPrivateField(app, "audioSynth", audioSynth);
            SetPrivateField(app, "celebration", celebration);

            // Save scene to Assets/Scenes/MainSandbox.unity
            string scenePath = "Assets/Scenes/MainSandbox.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[CosmoForge] Successfully built and saved production scene to {scenePath}");

            // Register in Build Settings
            var originalScenes = EditorBuildSettings.scenes;
            var newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
            for (int i = 0; i < originalScenes.Length; i++)
            {
                newScenes[i + 1] = originalScenes[i];
            }
            EditorBuildSettings.scenes = newScenes;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
