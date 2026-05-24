using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.KDH._01.Code.Map
{
    public class ChangeMap : MonoBehaviour
    {
        [Header("맵 설정")]
        [SerializeField] private GameObject currentMap;  // 현재 맵
        [SerializeField] private GameObject nextMap;     // 다음 맵
        [SerializeField] private float mapChangeDelay = 2f; // 몇 초 후 전환

        public static Action<string> OnPlayerOut;

        private List<string> _alivePlayers = new List<string>
        {
            "Player1", "Player2", "Player3", "Player4"
        };

        private void OnEnable()  => OnPlayerOut += HandlePlayerOut;
        private void OnDisable() => OnPlayerOut -= HandlePlayerOut;

        private void HandlePlayerOut(string playerName)
        {
            _alivePlayers.Remove(playerName);

            if (_alivePlayers.Count == 1)
            {
                
                StartCoroutine(SwitchMap());
            }
            else if (_alivePlayers.Count == 0)
            {
                StartCoroutine(SwitchMap());
            }
        }
        private IEnumerator SwitchMap()
        {
            yield return new WaitForSeconds(mapChangeDelay);

            if (currentMap != null) currentMap.SetActive(false);
            if (nextMap != null)    nextMap.SetActive(true);
        }
        
    }
    
}