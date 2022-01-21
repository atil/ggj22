using JamKit;
using UnityEngine;

namespace Game
{
    public class GameUi : UiBase
    {
        [SerializeField] private FlashInfo _openFlashInfo;
        
        void Start()
        {
            Flash(_openFlashInfo);
        }
    }
}