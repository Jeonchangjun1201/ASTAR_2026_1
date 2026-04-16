using System;
using UnityEngine;
namespace BFS
{
    public class FSMonitorScreen : MonoBehaviour, IFSScreen
    {
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            ChangeScreenColor(Color.black);
        }
        public void ChangeScreenColor(Color color)               // Gets color as parameter, then changes its material(color) equal to that
        {
            _meshRenderer.material.color = color;
        }
        public void ResetScreenColor()                           // Sets its color to black
        {
            _meshRenderer.material.color = Color.black;
        }
    }
}
