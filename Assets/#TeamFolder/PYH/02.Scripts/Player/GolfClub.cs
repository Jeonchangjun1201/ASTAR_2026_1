using UnityEngine;

namespace PYH.Player
{
    public class GolfClub : MonoBehaviour
    {
        [SerializeField] private float _power;
        [SerializeField] private Player _owner;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out Player player))
            {
                if (player != _owner)
                {
                    player.Push(_owner.gameObject.transform.forward, _power);
                    Debug.Log("PUSH!");
                }
            }
        }
    }
}
