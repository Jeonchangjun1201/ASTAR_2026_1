using UnityEngine;

namespace KSY.Servers
{
    [CreateAssetMenu(fileName = "ListeningInfo", menuName = "KSY/SO/ListeningInfo")]
    public class ListeningDataSO : ScriptableObject
    {
        [field : SerializeField] public int Port {get; private set;}
    }
}

