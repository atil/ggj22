using JamKit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    public class SplashUi : UiBase
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private FlashInfo _openFlashInfo;
        [SerializeField] private FlashInfo _closeFlashInfo;

        void Start()
        {
            Flash(_openFlashInfo);
            Sfx.Instance.StartMusic("Music", true);
        }
        
        public void OnClickedPlayButton()
        {
            _playButton.interactable = false;
            Sfx.Instance.Play("ClickButton");
            Flash(_closeFlashInfo, () => SceneManager.LoadScene("Game"));
        }
    }
}