using UnityEngine;

namespace JamKit
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _playerMotor = default;

        [SerializeField] private MouseLook _mouseLook = default;

        private void Update()
        {
            float dt = Time.deltaTime;
            _mouseLook.Tick(dt);
            _playerMotor.Tick(dt);
        }

        public void ResetAt(Transform t)
        {
            _mouseLook.ResetAt(t, null);
            _playerMotor.ResetAt(t);
        }
    }
}