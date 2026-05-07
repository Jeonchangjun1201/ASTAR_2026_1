using System;
using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Battle
{
    [CreateAssetMenu(menuName = "JCJ/Battle/Weapon Catalog", fileName = "BattleWeaponCatalog")]
    public class BattleWeaponCatalog : ScriptableObject
    {
        [Serializable]
        public class GradeLoadout
        {
            public BattleWeaponGrade grade;
            public BattleWeaponDefinition[] weapons;
        }

        [SerializeField] private GradeLoadout[] _loadouts;

        public IReadOnlyList<GradeLoadout> Loadouts => _loadouts;

        public BattleWeaponDefinition[] GetWeapons(BattleWeaponGrade grade)
        {
            if (_loadouts == null) return Array.Empty<BattleWeaponDefinition>();
            for (int i = 0; i < _loadouts.Length; i++)
            {
                var loadout = _loadouts[i];
                if (loadout == null || loadout.grade != grade || loadout.weapons == null) continue;
                var candidates = new List<BattleWeaponDefinition>(loadout.weapons.Length);
                for (int j = 0; j < loadout.weapons.Length; j++)
                {
                    if (loadout.weapons[j] != null) candidates.Add(loadout.weapons[j]);
                }

                return candidates.ToArray();
            }

            return Array.Empty<BattleWeaponDefinition>();
        }

        public BattleWeaponDefinition GetRandomWeapon(BattleWeaponGrade grade)
        {
            var candidates = GetWeapons(grade);
            if (candidates.Length == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Length)];
        }

        public static BattleWeaponGrade RankToGrade(int rank)
        {
            return rank switch
            {
                1 => BattleWeaponGrade.Rank1,
                2 => BattleWeaponGrade.Rank2,
                3 => BattleWeaponGrade.Rank3,
                _ => BattleWeaponGrade.Rank4
            };
        }
    }
}
