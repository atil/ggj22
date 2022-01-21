using System.Collections;
using UnityEngine;

namespace JamKit
{
    public class MouseLook : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera = default;

        [SerializeField] private Camera _weaponCamera = default;

        [SerializeField] private AnimationCurve _fovKickCurve = default;

        [Header("Config")] [SerializeField] private bool _isInverted = false;

        [SerializeField] private bool _zoomEnabled = true;

        [SerializeField] private int _sensitivityMax = 300;

        [SerializeField] private int _sensitivityMin = 50;

        [SerializeField] private int _fovMax = 110;

        [SerializeField] private int _fovMin = 60;

        [SerializeField] private float _sensitivity = 150;

        [SerializeField] private float _zoomSensitivity = 30;

        private float _fov = 60;
        private const float ZoomFov = 10;
        private const float ZoomSpeed = 40;

        private float _currentFov = 60;
        private float _pitch = 0;

        public void OnSensitivityChanged(float normalizedValue)
        {
            _sensitivity = Mathf.Lerp(_sensitivityMin, _sensitivityMax, normalizedValue);
            _zoomSensitivity = _sensitivity / 5f;
        }

        public void OnFovChanged(float normalizedValue)
        {
            _fov = Mathf.Lerp(_fovMin, _fovMax, normalizedValue);
        }

        public void Tick(float dt)
        {
            var t = Mathf.Lerp(0f, 1f, (_currentFov - ZoomFov) / (_fov - ZoomFov));
            var sensitivity = Mathf.Lerp(_zoomSensitivity, _sensitivity, t);

            _pitch += Input.GetAxis("Mouse Y") * sensitivity * dt * (_isInverted ? 1 : -1);
            _pitch = Mathf.Clamp(_pitch, -89, 89);

            _mainCamera.transform.localRotation = Quaternion.Euler(Vector3.right * _pitch);
            transform.rotation *= Quaternion.Euler(Input.GetAxis("Mouse X") * sensitivity * dt * Vector3.up);

            _mainCamera.fieldOfView = Mathf.Lerp(_mainCamera.fieldOfView, _currentFov, ZoomSpeed * dt);
            _weaponCamera.fieldOfView = Mathf.Lerp(_weaponCamera.fieldOfView, _currentFov, ZoomSpeed * dt);

            if (_zoomEnabled)
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    _currentFov = ZoomFov;
                }
                else
                {
                    _currentFov = _fov;
                }
            }
        }

        public void ResetAt(Transform t, Transform lookAt)
        {
            Gravity.Set(Vector3.down);

            transform.rotation = Quaternion.LookRotation(t.forward, Gravity.Up);

            Vector3 lookDir;
            if (lookAt != null)
            {
                lookDir = (lookAt.position - t.position).normalized;
            }
            else
            {
                lookDir = t.forward;
            }

            Quaternion resetRot = Quaternion.LookRotation(lookDir, Gravity.Up);
            if (resetRot.eulerAngles.x < 90)
            {
                _pitch = resetRot.eulerAngles.x;
            }
            else
            {
                _pitch = resetRot.eulerAngles.x - 360;
            }

            transform.rotation *= Quaternion.FromToRotation(transform.forward.WithY(0), lookDir.WithY(0));
        }

        public void FovKick(float duration, float targetFovCoeff)
        {
            StartCoroutine(FovKickCoroutine(_mainCamera, duration, targetFovCoeff));
            StartCoroutine(FovKickCoroutine(_weaponCamera, duration, targetFovCoeff));
        }

        private IEnumerator FovKickCoroutine(Camera cam, float duration, float targetFovCoeff)
        {
            float baseFov = cam.fieldOfView;
            float targetFov = cam.fieldOfView * targetFovCoeff;

            for (float f = 0; f < duration; f += Time.deltaTime)
            {
                cam.fieldOfView = Mathf.Lerp(baseFov, targetFov, _fovKickCurve.Evaluate(f / duration));
                yield return null;
            }

            cam.fieldOfView = baseFov;
        }
    }
}