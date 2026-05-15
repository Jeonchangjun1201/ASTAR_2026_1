using UnityEngine;
using _TeamFolder.JCJ.Battle;

namespace _TeamFolder.JCJ.Battle.Session
{
    public static class BattlePlayerViewLayers
    {
        public static void ApplyLocalThirdPersonBodyLayersToPlayer(GameObject playerRoot)
        {
            if (playerRoot == null) return;
            int defaultLayer = 0;
            var renderers = playerRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r is LineRenderer) continue;
                r.gameObject.layer = defaultLayer;
            }
        }

        public static void ApplyLocalFirstPersonBodyLayersToPlayer(GameObject playerRoot)
        {
            if (playerRoot == null) return;
            int lb = LayerMask.NameToLayer("BattleLocalBody");
            if (lb < 0) return;
            Transform camT = null;
            var fpc = BattleFirstPersonCamera.Instance;
            if (fpc != null) camT = fpc.transform;
            Transform weaponMount = playerRoot.transform.Find("WeaponMount");
            int defaultLayer = 0;
            var renderers = playerRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r is LineRenderer) continue;
                if (camT != null && (r.transform == camT || r.transform.IsChildOf(camT))) continue;
                if (weaponMount != null && (r.transform == weaponMount || r.transform.IsChildOf(weaponMount)))
                {
                    r.gameObject.layer = defaultLayer;
                    continue;
                }

                r.gameObject.layer = lb;
            }
        }
    }
}
