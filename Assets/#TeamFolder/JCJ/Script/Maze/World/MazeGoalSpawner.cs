using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골 프리팹과 골 관련 부가 컴포넌트를 생성하는 스포너 계약.
    /// </summary>
    public interface IMazeGoalSpawner
    {
        GameObject Spawn(Vector2Int cell, float cellSize, GameObject goalPrefab,
                        GoalAudioSettings audioSettings, IRankService rankService,
                        Color beaconColor, Transform parent);
    }

    /// <summary>
    /// 골 프리팹을 생성하고 GoalTrigger, 힌트 사운드, 비콘 같은 필수 컴포넌트가 한 번씩만 붙도록 보장한다.
    /// </summary>
    public class MazeGoalSpawner : MonoBehaviour, IMazeGoalSpawner
    {
        public GameObject Spawn(Vector2Int cell, float cellSize, GameObject goalPrefab,
                               GoalAudioSettings audioSettings, IRankService rankService,
                               Color beaconColor, Transform parent)
        {
            if (goalPrefab == null)
            {
                Debug.LogWarning("[MazeGoalSpawner] goalPrefab is null.");
                return null;
            }

            Vector3 pos = new(cell.x * cellSize, 0f, cell.y * cellSize);
            var goal = Instantiate(goalPrefab, pos, Quaternion.identity, parent);

            var trigger = goal.GetComponent<GoalTrigger>() ?? goal.AddComponent<GoalTrigger>();
            trigger.Inject(rankService);

            if (goal.GetComponent<AudioSource>() == null) goal.AddComponent<AudioSource>();
            var hint = goal.GetComponent<GoalAudioHint>() ?? goal.AddComponent<GoalAudioHint>();
            if (audioSettings != null)
            {
                hint.Inject(audioSettings);
                hint.StartHint();
            }

            var beacon = goal.GetComponent<GoalBeacon>() ?? goal.AddComponent<GoalBeacon>();
            beacon.Build(beaconColor);

            return goal;
        }
    }
}
