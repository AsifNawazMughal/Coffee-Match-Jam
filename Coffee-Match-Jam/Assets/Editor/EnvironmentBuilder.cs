using UnityEditor;
using UnityEngine;

/// <summary>
/// Coffee Match > 1. Build All Assets
/// Creates all materials, ScriptableObjects, and prefabs needed for the game.
/// Run this before building the scene.
/// </summary>
public static class EnvironmentBuilder
{
    const string MatPath = "Assets/Materials";
    const string PfbPath = "Assets/Prefabs";
    const string SOPath  = "Assets/ScriptableObjects";

    // ── Entry Point ────────────────────────────────────────────────────────

    [MenuItem("Coffee Match/1. Build All Assets")]
    public static void BuildAll()
    {
        EnsureFolders();
        BuildMaterials();
        BuildScriptableObjects();
        BuildCupPrefab();
        BuildContainerPrefab("Container_Small", cols: 2, rows: 2, depth: 0.90f);
        BuildContainerPrefab("Container_Large", cols: 2, rows: 3, depth: 1.22f);
        BuildGeneratorPrefab();
        BuildLaneTrackPrefab();
        BuildSlotPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All assets built. Run: Coffee Match > 2. Build Scene");
    }

    // ── Folders ────────────────────────────────────────────────────────────

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(MatPath)) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(PfbPath)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(SOPath))  AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
    }

    // ── Materials ──────────────────────────────────────────────────────────

    static void BuildMaterials()
    {
        Mk("Mat_Red",       new Color(0.85f, 0.20f, 0.18f));
        Mk("Mat_Blue",      new Color(0.22f, 0.52f, 0.88f));
        Mk("Mat_Yellow",    new Color(0.95f, 0.80f, 0.15f));
        Mk("Mat_White",     Color.white);
        Mk("Mat_Cream",     new Color(1.0f,  0.95f, 0.78f));
        Mk("Mat_Ground",    new Color(0.65f, 0.47f, 0.30f));
        Mk("Mat_Belt",      new Color(0.88f, 0.72f, 0.50f));
        Mk("Mat_BeltSide",  new Color(0.68f, 0.52f, 0.32f));
        Mk("Mat_Generator", new Color(0.92f, 0.72f, 0.18f));
        Mk("Mat_Water",     new Color(0.30f, 0.68f, 0.85f));
        Mk("Mat_Sand",      new Color(0.93f, 0.85f, 0.65f));
        Mk("Mat_SlotFrame", new Color(0.55f, 0.40f, 0.24f));
        Mk("Mat_SlotInner", new Color(0.45f, 0.32f, 0.18f));
    }

    static void Mk(string name, Color color)
    {
        string path = $"{MatPath}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        EditorUtility.SetDirty(mat);
    }

    static Material M(string name) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{MatPath}/{name}.mat");

    // ── ScriptableObjects ──────────────────────────────────────────────────

    static void BuildScriptableObjects()
    {
        MkColorDef(PackageColor.Red,    "ColorDef_Red",    new Color(0.85f, 0.20f, 0.18f), "Mat_Red");
        MkColorDef(PackageColor.Blue,   "ColorDef_Blue",   new Color(0.22f, 0.52f, 0.88f), "Mat_Blue");
        MkColorDef(PackageColor.Yellow, "ColorDef_Yellow", new Color(0.95f, 0.80f, 0.15f), "Mat_Yellow");
        MkContainerType("ContainerType_Small", ContainerSize.Small, 2, 2);
        MkContainerType("ContainerType_Large", ContainerSize.Large, 2, 3);
    }

    static void MkColorDef(PackageColor type, string name, Color color, string matName)
    {
        string path = $"{SOPath}/{name}.asset";
        var so = AssetDatabase.LoadAssetAtPath<ColorDefinitionSO>(path);
        if (so == null) { so = ScriptableObject.CreateInstance<ColorDefinitionSO>(); AssetDatabase.CreateAsset(so, path); }
        so.colorType    = type;
        so.displayColor = color;
        so.material     = M(matName);
        EditorUtility.SetDirty(so);
    }

    static void MkContainerType(string name, ContainerSize size, int cols, int rows)
    {
        string path = $"{SOPath}/{name}.asset";
        var so = AssetDatabase.LoadAssetAtPath<ContainerTypeSO>(path);
        if (so == null) { so = ScriptableObject.CreateInstance<ContainerTypeSO>(); AssetDatabase.CreateAsset(so, path); }
        so.size    = size;
        so.columns = cols;
        so.rows    = rows;
        EditorUtility.SetDirty(so);
    }

    // ── Cup Prefab ─────────────────────────────────────────────────────────
    // Small white cylinder — the item that goes inside a container

    static void BuildCupPrefab()
    {
        string path = $"{PfbPath}/Cup.prefab";
        if (Exists(path)) return;

        var go = Prim(PrimitiveType.Cylinder, "Cup", null, Vector3.zero, new Vector3(0.17f, 0.09f, 0.17f), M("Mat_White"));
        Save(go, path);
    }

    // ── Container Prefabs ──────────────────────────────────────────────────
    // Closed cube box with a Lid on top + capsule "cans" inside.
    // The Lid is animated away by Container.Open() so the cans become visible.

    static void BuildContainerPrefab(string prefabName, int cols, int rows, float depth)
    {
        string path = $"{PfbPath}/{prefabName}.prefab";

        // Always rebuild so layout changes propagate.
        if (Exists(path)) AssetDatabase.DeleteAsset(path);

        const float w    = 1.08f;
        const float wall = 0.09f;
        const float h    = 0.42f;

        var root = new GameObject(prefabName);

        // Click detection covers the whole cube including the lid.
        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(w, h + wall, depth);
        col.center = new Vector3(0f, (h + wall) / 2f, 0f);
        col.enabled = false; // LaneController enables only on the top box.
        root.AddComponent<Container>();
        root.AddComponent<ContainerClickHandler>();

        // Open-top body: 4 walls + bottom.
        Panel(root, "Bottom", new Vector3(0,     0,        0),         new Vector3(w,    wall, depth),  "Mat_Belt");
        Panel(root, "WallL",  new Vector3(-w/2,  h/2,      0),         new Vector3(wall, h,    depth),  "Mat_Belt");
        Panel(root, "WallR",  new Vector3( w/2,  h/2,      0),         new Vector3(wall, h,    depth),  "Mat_Belt");
        Panel(root, "WallB",  new Vector3(0,     h/2,     -depth/2),   new Vector3(w,    h,    wall),   "Mat_Belt");
        Panel(root, "WallF",  new Vector3(0,     h/2,      depth/2),   new Vector3(w,    h,    wall),   "Mat_Belt");

        // Lid covering the top — opened with DOTween at runtime.
        Panel(root, "Lid",    new Vector3(0,     h + wall/2f, 0),      new Vector3(w,    wall, depth),  "Mat_Belt");

        // Capsule cans inside, evenly spaced in a cols×rows grid.
        float spX  = 0.27f;
        float spZ  = depth / (rows + 1);
        float canY = wall + 0.18f;
        float offX = -(cols - 1) * spX / 2f;
        float offZ = -(rows - 1) * spZ / 2f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Prim(PrimitiveType.Capsule,
                     $"Can_{r}_{c}",
                     root,
                     new Vector3(offX + c * spX, canY, offZ + r * spZ),
                     new Vector3(0.18f, 0.12f, 0.18f),
                     M("Mat_White"));
            }
        }

        Save(root, path);
    }

    // ── Container Generator Prefab ─────────────────────────────────────────
    // Yellow machine at the top of each lane — generates containers

    static void BuildGeneratorPrefab()
    {
        string path = $"{PfbPath}/ContainerGenerator.prefab";
        if (Exists(path)) return;

        var root = new GameObject("ContainerGenerator");

        // Main body
        Prim(PrimitiveType.Cube, "Body",   root, new Vector3(0, 0.52f, 0),   new Vector3(1.48f, 1.04f, 1.28f), M("Mat_Generator"));
        // Colored top lip (tinted per lane in SceneCreator)
        Prim(PrimitiveType.Cube, "TopLip", root, new Vector3(0, 1.06f, 0),   new Vector3(1.58f, 0.12f, 1.38f), M("Mat_BeltSide"));
        // Down-arrow indicator: a flat diamond (cube rotated 45° on Z)
        var arrow = Prim(PrimitiveType.Cube, "Arrow", root, new Vector3(0, 0.52f, 0.65f), new Vector3(0.22f, 0.22f, 0.07f), M("Mat_BeltSide"));
        arrow.transform.localEulerAngles = new Vector3(0, 0, 45f);

        Save(root, path);
    }

    // ── Lane Track Prefab ──────────────────────────────────────────────────
    // The belt + side walls, 5 container slot markers, spawn & delivery points

    static void BuildLaneTrackPrefab()
    {
        string path = $"{PfbPath}/LaneTrack.prefab";
        if (Exists(path)) return;

        const float len  = 5.5f;
        const float half = len / 2f;

        var root = new GameObject("LaneTrack");

        // Belt surface
        Prim(PrimitiveType.Cube, "Belt",  root, new Vector3(0, 0,     half), new Vector3(1.45f, 0.10f, len),  M("Mat_Belt"));
        // Side walls
        Prim(PrimitiveType.Cube, "WallL", root, new Vector3(-0.72f, 0.20f, half), new Vector3(0.10f, 0.40f, len), M("Mat_BeltSide"));
        Prim(PrimitiveType.Cube, "WallR", root, new Vector3( 0.72f, 0.20f, half), new Vector3(0.10f, 0.40f, len), M("Mat_BeltSide"));

        // Spawn point (top — where generator drops containers)
        Marker(root, "SpawnPoint",    new Vector3(0, 0.28f, len - 0.3f));
        // Delivery point (bottom — where players pick up)
        Marker(root, "DeliveryPoint", new Vector3(0, 0.28f, 0.3f));

        // 5 container slot markers evenly spaced along the lane
        for (int i = 0; i < 5; i++)
        {
            float z = 0.4f + i * (len - 0.4f) / 4f;
            Marker(root, $"ContainerSlot_{i}", new Vector3(0, 0.25f, z));
        }

        Save(root, path);
    }

    // ── Slot Prefab ────────────────────────────────────────────────────────
    // One holding slot in the SlotsRow between player strip and lanes

    static void BuildSlotPrefab()
    {
        string path = $"{PfbPath}/Slot.prefab";
        if (Exists(path)) return;

        var root = new GameObject("Slot");
        Panel(root, "Frame", Vector3.zero,              new Vector3(1.10f, 0.10f, 1.00f), "Mat_SlotFrame");
        Panel(root, "Inner", new Vector3(0, 0.05f, 0),  new Vector3(0.88f, 0.08f, 0.80f), "Mat_SlotInner");

        Save(root, path);
    }

    // ── Low-level helpers ──────────────────────────────────────────────────

    static GameObject Prim(PrimitiveType type, string name, GameObject parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        var col = go.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);
        if (parent != null) go.transform.SetParent(parent.transform);
        return go;
    }

    static void Panel(GameObject parent, string name, Vector3 localPos, Vector3 scale, string matName)
    {
        Prim(PrimitiveType.Cube, name, parent, localPos, scale, M(matName));
    }

    static GameObject Marker(GameObject parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        return go;
    }

    static bool Exists(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

    static void Save(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
