using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using csiimnida.CSILib.SoundManager.RunTime;
using TMPro;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class TitleGuideUiControlHub : PopupUi
    {
        [SerializeField] private CanvasGroup canvas;
        
        [Header("Buttons")]
        [SerializeField] private GuideButton[] buttons;
        [SerializeField] private Transform btnPoint;
        [SerializeField] private GameObject btnPrefab;

        [SerializeField] private GuideUiSO[] guideUiData;

        [Header("Info")]
        [SerializeField] private TMP_Text contentLabel;

        [SerializeField] private string defaultMessage;

        private void Awake()
        {
            contentLabel.text = defaultMessage;
            
            buttons = new GuideButton[guideUiData.Length];
            
            for (int i = 0; i < guideUiData.Length; i++)
            {
                GuideButton btn = buttons[i] = Instantiate(btnPrefab, btnPoint).GetComponent<GuideButton>();
                
                btn.Initialize(guideUiData[i]);
                btn.OnButtonClickEvent += ShowContent;
            }
            
            AStarEventBus.Subscribe<GuideUiEvent>(InteractGuide);
        }
        private void OnDestroy()
        {
            foreach (GuideButton button in buttons)
            {
                button.OnButtonClickEvent -= ShowContent;
            }
            
            AStarEventBus.Unsubscribe<GuideUiEvent>(InteractGuide);
        }

        private void ShowContent(GuideUiSO guideSo)
        {
            contentLabel.text = guideSo.MiniGameInfo;
        }

        public override bool InteractPopup() // don't use on button => on click event
        {
            IsOpen = !IsOpen;
            
            canvas.interactable = IsOpen;
            canvas.blocksRaycasts = IsOpen;
            canvas.alpha = IsOpen ? 1 : 0;
            
            return IsOpen;
        }

        public void InteractGuide()
        {
            SoundManager.Instance.PlaySound("General-Ui_Click");
            AStarEventBus.Publish(new UiInteractEvent(this));
        }
        private void InteractGuide(GuideUiEvent @event)
        {
            AStarEventBus.Publish(new UiInteractEvent(this));
        }
    }
}
