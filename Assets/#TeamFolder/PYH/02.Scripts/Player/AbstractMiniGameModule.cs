using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.Player
{
    public class AbstractMiniGameModule : MonoBehaviour
    {
        public int Index;
        
        public void DelPlayer()
        {
            Debug.Log($"Player {gameObject.name} Is Dead ");
            gameObject.SetActive(false);
        }
    }
}