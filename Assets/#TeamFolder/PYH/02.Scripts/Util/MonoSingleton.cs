using UnityEngine;

namespace PYH.Util
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance = null;

        // To be implemented. . .
    }
}