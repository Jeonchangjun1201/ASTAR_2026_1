using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using _TeamFolder.JCJ.Battle;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Editor
{
public static class BattleWeaponFirstPersonPoseAutoTuner
{
    private const string MenuPath = "JCJ/Battle/Auto-Tune First Person Weapon Poses (Play Mode)";

    [MenuItem(MenuPath)]
    private static void Run()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[JCJ Battle] 플레이 모드에서 배틀이 실행 중일 때 메뉴를 실행하세요.");
            return;
        }

        var manager = ResolveLocalWeaponManager();
        if (manager == null)
        {
            Debug.LogError("[JCJ Battle] 로컬 BattleWeaponManager를 찾지 못했습니다.");
            return;
        }

        var catalog = AssetDatabase.LoadAssetAtPath<BattleWeaponCatalog>("Assets/#TeamFolder/JCJ/Data/Battle/Catalogs/BattleWeaponCatalog.asset");
        if (catalog == null)
        {
            Debug.LogError("[JCJ Battle] BattleWeaponCatalog 에셋을 찾지 못했습니다.");
            return;
        }

        IReadOnlyList<BattleWeaponDefinition> weapons = catalog.GetAllWeaponDefinitionsDistinct();
        if (weapons.Count == 0)
        {
            Debug.LogError("[JCJ Battle] 총정의가 없습니다.");
            return;
        }

        var mgrSo = new SerializedObject(manager);
        Vector3 mountPos = mgrSo.FindProperty("_weaponMountLocalPosition").vector3Value;
        Vector3 mountEuler = mgrSo.FindProperty("_weaponMountLocalEulerAngles").vector3Value;

        int ok = 0;
        for (int i = 0; i < weapons.Count; i++)
        {
            BattleWeaponDefinition def = weapons[i];
            if (def == null) continue;

            ForceSoUseDefaultMount(def);
            manager.EquipWeapon(def);

            Transform mountTf = manager.transform.Find("WeaponMount");
            if (mountTf == null) continue;
            Transform weaponTf = FindWeaponRootChild(mountTf);
            if (weaponTf == null) continue;

            weaponTf.localPosition = Vector3.zero;
            weaponTf.localRotation = Quaternion.identity;
            weaponTf.localScale = Vector3.one;

            Bounds? boundsMountLocal = ComputeRendererBoundsMountLocal(mountTf, weaponTf);
            if (!boundsMountLocal.HasValue) continue;

            Bounds b = boundsMountLocal.Value;
            float targetMaxZ = 0.2f;
            float dz = targetMaxZ - b.max.z;
            dz = Mathf.Clamp(dz, -0.14f, 0.32f);
            float tx = Mathf.Clamp(-b.center.x * 0.48f + 0.078f, 0.02f, 0.3f);
            float ty = Mathf.Clamp(-b.center.y - b.extents.y * 0.38f - 0.045f, -0.42f, -0.04f);
            Vector3 viewPos = new Vector3(tx, ty, dz);

            SerializedObject so = new SerializedObject(def);
            Vector3 prevEuler = so.FindProperty("_viewLocalEuler").vector3Value;
            Vector3 prevScale = so.FindProperty("_viewLocalScale").vector3Value;

            so.FindProperty("_viewLocalPosition").vector3Value = viewPos;
            so.FindProperty("_viewLocalEuler").vector3Value = prevEuler;
            so.FindProperty("_viewLocalScale").vector3Value = prevScale;
            so.FindProperty("_useCustomMountPose").boolValue = true;
            so.FindProperty("_mountLocalPosition").vector3Value = mountPos;
            so.FindProperty("_mountLocalEulerAngles").vector3Value = mountEuler;
            so.ApplyModifiedPropertiesWithoutUndo();

            manager.EquipWeapon(def);
            Debug.Log("[JCJ Battle] " + def.WeaponId + " view=" + viewPos + " mount=" + mountPos);
            ok++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[JCJ Battle] 무기 1인칭 포즈 SO 반영 완료: " + ok + "개");
    }

    private static BattleWeaponManager ResolveLocalWeaponManager()
    {
        RuntimePlayerIdentity[] ids = Object.FindObjectsByType<RuntimePlayerIdentity>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < ids.Length; i++)
        {
            if (!ids[i].IsLocalOwned) continue;
            BattleWeaponManager m = ids[i].GetComponent<BattleWeaponManager>();
            if (m != null) return m;
        }

        return null;
    }

    private static Transform FindWeaponRootChild(Transform mountTf)
    {
        if (mountTf.childCount == 0) return null;
        return mountTf.GetChild(0);
    }

    private static Bounds? ComputeRendererBoundsMountLocal(Transform mountTf, Transform weaponRoot)
    {
        Renderer[] renderers = weaponRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return null;

        bool init = false;
        Bounds acc = default;
        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer ren = renderers[r];
            if (ren == null || !ren.enabled) continue;
            Bounds wb = ren.bounds;
            EncapsulateWorldBoundsAsMountLocal(mountTf, ref init, ref acc, wb);
        }

        if (!init) return null;
        return acc;
    }

    private static void EncapsulateWorldBoundsAsMountLocal(Transform mountTf, ref bool init, ref Bounds acc, Bounds worldBounds)
    {
        Vector3[] c =
        {
            new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z),
            new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z),
            new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z),
            new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z),
            new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z),
            new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z),
            new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z),
            new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z)
        };

        for (int i = 0; i < c.Length; i++)
        {
            Vector3 lp = mountTf.InverseTransformPoint(c[i]);
            if (!init)
            {
                acc = new Bounds(lp, Vector3.zero);
                init = true;
            }
            else
            {
                acc.Encapsulate(lp);
            }
        }
    }

    private static void ForceSoUseDefaultMount(BattleWeaponDefinition def)
    {
        SerializedObject so = new SerializedObject(def);
        so.FindProperty("_useCustomMountPose").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
}
