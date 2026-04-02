using UnityEngine;
namespace GDH
{
    public class FSGameManager : MonoBehaviour
    {
        [SerializeField] PlayerMovementBFS bfs;
        [SerializeField] PlayerBFS bfs2;

        private void Awake()
        {
            bfs.Initialize(bfs2.transform, bfs2.GetComponent<CharacterController>());
        }
    }
}
