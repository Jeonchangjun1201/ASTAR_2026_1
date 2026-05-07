using System;
using UnityEngine;

namespace KDH
{
    public class FallZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider collision)
        {
            TopManager top = collision.GetComponent<TopManager>();
            if (top == null) return;

            top.NotifyFallen();
        }           
    }
}