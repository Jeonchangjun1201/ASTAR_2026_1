using System;
using UnityEngine;
namespace GDH
{
    public class FSMonitorScreen : MonoBehaviour, IFSScreen
    {
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            ChangeScreenColor(Color.black);
        }
        public void ChangeScreenColor(Color color)
        {
            _meshRenderer.material.color = color;
        }
        public void ResetScreenColor()
        {
            _meshRenderer.material.color = Color.black;
        }
    }
}
