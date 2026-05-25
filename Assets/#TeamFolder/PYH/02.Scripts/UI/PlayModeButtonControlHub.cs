using _TeamFolder.PYH._02.Scripts.Data;
using Assets._TeamFolder.PYH._02.Scripts.Enum;
using Assets._TeamFolder.PYH._02.Scripts.UI.Event;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

public class PlayModeButtonControlHub : MonoBehaviour
{
    public void OnClickHost()
    {
        SoundManager.Instance.PlaySound("General-Ui_Click");
        AStarEventBus.Publish(new PlayModeSelectUiEvent(PlayModeEnum.HOST));
    }
    public void OnClickJoin()
    {
        SoundManager.Instance.PlaySound("General-Ui_Click");
        AStarEventBus.Publish(new PlayModeSelectUiEvent(PlayModeEnum.JOIN));
    }
}
