using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class PlaygroundBuilder
{
    private const string ScenePath = "Assets/Scenes/Playground.unity";

    [MenuItem("Tools/Survivors/Create Playground Scene")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        Material red = Material("Player Red", new Color(0.9f, 0.035f, 0.065f));
        Material grey = Material("Enemy Grey", new Color(0.47f, 0.5f, 0.54f));
        Material bullet = Material("Bullet Gold", new Color(1f, 0.8f, 0.18f));
        Material hit = Material("Player Hit", new Color(1f, 0.7f, 0.7f));
        Material floor = Material("Arena Floor", new Color(0.1f, 0.15f, 0.2f));
        Material grid = Material("Arena Grid", new Color(0.15f, 0.22f, 0.28f));
        Material wall = Material("Arena Border", new Color(0.27f, 0.4f, 0.46f));
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject environment = new GameObject("Arena");
        Cube("Floor", new Vector3(0, -0.15f, 0), new Vector3(36, 0.3f, 36), floor, environment.transform);
        for (int i = -16; i <= 16; i += 2)
        {
            Cube("Grid X", new Vector3(i, 0.003f, 0), new Vector3(0.025f, 0.005f, 36), grid, environment.transform);
            Cube("Grid Z", new Vector3(0, 0.003f, i), new Vector3(36, 0.005f, 0.025f), grid, environment.transform);
        }
        Cube("North Border", new Vector3(0, 0.2f, 18.15f), new Vector3(36.6f, 0.4f, 0.3f), wall, environment.transform);
        Cube("South Border", new Vector3(0, 0.2f, -18.15f), new Vector3(36.6f, 0.4f, 0.3f), wall, environment.transform);
        Cube("East Border", new Vector3(18.15f, 0.2f, 0), new Vector3(0.3f, 0.4f, 36), wall, environment.transform);
        Cube("West Border", new Vector3(-18.15f, 0.2f, 0), new Vector3(0.3f, 0.4f, 36), wall, environment.transform);
        GameObject player = Cube("Player", new Vector3(0, 0.5f, 0), Vector3.one, red, null);
        CharacterController character = player.AddComponent<CharacterController>();
        character.height = 1f;
        character.radius = 0.48f;
        character.center = Vector3.zero;
        character.minMoveDistance = 0f;
        PlayerController controller = player.AddComponent<PlayerController>();
        player.AddComponent<PlayerAvatar>();
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 20.88838f, -14.62620f);
        camera.transform.LookAt(Vector3.zero);
        camera.orthographic = false;
        camera.fieldOfView = 50f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.055f, 0.08f);
        camera.gameObject.AddComponent<PlaygroundCamera>();
        Light light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.7f, 0.8f);
        SurvivorPlayground playground = new GameObject("Playground").AddComponent<SurvivorPlayground>();
        playground.Configure(controller, health, player.GetComponent<Renderer>(), grey, bullet, red, hit);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            if (existing.path != ScenePath) scenes.Add(existing);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("PLAYGROUND: Scene created at " + ScenePath);
    }

    private static Material Material(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetFloat("_Smoothness", 0.25f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    // Command-line smoke test runs the real scene through shooting, damage, death and restart.
    public static void BuildAndVerify()
    {
        WaveSessionChecks.Run();
        Build();
        SessionState.SetBool("PlaygroundVerify", true);
        EditorApplication.EnterPlaymode();
    }

    private static double started;
    private static int stage;
    private static int hits;
    private static Vector3 initialPosition;
    private static Keyboard testKeyboard;
    private static float movementStarted;

    [InitializeOnLoadMethod]
    private static void RegisterVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PlaygroundVerify", false)) return;
            // Batch mode has no focused Game view; route synthetic test events into gameplay.
            InputSystem.settings = UnityEngine.Object.Instantiate(InputSystem.settings);
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            started = EditorApplication.timeSinceStartup;
            stage = 0;
            hits = 0;
            EditorApplication.update += VerifyUpdate;
        };
    }

    private static void VerifyUpdate()
    {
        try
        {
            SurvivorPlayground game = UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
            if (EditorApplication.timeSinceStartup - started > 40) throw new Exception("Playground verification timed out.");
            if (game == null || Time.timeSinceLevelLoad < 0.2f) return;
            if (stage == 0)
            {
                Require(game.EnemyCount==0,"No combat before starter selection");
                game.NewGame();
                game.BeginRun(0);
                ForestSceneChecks.CheckObstacles();
                ArenaObstacles arena = UnityEngine.Object.FindFirstObjectByType<ArenaObstacles>();
                if (arena != null)
                    for (int layout = 0; layout < 4; layout++)
                    {
                        Vector3 previous = arena.Blocks[0].center;
                        arena.Generate();
                        Require(arena.Blocks[0].center != previous, "New run changes obstacle layout");
                        ForestSceneChecks.CheckObstacles();
                    }
                Require(game.Health.Current == 100 && game.EnemyCount >= 6, "Initial health / enemies");
                ForestSceneChecks.CheckProjectileCombat();
                ForestSceneChecks.CheckAvatarPulse();
                ForestSceneChecks.CheckWavePopulation();
                ForestSceneChecks.CheckCameraOrbit();
                ItemSystemChecks.Run(game);
                initialPosition = game.Health.transform.position;
                testKeyboard = InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.W, Key.D));
                InputSystem.Update();
                movementStarted = Time.timeSinceLevelLoad;
                stage = 1;
            }
            else if (stage == 1 && Time.timeSinceLevelLoad - movementStarted > 0.8f)
            {
                Vector3 movement = game.Health.transform.position - initialPosition;
                Require(movement.x > 0.1f && movement.z > 0.1f, "WASD input movement: " + movement);
                InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
                stage = 2;
            }
            else if (stage == 2 && game.Kills > 0)
            {
                game.Restart();
                game.BeginRun(0);
                // Isolate the health checks from natural enemy contacts during this phase.
                game.enabled = false;
                Require(game.Health.TakeDamage(10) && game.Health.Current == 90, "Contact damage is 10");
                Require(!game.Health.TakeDamage(10) && game.Health.Current == 90, "Invulnerability prevents frame damage");
                hits = 1;
                stage = 3;
            }
            else if (stage == 3)
            {
                if (game.Health.TakeDamage(10)) hits++;
                if (!game.Health.IsDead) return;
                Require(hits == 10 && game.Health.Current == 0, "Ten hits kill the player");
                Require(!game.Health.GetComponent<PlayerController>().enabled, "Movement stops on death");
                game.enabled = true;
                InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.R));
                stage = 4;
            }
            else if (stage == 4 && !game.Health.IsDead)
            {
                Require(game.Health.Current == 100 && game.Kills == 0 && game.EnemyCount == 0, "R returns to starter selection");
                InputSystem.RemoveDevice(testKeyboard);
                SessionState.SetBool("PlaygroundVerify", false);
                EditorApplication.update -= VerifyUpdate;
                Debug.Log("PLAYGROUND_VERIFY_PASSED: movement, auto fire / kills, 10 damage, invulnerability, death, R restart.");
                EditorApplication.Exit(0);
            }
        }
        catch (Exception exception)
        {
            SessionState.SetBool("PlaygroundVerify", false);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("PLAYGROUND_VERIFY_FAILED: " + message);
    }
}
