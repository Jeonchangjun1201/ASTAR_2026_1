using System.Collections.Generic;
using UnityEngine;
namespace BFS
{
    public class FSScreenManager : MonoBehaviour
    {
        public Dictionary<PlateColor, IFSScreen> ScreenDict { get; private set; } = new Dictionary<PlateColor, IFSScreen>();

        private void Awake()
        {
            IFSScreen[] list = GetComponentsInChildren<IFSScreen>();
            foreach (IFSScreen l in list)
            {
                ScreenDict.Add(l.GivenColor, l);
            }
        }

        public void ChangeScreenColor(PlateColor screen) // Method to change monitor screen color // 모니터 화면 색을 변경하는 메서드
        {
            Color color = new Color();
            switch (screen)
            {
                case PlateColor.RED:
                    color = Color.red;
                    break;
                case PlateColor.GREEN:
                    color = Color.green;
                    break;
                case PlateColor.BLUE:
                    color = Color.blue;
                    break;
                case PlateColor.YELLOW:
                    color = Color.yellow;
                    break;
                default:
                    throw new System.ArgumentException("INVALID TYPE");
            }
            ScreenDict[screen].ChangeScreenColor(color);
        }

        public void ResetScreenColor()
        {
            foreach(PlateColor k in ScreenDict.Keys)
            {
                ScreenDict[k].ResetScreenColor();
            }
        }
    }
}
