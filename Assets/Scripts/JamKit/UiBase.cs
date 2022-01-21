using System;
using UnityEngine;
using UnityEngine.UI;

namespace JamKit
{
    [Serializable]
    public class FlashInfo
    {
        [SerializeField] private Color _startColor = default;
        public Color StartColor => _startColor;

        [SerializeField] private Color _endColor = default;
        public Color EndColor => _endColor;

        [SerializeField] private float _duration = default;
        public float Duration => _duration;

        [SerializeField] private AnimationCurve _curve = default;
        public AnimationCurve Curve => _curve;
    }

    public abstract class UiBase : MonoBehaviour
    {
        [SerializeField] private Image _coverImage = default;

        protected void Flash(FlashInfo flashInfo)
        {
            Flash(flashInfo, null);
        }
        
        protected void Flash(FlashInfo flashInfo, Action postAction)
        {
            Curve.Tween(flashInfo.Curve,
                flashInfo.Duration,
                t => { _coverImage.color = Color.Lerp(flashInfo.StartColor, flashInfo.EndColor, t); },
                () =>
                {
                    _coverImage.color = flashInfo.EndColor;
                    postAction?.Invoke();
                });
            
        }
    }
}