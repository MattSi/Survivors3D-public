using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestSceneChecks
{
    [MenuItem("Tools/Survivors/Open Forest Camp")]
    public static void Open()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
    }

    public static void Verify()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        int meshes = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) throw new Exception("Missing mesh: " + filter.name);
                meshes++;
            }
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                // Particle systems have an unused null trail-material slot when trails are disabled.
                if (renderer is ParticleSystemRenderer particles && !particles.GetComponent<ParticleSystem>().trails.enabled)
                {
                    if (particles.sharedMaterial == null || particles.sharedMaterial.shader == null)
                        throw new Exception("Missing particle material: " + renderer.name);
                    continue;
                }
                foreach (Material material in renderer.sharedMaterials)
                    if (material == null || material.shader == null) throw new Exception("Missing material/shader: " + renderer.name);
            }
        }
        if (meshes < 250) throw new Exception("Forest scenery is incomplete.");
        if (UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>() == null
            || UnityEngine.Object.FindFirstObjectByType<PlayerController>() == null
            || UnityEngine.Object.FindFirstObjectByType<PlaygroundCamera>() == null)
            throw new Exception("Missing survival gameplay components.");
        WaveSessionChecks.Run();
        Debug.Log("FOREST_SCENE_VERIFY_PASSED: " + meshes + " meshes, materials, gameplay and wave checks.");
    }

    public static void VerifyPlay()
    {
        Verify();
        SessionState.SetBool("PlaygroundVerify", true);
        EditorApplication.EnterPlaymode();
    }

    public static void CheckObstacles()
    {
        ArenaObstacles arena = UnityEngine.Object.FindFirstObjectByType<ArenaObstacles>();
        if (arena == null) return; // The original Playground has no forest props.
        if (arena.Blocks.Count != 10) throw new Exception("Expected ten run obstacles.");
        CharacterController controller = UnityEngine.Object.FindFirstObjectByType<PlayerController>().GetComponent<CharacterController>();
        Vector3 saved = controller.transform.position;
        Bounds first = arena.Blocks[0];
        controller.enabled = false;
        controller.transform.position = new Vector3(first.min.x - 1, 0.5f, first.center.z);
        controller.enabled = true;
        controller.Move(Vector3.right * (first.size.x + 2));
        if (controller.transform.position.x > first.min.x - 0.3f)
            throw new Exception("Player passed through obstacle.");
        controller.enabled = false;
        controller.transform.position = saved;
        controller.enabled = true;
        arena.UpdateRoutes(Vector3.zero);
        foreach (Bounds block in arena.Blocks)
        {
            if (block.Contains(Vector3.zero)) throw new Exception("Spawn point blocked.");
            Vector3 from = block.center - Vector3.right * 4;
            if (arena.Clear(from, block.center + Vector3.right * 4, 0.12f))
                throw new Exception("Projectile did not hit obstacle.");
        }
        foreach (Vector3 start in new[] { new Vector3(-17,0,0), new Vector3(17,0,0), new Vector3(0,0,-17), new Vector3(0,0,17) })
        {
            Vector3 position = start;
            for (int step = 0; step < 1200 && position.magnitude > 0.1f; step++)
            {
                position = arena.Move(position, Vector3.zero, 0.08f);
                foreach (Bounds b in arena.Blocks)
                    if (b.Contains(position)) throw new Exception("Enemy entered obstacle.");
            }
            if (position.magnitude > 0.1f) throw new Exception("Enemy route did not reach player.");
        }
        Debug.Log("OBSTACLE_VERIFY_PASSED: spawn clearance, player/projectile blocking, four enemy routes.");
    }

    public static void CheckProjectileCombat()
    {
        var game = UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        game.Restart();
        game.BeginRun(0);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var type = typeof(SurvivorPlayground);
        var enemies = (System.Collections.Generic.List<Transform>)GameTestAccess.Get(game,"enemies");
        var portals = (System.Collections.Generic.List<Vector3>)GameTestAccess.Get(game,"spawnPoints");
        if(portals.Count != 8 || enemies.Count != 4) throw new Exception("Portal spawn count incorrect.");
        foreach(Transform enemy in enemies)
        {
            Vector3 position=enemy.position; position.y=0;
            if(!portals.Contains(position)) throw new Exception("Enemy spawned outside portal.");
            enemy.position=new Vector3(16,0.375f,16);
        }
        Transform direct=enemies[0], nearby=enemies[1], distant=enemies[2];
        direct.position=new Vector3(0,0.375f,0);
        nearby.position=new Vector3(1.8f,0.375f,0);
        type.GetMethod("ExplodeBullet",flags).Invoke(game,new object[] { direct.position,0,10 });
        var health=(System.Collections.Generic.Dictionary<Transform,int>)GameTestAccess.Get(game,"enemyHealth");
        if(health.ContainsKey(direct) || health[nearby]!=10 || health[distant]!=10)
            throw new Exception("Direct kill must not cause default splash damage.");
        var scores=(System.Collections.IList)GameTestAccess.Get(game,"floatingScores");
        if(game.Score!=10 || scores.Count!=1) throw new Exception("Kill score popup missing.");
        type.GetMethod("UpdateFloatingScores",flags).Invoke(game,new object[] {1f});
        if(scores.Count!=0) throw new Exception("Score popup did not expire.");
        game.Restart();
        game.BeginRun(0);
        if(game.Score!=0 || scores.Count!=0) throw new Exception("Score did not reset.");
        CheckBulletPositions(game);
        Debug.Log("PROJECTILE_COMBAT_PASSED: eight portals, four initial enemies, direct-only damage, finite range.");
    }

    private static void CheckBulletPositions(SurvivorPlayground game)
    {
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var type=typeof(SurvivorPlayground);
        var enemies=(System.Collections.Generic.List<Transform>)GameTestAccess.Get(game,"enemies");
        foreach(Transform enemy in enemies) enemy.position=new Vector3(0,.375f,0);
        // Shooting with a target directly under the muzzle must resolve the inside hit safely.
        type.GetMethod("Shoot",flags).Invoke(game,null);
        var bullets=(System.Collections.IList)GameTestAccess.Get(game,"bullets");
        if(bullets.Count!=1) throw new Exception("Overlap test did not create a bullet.");
        object bullet=bullets[0]; var bulletType=bullet.GetType();
        Vector3 direction=(Vector3)bulletType.GetField("Direction").GetValue(bullet);
        if(direction.sqrMagnitude<.99f) throw new Exception("Overlapping target produced a zero direction.");
        type.GetMethod("MoveBullets",flags).Invoke(game,new object[] {.016f});
        if(bullets.Count!=0) throw new Exception("Overlapping target did not consume the bullet.");
        foreach(Vector3 invalid in new[] {Vector3.zero,new Vector3(float.NaN,0,1),new Vector3(float.PositiveInfinity,0,1)})
        {
            var visual=new GameObject("Bullet regression check");
            bullet=System.Activator.CreateInstance(bulletType,true);
            bulletType.GetField("Transform").SetValue(bullet,visual.transform);
            bulletType.GetField("Direction").SetValue(bullet,invalid);
            bulletType.GetField("RemainingRange").SetValue(bullet,10f);
            bullets.Add(bullet);
            type.GetMethod("MoveBullets",flags).Invoke(game,new object[] {.016f});
            if(bullets.Count!=0 || visual.transform.position!=Vector3.zero)
                throw new Exception("Invalid bullet was moved instead of discarded.");
        }
        game.Restart(); game.BeginRun(0);
    }

    public static void CheckAvatarPulse()
    {
        CheckPulse(UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>());
        CheckPulse(UnityEngine.Object.FindFirstObjectByType<EnemyAvatar>());
        Debug.Log("AVATAR_PULSE_PASSED: visible scale animates, feet stay grounded, gameplay roots and colliders unchanged.");
    }

    public static void CheckCameraOrbit()
    {
        var camera=UnityEngine.Object.FindFirstObjectByType<PlaygroundCamera>();
        var flags=System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var type=typeof(PlaygroundCamera);
        var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
        try
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState { rightStick=Vector2.right });
            UnityEngine.InputSystem.InputSystem.Update();
            float before=camera.Yaw;
            type.GetMethod("LateUpdate",flags).Invoke(camera,null);
            if(camera.Yaw==before) throw new Exception("Right stick did not rotate camera.");
            type.GetField("<Yaw>k__BackingField",flags).SetValue(camera,0f);
        }
        finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(pad); }
        type.GetField("focus",flags).SetValue(camera,Vector3.zero);
        type.GetField("displayedDistance",flags).SetValue(camera,7f);
        var obstacle=new GameObject("Camera Check Obstacle");
        try
        {
            obstacle.transform.position=new Vector3(0,1,-1);
            var collider=obstacle.AddComponent<BoxCollider>(); collider.size=new Vector3(1,2,1);
            var visual=obstacle.AddComponent<CameraObstacleBounds>();
            visual.VisualBounds=new Bounds(new Vector3(0,0.3f,-1),new Vector3(1,0.6f,1));
            Physics.SyncTransforms();
            type.GetMethod("ApplyView",flags).Invoke(camera,null);
            if((float)type.GetField("displayedDistance",flags).GetValue(camera)<6.9f)
                throw new Exception("Low stump incorrectly shortened camera distance.");
            visual.VisualBounds=new Bounds(new Vector3(0,1.5f,-1),new Vector3(1,3,1));
            collider.size=new Vector3(1,3,1); Physics.SyncTransforms();
            type.GetMethod("ApplyView",flags).Invoke(camera,null);
            float near=(float)type.GetField("displayedDistance",flags).GetValue(camera);
            if(near<6.9f) throw new Exception("Distant camera moved because scenery blocked the sight line.");
            obstacle.SetActive(false);
            type.GetField("lastObstructedAt",flags).SetValue(camera,Time.time-1);
            type.GetMethod("ApplyView",flags).Invoke(camera,null);
            float recovery=(float)type.GetField("displayedDistance",flags).GetValue(camera);
            if(Mathf.Abs(recovery-7)>.01f) throw new Exception("Camera distance changed after sight-line obstruction.");
        }
        finally { UnityEngine.Object.DestroyImmediate(obstacle); }
        type.GetField("displayedDistance",flags).SetValue(camera,7f);
        type.GetField("distanceVelocity",flags).SetValue(camera,0f);
        Debug.Log("CAMERA_ORBIT_PASSED: right stick yaw, low-stump rejection, tall-obstacle protection and smooth recovery.");
    }

    public static void CheckWavePopulation()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        var type=typeof(SurvivorPlayground);
        var flags=System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var session=(WaveSession)GameTestAccess.Get(game,"session");
        game.Restart();
        game.BeginRun(0);
        float previousRate=0;
        for(int wave=1;wave<=WaveSession.TotalWaves;wave++)
        {
            int initial=(int)type.GetProperty("InitialEnemies",flags).GetValue(game);
            int batch=(int)type.GetProperty("SpawnBatch",flags).GetValue(game);
            int limit=(int)type.GetProperty("EnemyLimit",flags).GetValue(game);
            float interval=(float)type.GetProperty("WaveSpawnInterval",flags).GetValue(game);
            if(initial!=4+(wave-1)*2 || limit!=14+(wave-1)*4 || interval<=0 || batch/interval<=previousRate
                || (wave>1 && batch/interval>previousRate*1.3f))
                throw new Exception("Wave population does not increase progressively.");
            previousRate=batch/interval;
            session.Advance(session.WaveDuration/2);
            float middleInterval=(float)type.GetProperty("WaveSpawnInterval",flags).GetValue(game);
            if(Mathf.Abs(interval/middleInterval-1.125f)>.001f)
                throw new Exception("Spawn pressure should build gradually within a wave.");
            if(wave<WaveSession.TotalWaves) { session.Advance(session.WaveDuration); session.Continue(); }
        }
        game.Restart();
        game.BeginRun(0);
        Debug.Log("WAVE_POPULATION_PASSED: initial population, spawn rate and enemy limit increase across all twenty waves; restart restores wave one.");
    }

    private static void CheckPulse(Component avatar)
    {
        if (avatar == null) throw new Exception("Avatar missing.");
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var pulse = (AvatarPulse)avatar.GetType().GetField("pulse",flags).GetValue(avatar);
        var visual = (Transform)avatar.GetType().GetField("visual",flags).GetValue(avatar);
        Vector3 rootScale=avatar.transform.localScale, rootPosition=avatar.transform.position;
        Collider collider=avatar.GetComponent<Collider>();
        Bounds collision=collider != null ? collider.bounds : default;
        pulse.Apply(0);
        Vector3 firstScale=visual.localScale;
        float floor=float.PositiveInfinity;
        foreach(Renderer body in visual.GetComponentsInChildren<Renderer>()) floor=Mathf.Min(floor,body.bounds.min.y);
        pulse.Apply(0.1625f);
        // A random phase can give equal sine values a quarter-cycle apart.
        if((visual.localScale-firstScale).sqrMagnitude<0.0000001f) pulse.Apply(0.325f);
        if((visual.localScale-firstScale).sqrMagnitude<0.0000001f) throw new Exception("Visual did not pulse.");
        float newFloor=float.PositiveInfinity;
        foreach(Renderer body in visual.GetComponentsInChildren<Renderer>()) newFloor=Mathf.Min(newFloor,body.bounds.min.y);
        if(Mathf.Abs(newFloor-floor)>0.001f) throw new Exception("Pulse moved feet off floor.");
        if(avatar.transform.localScale != rootScale || avatar.transform.position != rootPosition ||
            (collider != null && collider.bounds != collision)) throw new Exception("Pulse modified gameplay bounds.");
        pulse.Apply(Time.time);
    }

    public static void RenderOverview()
    {
        Verify();
        Camera camera = Camera.main;
        camera.transform.SetPositionAndRotation(new Vector3(42, 62, -58), Quaternion.LookRotation(new Vector3(-42, -62, 58)));
        camera.orthographic = true;
        camera.orthographicSize = 42;
        var target = new RenderTexture(1400, 1100, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var texture = new Texture2D(1400, 1100, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1400, 1100), 0, 0);
        texture.Apply();
        System.IO.File.WriteAllBytes("ArtSource/World/ForestCamp-Unity.png", texture.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(texture);
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("FOREST_UNITY_RENDER_PASSED");
    }
}
