using UnityEngine;

namespace KSY.Utility
{
    public static class AddClass
    {
        public static Vector3 Multiplication(this Vector3 vec, Vector3 dir)
        {
            return new Vector3()
            {
                x = vec.x * dir.x,
                y = vec.y * dir.y,
                z = vec.z * dir.z
            };
        }

        public static bool TryGetComponentInChildren<T>(this Component component, out T value)
        {
            value = component.GetComponentInChildren<T>();
            return value != null;
        }

        public static bool TryGetComponentsInChildren<T>(this Component component, out T[] value)
        {
            value = component.GetComponentsInChildren<T>();
            return value != null && value.Length > 0;
        }

        public static void AddFloat(this Material material, string name, float value) => material.AddFloat(Shader.PropertyToID(name), value);
        public static void AddFloat(this Material material, int nameId, float value)
        {
            float matValue = material.GetFloat(nameId);
            material.SetFloat(nameId, matValue + value);
        }
    }
}