using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Coffee Match > 2. Build Scene
/// Assembles the full game scene from prefabs created by EnvironmentBuilder.
/// </summary>
public static class SceneCreator
{
    const string MatPath = "Assets/Materials";
    const string PfbPath = "Assets/Prefabs";

    // World-space Z positions (larger Z = further from camera = higher on screen)
    const float Z_Water        = 15.0f;
    const float Z_Sand         = 11.5f;
    const float Z_SlotsRow     =  8.0f;
    const float Z_Generator    =  7.0f;   // top of each lane
    const float Z_LaneBottom   =  1.0f;   // delivery end (close to camera)
    const float Z_Collection   =  0.3f;   // colored pad where player picks up

    static readonly float[]        LaneX  = { -3.2f, 0f, 3.2f };
    static readonly PackageColor[] Colors = { PackageColor.Red, PackageColor.Blue, PackageColor.Yellow };
    static readonly string[]       MatNames = { "Mat_Red", "Mat_Blue", "Mat_Yellow" };

    // ── Entry Point ────────────────────────────────────────────────────────

    [MenuItem("Coffee Match/2. Build Scene")]
    public static void BuildScene()
    {
        if (!AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/LaneTrack.prefab"))
        {
            Debug.LogError("Prefabs missing — run 'Coffee Match > 1. Build All Assets' first.");
            return;
        }

        ClearScene();
        PlaceLighting();
        PlaceCamera();
        PlaceBackground();
        PlaceSlotsRow();
        PlaceLanes();
        PlaceGameManager();

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Scene ready. Press Play (scripts will be wired next).");
    }

    // ── Clear ──────────────────────────────────────────────────────────────

    static void ClearScene()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            Object.DestroyImmediate(go);
    }

    // ── Lighting ───────────────────────────────────────────────────────────

    static void PlaceLighting()
    {
        var go = new GameObject("Directional Light");
        var l  = go.AddComponent<Light>();
        l.type      = LightType.Directional;
        l.color     = new Color(1f, 0.96f, 0.88f);
        l.intensity = 1.1f;
        l.shadows   = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
    }

    // ── Camera ─────────────────────────────────────────────────────────────
    // Perspective, 55° tilt — matches the reference screenshots

    static void PlaceCamera()
    {
        var go  = new GameObject("Main Camera");
        go.tag  = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView    = 60f;
        cam.nearClipPlane  = 0.1f;
        cam.farClipPlane   = 80f;
        cam.backgroundColor = new Color(0.49f, 0.77f, 0.91f);
        go.AddComponent<AudioListener>();
        go.AddComponent<UniversalAdditionalCameraData>();
        go.transform.position    = new Vector3(0f, 12f, -7f);
        go.transform.eulerAngles = new Vector3(55f, 0f, 0f);
    }

    // ── Background ─────────────────────────────────────────────────────────

    static void PlaceBackground()
    {
        var bg = new GameObject("--- Background ---");
        Plane(bg, "Water",  new Vector3(0, -0.12f, Z_Water),       new Vector3(8, 1, 5),   "Mat_Water");
        Plane(bg, "Sand",   new Vector3(0, -0.08f, Z_Sand),        new Vector3(8, 1, 2),   "Mat_Sand");
        Plane(bg, "Floor",  new Vector3(0, -0.06f, 4.5f),          new Vector3(3.5f,1,3.5f),"Mat_Ground");
    }

    // ── Slots Row ──────────────────────────────────────────────────────────
    // 7 holding slots between the player strip and the lane generators

    static void PlaceSlotsRow()
    {
        var parent  = new GameObject("--- SlotsRow ---");
        var slotPfb = Pfb("Slot");

        const int   count   = 7;
        const float spacing = 1.25f;
        float startX = -(count - 1) * spacing / 2f;

        for (int i = 0; i < count; i++)
        {
            var slot = Inst(slotPfb, $"Slot_{i}", new Vector3(startX + i * spacing, 0.06f, Z_SlotsRow));
            slot.transform.SetParent(parent.transform);
        }
    }

    // ── Lanes ──────────────────────────────────────────────────────────────
    // 3 lanes, each: LaneTrack + ContainerGenerator + colored delivery pad

    static void PlaceLanes()
    {
        var lanesRoot = new GameObject("--- Lanes ---");

        for (int i = 0; i < 3; i++)
        {
            float x = LaneX[i];
            PackageColor color = Colors[i];

            var laneGO = new GameObject($"Lane_{i}_{color}");
            laneGO.transform.SetParent(lanesRoot.transform);

            // Lane track: starts at Z_LaneBottom, runs to just below the generator
            var track = Inst(Pfb("LaneTrack"), "Track", new Vector3(x, 0f, Z_LaneBottom));
            track.transform.SetParent(laneGO.transform);

            // Generator sits at the top of the lane
            var gen = Inst(Pfb("ContainerGenerator"), "Generator", new Vector3(x, 0f, Z_Generator));
            gen.transform.SetParent(laneGO.transform);

            // Tint the generator top lip with the lane's color
            TintChild(gen, "TopLip", MatNames[i]);

            // Colored delivery pad at the bottom of the lane
            var pad = Cube(laneGO, $"DeliveryPad_{color}",
                           new Vector3(x, -0.03f, Z_Collection),
                           new Vector3(1.5f, 0.08f, 1.3f),
                           MatNames[i]);

            // Delivery point marker (scripts will reference this)
            Marker(laneGO, "DeliveryPoint", new Vector3(x, 0.2f, Z_Collection));
            Marker(laneGO, "SpawnPoint",    new Vector3(x, 0.2f, Z_Generator - 0.3f));
        }
    }

    // ── GameManager ────────────────────────────────────────────────────────

    static void PlaceGameManager()
    {
        new GameObject("--- GameManager ---");
    }

    // ── Low-level helpers ──────────────────────────────────────────────────

    static void Plane(GameObject parent, string name, Vector3 pos, Vector3 scale, string matName)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(matName);
        Object.DestroyImmediate(go.GetComponent<MeshCollider>());
        go.transform.SetParent(parent.transform);
    }

    static GameObject Cube(GameObject parent, string name, Vector3 pos, Vector3 scale, string matName)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(matName);
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.transform.SetParent(parent.transform);
        return go;
    }

    static GameObject Marker(GameObject parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.SetParent(parent.transform);
        return go;
    }

    static GameObject Inst(GameObject prefab, string name, Vector3 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        return go;
    }

    static void TintChild(GameObject parent, string childName, string matName)
    {
        var child = parent.transform.Find(childName);
        if (child == null) return;
        var mr = child.GetComponent<MeshRenderer>();
        if (mr == null) return;
        mr.sharedMaterial = Mat(matName);
    }

    static GameObject Pfb(string name) =>
        AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/{name}.prefab");

    static Material Mat(string name) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{MatPath}/{name}.mat");
}
