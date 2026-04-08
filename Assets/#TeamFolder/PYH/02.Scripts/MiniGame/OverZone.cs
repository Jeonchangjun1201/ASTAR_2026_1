using UnityEngine;

namespace PYH.MiniGame
{
    using PYH.Player;

    [RequireComponent(typeof(Collider))]
    public class OverZone : MonoBehaviour
    {
        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out Player player))
            {
                player.OverPlayer();
            }
        }
    }
}
