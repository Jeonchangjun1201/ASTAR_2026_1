using UnityEngine;
using UnityEditor;

public class DeathmatchArenaBuilder : EditorWindow
{
    [MenuItem("Tools/Build Deathmatch Arena")]
    static void BuildArena()
    {
        var arena = GameObject.Find("DeathmatchArena");
        if (arena == null)
        {
            arena = new GameObject("DeathmatchArena");
            arena.transform.position = Vector3.zero;
        }

        var existingBuilding = arena.transform.Find("CenterBuilding");
        if (existingBuilding != null)
            DestroyImmediate(existingBuilding.gameObject);

        while (arena.transform.childCount > 0)
            DestroyImmediate(arena.transform.GetChild(0).gameObject);

        string prefabRoot = "Assets/LowPolyFPSLite/Prefabs/";
        GameObject wall01 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Wall_01.prefab");
        GameObject wall02 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Wall_02.prefab");
        GameObject wall03 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Wall_03.prefab");
        GameObject wall04 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Wall_04.prefab");
        GameObject wall05 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Wall_05.prefab");
        GameObject wall06 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Wall_06.prefab");
        GameObject wallPart = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "WallPart_01.prefab");
        GameObject building = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Building_01.prefab");
        GameObject box = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Box_01.prefab");
        GameObject brick = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Brick_01.prefab");
        GameObject jug01 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Jug_01.prefab");
        GameObject jug02 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "Jug_02.prefab");
        GameObject plank01 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "WoodPlank_01.prefab");
        GameObject plank02 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "WoodPlank_02.prefab");
        GameObject plank03 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRoot + "WoodPlank_03.prefab");

        Transform t = arena.transform;

        Spawn(building, "CenterBuilding", t, new Vector3(0, 0, 0), new Vector3(0, 45, 0), Vector3.one * 1.3f);

        float edge = 20f;
        float wallWidth = 4f;
        Vector3 wallScale = new Vector3(2.5f, 1.5f, 1f);
        float scaledWidth = wallWidth * wallScale.x;

        for (int i = 0; i < 4; i++)
        {
            float offset = -edge + scaledWidth * 0.5f + i * scaledWidth;
            Spawn(wall01, $"NorthWall_{i}", t, new Vector3(offset, 0, edge), Vector3.zero, wallScale);
            Spawn(wall01, $"SouthWall_{i}", t, new Vector3(-offset, 0, -edge), new Vector3(0, 180, 0), wallScale);
            Spawn(wall01, $"EastWall_{i}", t, new Vector3(edge, 0, -offset), new Vector3(0, 90, 0), wallScale);
            Spawn(wall01, $"WestWall_{i}", t, new Vector3(-edge, 0, offset), new Vector3(0, 270, 0), wallScale);
        }

        Spawn(wall03, "NW_Corner", t, new Vector3(-edge, 0, edge), new Vector3(0, 315, 0), new Vector3(1.5f, 1.5f, 1f));
        Spawn(wall03, "NE_Corner", t, new Vector3(edge, 0, edge), new Vector3(0, 45, 0), new Vector3(1.5f, 1.5f, 1f));
        Spawn(wall03, "SE_Corner", t, new Vector3(edge, 0, -edge), new Vector3(0, 135, 0), new Vector3(1.5f, 1.5f, 1f));
        Spawn(wall03, "SW_Corner", t, new Vector3(-edge, 0, -edge), new Vector3(0, 225, 0), new Vector3(1.5f, 1.5f, 1f));

        Spawn(wall02, "NW_InnerWall_A", t, new Vector3(-8, 0, 12), new Vector3(0, 20, 0), new Vector3(1.8f, 1.2f, 1f));
        Spawn(wall04, "NW_InnerWall_B", t, new Vector3(-14, 0, 8), new Vector3(0, 90, 0), new Vector3(1.5f, 1f, 1f));
        Spawn(wall05, "NW_CoverWall", t, new Vector3(-11, 0, 14), new Vector3(0, 0, 0), new Vector3(1.2f, 1f, 1f));
        Spawn(building, "NW_Ruin", t, new Vector3(-15, 0, 15), new Vector3(0, 30, 0), Vector3.one * 0.8f);
        Spawn(brick, "NW_Rubble_A", t, new Vector3(-9, 0, 16), new Vector3(0, 45, 0), Vector3.one * 1.5f);
        Spawn(brick, "NW_Rubble_B", t, new Vector3(-13, 0, 11), new Vector3(0, 120, 0), Vector3.one * 1.2f);
        Spawn(box, "NW_Crate", t, new Vector3(-6, 0, 10), new Vector3(0, 15, 0), Vector3.one * 1.3f);
        Spawn(jug01, "NW_Jug", t, new Vector3(-17, 0, 17), Vector3.zero, Vector3.one);
        Spawn(plank01, "NW_Plank", t, new Vector3(-12, 0, 16), new Vector3(0, 60, 0), Vector3.one * 1.5f);

        Spawn(wall02, "NE_InnerWall_A", t, new Vector3(10, 0, 14), new Vector3(0, 90, 0), new Vector3(2f, 1.2f, 1f));
        Spawn(wall06, "NE_InnerWall_B", t, new Vector3(14, 0, 10), new Vector3(0, 0, 0), new Vector3(1.5f, 1f, 1f));
        Spawn(wall04, "NE_Corridor", t, new Vector3(12, 0, 6), new Vector3(0, 90, 0), new Vector3(1.8f, 1f, 1f));
        Spawn(box, "NE_CrateStack", t, new Vector3(15, 0, 13), new Vector3(0, 55, 0), Vector3.one * 1.4f);
        Spawn(box, "NE_Crate_A", t, new Vector3(8, 0, 16), new Vector3(0, 30, 0), Vector3.one * 1.2f);
        Spawn(box, "NE_Crate_B", t, new Vector3(16, 0, 8), new Vector3(0, 75, 0), Vector3.one);
        Spawn(plank02, "NE_Debris", t, new Vector3(17, 0, 15), new Vector3(0, 45, 0), Vector3.one * 1.3f);
        Spawn(jug02, "NE_Jug", t, new Vector3(9, 0, 11), Vector3.zero, Vector3.one * 0.9f);

        Spawn(wall02, "SW_AlleyWall_A", t, new Vector3(-12, 0, -8), new Vector3(0, 90, 0), new Vector3(2f, 1.3f, 1f));
        Spawn(wall05, "SW_AlleyWall_B", t, new Vector3(-8, 0, -12), new Vector3(0, 0, 0), new Vector3(1.8f, 1.3f, 1f));
        Spawn(wall01, "SW_AlleyWall_C", t, new Vector3(-14, 0, -14), new Vector3(0, 45, 0), new Vector3(1.5f, 1.2f, 1f));
        Spawn(wall06, "SW_CoverWall", t, new Vector3(-10, 0, -16), new Vector3(0, 0, 0), new Vector3(1.2f, 0.8f, 1f));
        Spawn(box, "SW_Crate_A", t, new Vector3(-6, 0, -14), new Vector3(0, 60, 0), Vector3.one * 1.4f);
        Spawn(box, "SW_Crate_B", t, new Vector3(-16, 0, -10), new Vector3(0, 22, 0), Vector3.one * 1.1f);
        Spawn(brick, "SW_Rubble", t, new Vector3(-15, 0, -16), new Vector3(0, 90, 0), Vector3.one * 1.3f);
        Spawn(plank03, "SW_Plank", t, new Vector3(-9, 0, -17), new Vector3(0, 135, 0), Vector3.one * 1.4f);

        Spawn(wall04, "SE_Wall_A", t, new Vector3(10, 0, -12), new Vector3(0, 0, 0), new Vector3(1.6f, 1f, 1f));
        Spawn(wall03, "SE_Wall_B", t, new Vector3(14, 0, -8), new Vector3(0, 90, 0), new Vector3(1.4f, 1f, 1f));
        Spawn(brick, "SE_BrickPile", t, new Vector3(15, 0, -15), new Vector3(0, 110, 0), Vector3.one * 1.5f);
        Spawn(wall06, "SE_LowWall", t, new Vector3(8, 0, -16), new Vector3(0, 0, 0), new Vector3(1.5f, 0.7f, 1f));
        Spawn(box, "SE_Crate", t, new Vector3(12, 0, -17), new Vector3(0, 50, 0), Vector3.one * 1.2f);
        Spawn(jug02, "SE_Jug", t, new Vector3(17, 0, -17), Vector3.zero, Vector3.one * 1.1f);
        Spawn(plank01, "SE_Debris", t, new Vector3(16, 0, -12), new Vector3(0, 70, 0), Vector3.one * 1.2f);
        Spawn(brick, "SE_Rubble", t, new Vector3(11, 0, -14), new Vector3(0, 160, 0), Vector3.one * 1.1f);

        Spawn(wall01, "Mid_NW_Diagonal", t, new Vector3(-5, 0, 5), new Vector3(0, 45, 0), new Vector3(1.3f, 1f, 1f));
        Spawn(wall01, "Mid_SE_Diagonal", t, new Vector3(5, 0, -5), new Vector3(0, 45, 0), new Vector3(1.3f, 1f, 1f));
        Spawn(wall02, "Mid_NE_Diagonal", t, new Vector3(5, 0, 5), new Vector3(0, 135, 0), new Vector3(1.3f, 1f, 1f));
        Spawn(wall02, "Mid_SW_Diagonal", t, new Vector3(-5, 0, -5), new Vector3(0, 135, 0), new Vector3(1.3f, 1f, 1f));

        Spawn(box, "Mid_East_Cover", t, new Vector3(3.5f, 0, 0), new Vector3(0, 10, 0), Vector3.one * 1.2f);
        Spawn(box, "Mid_West_Cover", t, new Vector3(-3.5f, 0, 0), new Vector3(0, 55, 0), Vector3.one * 1.2f);
        Spawn(brick, "Mid_North_Rubble", t, new Vector3(0, 0, 7), new Vector3(0, 30, 0), Vector3.one * 1.3f);
        Spawn(brick, "Mid_South_Rubble", t, new Vector3(0, 0, -7), new Vector3(0, 210, 0), Vector3.one * 1.3f);

        Spawn(wallPart, "Mid_N_Cover", t, new Vector3(-3, 0, 10), new Vector3(0, 0, 0), new Vector3(1.5f, 1f, 1f));
        Spawn(wallPart, "Mid_S_Cover", t, new Vector3(3, 0, -10), new Vector3(0, 180, 0), new Vector3(1.5f, 1f, 1f));
        Spawn(wallPart, "Mid_E_Cover", t, new Vector3(10, 0, 3), new Vector3(0, 90, 0), new Vector3(1.5f, 1f, 1f));
        Spawn(wallPart, "Mid_W_Cover", t, new Vector3(-10, 0, -3), new Vector3(0, 270, 0), new Vector3(1.5f, 1f, 1f));

        Spawn(box, "Detail_Box_NE", t, new Vector3(3, 0, 14), new Vector3(0, 40, 0), Vector3.one);
        Spawn(box, "Detail_Box_SW", t, new Vector3(-3, 0, -14), new Vector3(0, 220, 0), Vector3.one);
        Spawn(plank02, "Detail_Plank_N", t, new Vector3(6, 0, 18), new Vector3(0, 15, 0), Vector3.one * 1.2f);
        Spawn(plank03, "Detail_Plank_S", t, new Vector3(-6, 0, -18), new Vector3(0, 195, 0), Vector3.one * 1.2f);
        Spawn(plank01, "Detail_Plank_E", t, new Vector3(18, 0, -4), new Vector3(0, 90, 0), Vector3.one);
        Spawn(plank02, "Detail_Plank_W", t, new Vector3(-18, 0, 4), new Vector3(0, 270, 0), Vector3.one);
        Spawn(jug01, "Detail_Jug_Center_N", t, new Vector3(2, 0, 4), Vector3.zero, Vector3.one * 0.8f);
        Spawn(jug02, "Detail_Jug_Center_S", t, new Vector3(-2, 0, -4), Vector3.zero, Vector3.one * 0.8f);
        Spawn(brick, "Detail_Brick_NE", t, new Vector3(14, 0, 2), new Vector3(0, 55, 0), Vector3.one);
        Spawn(brick, "Detail_Brick_SW", t, new Vector3(-14, 0, -2), new Vector3(0, 235, 0), Vector3.one);

        Spawn(wall05, "GatePost_N_Left", t, new Vector3(-2.5f, 0, 20), Vector3.zero, new Vector3(0.6f, 1.5f, 2f));
        Spawn(wall05, "GatePost_N_Right", t, new Vector3(2.5f, 0, 20), Vector3.zero, new Vector3(0.6f, 1.5f, 2f));
        Spawn(wall05, "GatePost_S_Left", t, new Vector3(-2.5f, 0, -20), new Vector3(0, 180, 0), new Vector3(0.6f, 1.5f, 2f));
        Spawn(wall05, "GatePost_S_Right", t, new Vector3(2.5f, 0, -20), new Vector3(0, 180, 0), new Vector3(0.6f, 1.5f, 2f));
        Spawn(wall05, "GatePost_E_Left", t, new Vector3(20, 0, 2.5f), new Vector3(0, 90, 0), new Vector3(0.6f, 1.5f, 2f));
        Spawn(wall05, "GatePost_E_Right", t, new Vector3(20, 0, -2.5f), new Vector3(0, 90, 0), new Vector3(0.6f, 1.5f, 2f));
        Spawn(wall05, "GatePost_W_Left", t, new Vector3(-20, 0, -2.5f), new Vector3(0, 270, 0), new Vector3(0.6f, 1.5f, 2f));
        Spawn(wall05, "GatePost_W_Right", t, new Vector3(-20, 0, 2.5f), new Vector3(0, 270, 0), new Vector3(0.6f, 1.5f, 2f));

        Undo.RegisterCreatedObjectUndo(arena, "Build Deathmatch Arena");
        EditorUtility.SetDirty(arena);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"Deathmatch Arena built with {arena.transform.childCount} objects.");
    }

    static GameObject Spawn(GameObject prefab, string name, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale)
    {
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = rot;
        go.transform.localScale = scale;
        go.isStatic = true;
        return go;
    }
}