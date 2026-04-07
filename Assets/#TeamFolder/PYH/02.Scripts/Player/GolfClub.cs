using UnityEngine;
using UnityEngine.InputSystem;

namespace PYH.Player
{
    public class GolfClub : MonoBehaviour
    {
        [SerializeField] private float _maxPower;
        [SerializeField] private float _powerMultpler;
        [SerializeField] private Player _owner;

        [SerializeField] private float _perPower;

        private void Update()
        {
            if (Mouse.current.leftButton.isPressed)
            {
                _perPower = Mathf.Clamp((_perPower + (1 * _powerMultpler) * Time.deltaTime), 0, 100);
            }
            else
            {
                _perPower = 0;
                Debug.Log("Swing!");
            }
        }
    }
}
