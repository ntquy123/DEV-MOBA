#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TerrainSceneCreator
{
    private const string ScenePath = "Assets/Scenes/TerrainScene.unity";

    [MenuItem("Tools/DEV MOBA/Create Terrain Scene")]
    public static void CreateTerrainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var terrainData = new TerrainData
        {
            heightmapResolution = 129,
            size = new Vector3(200f, 20f, 200f)
        };

        var heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (var x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (var y = 0; y < terrainData.heightmapResolution; y++)
            {
                heights[y, x] = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.15f;
            }
        }

        terrainData.SetHeights(0, 0, heights);

        var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        terrainGO.name = "Terrain";
        terrainGO.transform.position = Vector3.zero;

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 3f, 0f);

        var renderer = player.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            renderer.sharedMaterial = material;
        }

        var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGO.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(0f, 15f, -25f);
        cameraGO.transform.LookAt(player.transform);

        var lightGO = new GameObject("Directional Light", typeof(Light));
        var light = lightGO.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.color = Color.white;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Terrain scene created at '{ScenePath}'. Open it from the Project window.");
    }
}
#endif