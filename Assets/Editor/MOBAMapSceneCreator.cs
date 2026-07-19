#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MOBAMapSceneCreator
{
    private const string ScenePath = "Assets/Scenes/MOBAMap.unity";

    [MenuItem("Tools/DEV MOBA/Create MOBAMap Scene")]
    public static void CreateMOBAMapScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("MOBAMap");

        var terrain = CreateTerrain(root.transform);
        CreateSpawnPoint("BlueSpawn", new Vector3(-130f, 2f, -130f), Color.blue, root.transform);
        CreateSpawnPoint("RedSpawn", new Vector3(130f, 2f, 130f), Color.red, root.transform);
        CreateSpawnPoint("BlueBase", new Vector3(-150f, 2f, 150f), new Color(0.2f, 0.7f, 1f), root.transform);
        CreateSpawnPoint("RedBase", new Vector3(150f, 2f, -150f), new Color(1f, 0.25f, 0.5f), root.transform);

        var lanesRoot = new GameObject("Lanes");
        lanesRoot.transform.SetParent(root.transform);
        CreateLane("TopLane", new Vector3(-130f, 1f, 130f), new Vector3(130f, 1f, 130f), Color.yellow, lanesRoot.transform);
        CreateLane("MidLane", new Vector3(-130f, 1f, -130f), new Vector3(130f, 1f, 130f), Color.green, lanesRoot.transform);
        CreateLane("BottomLane", new Vector3(-130f, 1f, -130f), new Vector3(130f, 1f, -130f), Color.cyan, lanesRoot.transform);

        var towersRoot = new GameObject("Towers");
        towersRoot.transform.SetParent(root.transform);
        CreateTower("BlueTower", new Vector3(-100f, 1f, 100f), Color.blue, towersRoot.transform);
        CreateTower("RedTower", new Vector3(100f, 1f, -100f), Color.red, towersRoot.transform);

        CreateCamera(root.transform, new Vector3(0f, 120f, -220f), new Vector3(30f, 0f, 0f));
        CreateDirectionalLight(root.transform);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"MOBAMap scene created at '{ScenePath}'. Open it from the Project window.");
    }

    private static GameObject CreateTerrain(Transform parent)
    {
        var terrainData = new TerrainData
        {
            heightmapResolution = 257,
            size = new Vector3(300f, 20f, 300f)
        };

        var heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (var x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (var y = 0; y < terrainData.heightmapResolution; y++)
            {
                var nx = (float)x / (terrainData.heightmapResolution - 1);
                var ny = (float)y / (terrainData.heightmapResolution - 1);
                var edgeFalloff = Mathf.SmoothStep(1f, 0.25f, Mathf.Min(nx, 1 - nx)) * Mathf.SmoothStep(1f, 0.25f, Mathf.Min(ny, 1 - ny));
                heights[y, x] = Mathf.PerlinNoise(nx * 5f, ny * 5f) * 0.12f * edgeFalloff;
            }
        }

        terrainData.SetHeights(0, 0, heights);
        var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        terrainGO.name = "Terrain";
        terrainGO.transform.SetParent(parent);
        terrainGO.transform.position = Vector3.zero;
        return terrainGO;
    }

    private static void CreateSpawnPoint(string name, Vector3 position, Color color, Transform parent)
    {
        var spawn = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spawn.name = name;
        spawn.transform.SetParent(parent);
        spawn.transform.position = position;
        spawn.transform.localScale = Vector3.one * 4f;

        var collider = spawn.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        var renderer = spawn.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.5f);
            renderer.sharedMaterial = material;
        }
    }

    private static void CreateLane(string name, Vector3 start, Vector3 end, Color color, Transform parent)
    {
        var lane = new GameObject(name);
        lane.transform.SetParent(parent);

        var line = lane.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = 2f;
        line.endWidth = 2f;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = color;
        line.useWorldSpace = true;

        for (var i = 0; i <= 10; i++)
        {
            var t = i / 10f;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name + " Marker " + i;
            marker.transform.SetParent(lane.transform);
            marker.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * 0.5f;
            marker.transform.localScale = new Vector3(1f, 0.1f, 1f);
            var col = marker.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
            var rend = marker.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color * 0.4f;
                rend.sharedMaterial = mat;
            }
        }
    }

    private static void CreateTower(string name, Vector3 position, Color color, Transform parent)
    {
        var tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.name = name;
        tower.transform.SetParent(parent);
        tower.transform.position = position;
        tower.transform.localScale = new Vector3(6f, 12f, 6f);

        var renderer = tower.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.sharedMaterial = material;
        }
    }

    private static void CreateCamera(Transform parent, Vector3 position, Vector3 rotationEuler)
    {
        var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGO.tag = "MainCamera";
        cameraGO.transform.SetParent(parent);
        cameraGO.transform.position = position;
        cameraGO.transform.rotation = Quaternion.Euler(rotationEuler);
    }

    private static void CreateDirectionalLight(Transform parent)
    {
        var lightGO = new GameObject("Directional Light", typeof(Light));
        lightGO.transform.SetParent(parent);
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = lightGO.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.color = Color.white;
    }
}
#endif