using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DemoCompatibilityRepair
{
    const string Root="Assets/KUBIKOS - World/";
    public static void Repair()
    {
        try
        {
            var materials=new Dictionary<string,Material>();
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Root+"URP Support/Materials URP"}))
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                materials[mat.name]=mat;
            }
            int total=0;
            foreach(string path in Directory.GetFiles(Root+"Demo/Scenes","Demo*.unity"))
            {
                var scene=EditorSceneManager.OpenScene(path.Replace('\\','/'));
                // The package's legacy baked data cannot be consumed by Unity 6.
                Lightmapping.lightingDataAsset=null;
                LightmapSettings.lightmaps=Array.Empty<LightmapData>();
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight=new Color(.45f,.48f,.52f);
                RenderSettings.ambientIntensity=1;
                var ambient=new UnityEngine.Rendering.SphericalHarmonicsL2();
                ambient.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe=ambient;
                int replaced=0;
                foreach(GameObject root in scene.GetRootGameObjects())
                foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.lightmapIndex=-1;
                    Material[] slots=renderer.sharedMaterials; bool changed=false;
                    for(int i=0;i<slots.Length;i++)
                    {
                        Material original=slots[i]; if(original==null) continue;
                        if(materials.TryGetValue(original.name,out Material urp))
                        { if(urp!=original) { slots[i]=urp; changed=true; replaced++; } }
                        else if(original.shader==null || original.shader.name.StartsWith("Animmal/") || original.shader.name=="Standard")
                            throw new Exception("No URP replacement for "+original.name+" in "+path);
                    }
                    if(changed) { renderer.sharedMaterials=slots; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); EditorUtility.SetDirty(renderer); }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("DEMO_REPAIRED: "+path+" material slots="+replaced); total+=replaced;
            }
            AssetDatabase.SaveAssets();
            Verify();
            Debug.Log("DEMO_COMPATIBILITY_PASSED: "+total+" material references updated to supplied URP materials.");
            EditorApplication.Exit(0);
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    public static void Verify()
    {
        var failures=new HashSet<string>();
        foreach(string path in Directory.GetFiles(Root+"Demo/Scenes","Demo*.unity"))
        {
            var scene=EditorSceneManager.OpenScene(path.Replace('\\','/'));
            foreach(GameObject root in scene.GetRootGameObjects())
            {
                foreach(Transform child in root.GetComponentsInChildren<Transform>(true))
                    if(PrefabUtility.GetPrefabInstanceStatus(child.gameObject)==PrefabInstanceStatus.MissingAsset)
                        throw new Exception("Missing prefab remains: "+path+" / "+child.name);
                foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    foreach(Material material in renderer.sharedMaterials)
                        if(material!=null && (material.shader==null || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader)))
                        {
                            string key=material.shader!=null?material.shader.name:material.name;
                            if(failures.Add(key))
                            {
                                Debug.LogError("DEMO_SHADER: "+material.name+" / "+key);
                                if(material.shader!=null) foreach(var message in ShaderUtil.GetShaderMessages(material.shader)) Debug.LogError(message.message+" at "+message.file+":"+message.line);
                            }
                        }
            }
            if(path.EndsWith("Demo4.unity") || path.EndsWith("Demo1.unity")) Capture(path);
        }
        if(failures.Count>0) throw new Exception("Unsupported Demo shaders: "+failures.Count);
    }
    static void Capture(string scenePath)
    {
        Camera camera=Camera.main; if(camera==null) throw new Exception("Demo has no main camera");
        var texture=new RenderTexture(1200,800,24);
        var previous=camera.targetTexture; var active=RenderTexture.active;
        Texture2D image=null;
        try
        {
            camera.targetTexture=texture; camera.Render(); RenderTexture.active=texture;
            image=new Texture2D(1200,800,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1200,800),0,0); image.Apply();
            File.WriteAllBytes("Logs/"+Path.GetFileNameWithoutExtension(scenePath)+"-Repaired.png",image.EncodeToPNG());
        }
        finally { camera.targetTexture=previous; RenderTexture.active=active; UnityEngine.Object.DestroyImmediate(texture); if(image!=null) UnityEngine.Object.DestroyImmediate(image); }
    }
}
