using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.MiniGame
{
    [RequireComponent(typeof(Collider))]
    public class OverZone : MonoBehaviour
    {
        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out Player.Player player))
            {
                player.OverPlayer();
            }
        }
    }
}
