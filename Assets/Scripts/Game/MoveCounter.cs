using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    public class MoveCounter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI _inputCountText;
        [SerializeField] private GameObject _restartButton;
        [SerializeField] private GameObject _successIcon;

        public void InitLevel()
        {
            _restartButton.SetActive(false);
            _successIcon.SetActive(false);
        }

        public void SetCounter(int counter)
        {
            _inputCountText.gameObject.SetActive(true);
            _inputCountText.text = counter.ToString();
        }

        public void OnTokenClicked(int currentLevelInputLimit, int currentLevelInputCount, bool isLevelCompleted)
        {
            _inputCountText.text = (currentLevelInputLimit - currentLevelInputCount).ToString();
            _inputCountText.gameObject.SetActive(!isLevelCompleted && currentLevelInputLimit > currentLevelInputCount);
            _successIcon.gameObject.SetActive(isLevelCompleted);
            _restartButton.SetActive(!isLevelCompleted && currentLevelInputLimit <= currentLevelInputCount);
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}
