using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JamKit
{
    public class AimController : MonoBehaviour
    {
        [SerializeField] private float _aimDistance = default;

        [SerializeField] private LayerMask _aimLayer = default;

        [SerializeField] private Camera _aimCamera = default;

        [SerializeField] private RectTransform _imageTopLeft = default;

        [SerializeField] private RectTransform _imageTopRight = default;

        [SerializeField] private RectTransform _imageBottomLeft = default;

        [SerializeField] private RectTransform _imageBottomRight = default;

        private Transform _aimedTransform;
        private readonly List<Vector3> _aimedMeshVertices = new List<Vector3>();
        private readonly List<Vector3> _aimedMeshScreenPositions = new List<Vector3>();
        private readonly Vector3[] _crosshairTargetPositions = new Vector3[4];
        private readonly Vector3[] _crosshairDefaultPositions = new Vector3[4];

        private void Start()
        {
            if (_imageTopLeft == null)
            {
                Debug.LogWarning("Couldn't find the crosshair, disabling the aim controller");
                enabled = false;
                return;
            }
                
            _crosshairDefaultPositions[0] = _imageTopLeft.position;
            _crosshairDefaultPositions[1] = _imageTopRight.position;
            _crosshairDefaultPositions[2] = _imageBottomLeft.position;
            _crosshairDefaultPositions[3] = _imageBottomRight.position;

            StartCoroutine(SetAimedObject());
            StartCoroutine(MoveCrosshairSprites());
        }

        private IEnumerator SetAimedObject()
        {
            while (true)
            {
                yield return null;

                RaycastHit hit;
                Ray ray = _aimCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
                if (Physics.Raycast(ray, out hit, _aimDistance, _aimLayer))
                {
                    if (hit.transform == _aimedTransform)
                    {
                        continue;
                    }

                    if (!hit.transform.TryGetComponent(out MeshFilter meshFilter))
                    {
                        Debug.LogError("Aimed object doesn't have a mesh filter, even though it's layer is set as aimable");
                        continue;
                    }

                    _aimedTransform = hit.transform;
                    meshFilter.mesh.GetVertices(_aimedMeshVertices);
                }
                else
                {
                    _aimedTransform = null;
                }
            }
        }

        private IEnumerator MoveCrosshairSprites()
        {
            while (true)
            {
                if (_aimedTransform != null)
                {
                    _aimedMeshScreenPositions.Clear();
                    foreach (Vector3 v in _aimedMeshVertices)
                    {
                        _aimedMeshScreenPositions.Add(_aimCamera.WorldToScreenPoint(v + _aimedTransform.position));
                    }

                    float maxX = float.MinValue, maxY = float.MinValue;
                    float minX = float.MaxValue, minY = float.MaxValue;

                    foreach (Vector3 screenPos in _aimedMeshScreenPositions)
                    {
                        if (maxX < screenPos.x) maxX = screenPos.x;
                        if (minX > screenPos.x) minX = screenPos.x;
                        if (maxY < screenPos.y) maxY = screenPos.y;
                        if (minY > screenPos.y) minY = screenPos.y;
                    }

                    _crosshairTargetPositions[0] = new Vector3(minX, maxY, 0);
                    _crosshairTargetPositions[1] = new Vector3(maxX, maxY, 0);
                    _crosshairTargetPositions[2] = new Vector3(minX, minY, 0);
                    _crosshairTargetPositions[3] = new Vector3(maxX, minY, 0);
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        _crosshairTargetPositions[i] = _crosshairDefaultPositions[i];
                    }
                }

                float t = Time.deltaTime * 30f;
                _imageTopLeft.position = Vector3.Lerp(_imageTopLeft.position, _crosshairTargetPositions[0], t);
                _imageTopRight.position = Vector3.Lerp(_imageTopRight.position, _crosshairTargetPositions[1], t);
                _imageBottomLeft.position = Vector3.Lerp(_imageBottomLeft.position, _crosshairTargetPositions[2], t);
                _imageBottomRight.position = Vector3.Lerp(_imageBottomRight.position, _crosshairTargetPositions[3], t);

                yield return null;
            }
        }
    }
}