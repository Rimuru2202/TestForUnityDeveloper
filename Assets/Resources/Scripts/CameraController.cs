using UnityEngine;

namespace Resources.Scripts
{
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        [Header("Настройки камеры")]
        public float panSpeed = 20f;
        public float edgePanThreshold = 10f;
        public float dragPanSpeed = 0.5f;
        public float scrollSpeed = 300f;
        public float minY = 10f;
        public float maxY = 80f;

        [Header("Границы (по сторонам)")]
        public Transform boundaryLeft;
        public Transform boundaryRight;
        public Transform boundaryTop;
        public Transform boundaryBottom;
        public Vector2 padding = Vector2.zero;
        public bool useLateUpdate = true;

        private Vector3 _lastMousePos;
        private bool _isDragging;

        private void Update()
        {
            HandleKeyboardPan();
            HandleEdgePan();
            HandleMouseDrag();
            HandleTouchPanAndPinch();
            HandleScroll();

            if (!useLateUpdate)
                ApplyBounds();
        }

        private void LateUpdate()
        {
            if (useLateUpdate)
                ApplyBounds();
        }

        private void HandleKeyboardPan()
        {
            var h = 0f;
            var v = 0f;
            if (Input.GetKey("w")) v += 1f;
            if (Input.GetKey("s")) v -= 1f;
            if (Input.GetKey("d")) h += 1f;
            if (Input.GetKey("a")) h -= 1f;
            var input = new Vector3(h, 0, v);
            if (input.sqrMagnitude > 0.0001f)
            {
                transform.position += input.normalized * (panSpeed * Time.deltaTime);
            }
        }

        private void HandleEdgePan()
        {
            var pos = transform.position;
            if (Input.mousePosition.y >= Screen.height - edgePanThreshold) pos.z += panSpeed * Time.deltaTime;
            if (Input.mousePosition.y <= edgePanThreshold) pos.z -= panSpeed * Time.deltaTime;
            if (Input.mousePosition.x >= Screen.width - edgePanThreshold) pos.x += panSpeed * Time.deltaTime;
            if (Input.mousePosition.x <= edgePanThreshold) pos.x -= panSpeed * Time.deltaTime;
            transform.position = pos;
        }

        private void HandleMouseDrag()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _isDragging = true;
                _lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                _isDragging = false;
            }

            if (!_isDragging) return;
            var delta = Input.mousePosition - _lastMousePos;
            var move = new Vector3(-delta.x * dragPanSpeed * Time.deltaTime, 0, -delta.y * dragPanSpeed * Time.deltaTime);
            transform.position += move;
            _lastMousePos = Input.mousePosition;
        }

        private void HandleTouchPanAndPinch()
        {
            switch (Input.touchCount)
            {
                case 1:
                {
                    var t = Input.GetTouch(0);
                    switch (t.phase)
                    {
                        case TouchPhase.Began:
                            _lastMousePos = t.position;
                            break;
                        case TouchPhase.Moved:
                        {
                            var delta = t.position - (Vector2)_lastMousePos;
                            var move = new Vector3(-delta.x * dragPanSpeed * Time.deltaTime, 0, -delta.y * dragPanSpeed * Time.deltaTime);
                            transform.position += move;
                            _lastMousePos = t.position;
                            break;
                        }
                    }

                    break;
                }
                case 2:
                {
                    var t0 = Input.GetTouch(0);
                    var t1 = Input.GetTouch(1);

                    var prev0 = t0.position - t0.deltaPosition;
                    var prev1 = t1.position - t1.deltaPosition;

                    var prevDist = (prev0 - prev1).magnitude;
                    var curDist = (t0.position - t1.position).magnitude;
                    var diff = curDist - prevDist;

                    var pos = transform.position;
                    pos.y -= diff * (scrollSpeed * 0.01f) * Time.deltaTime;
                    pos.y = Mathf.Clamp(pos.y, minY, maxY);
                    transform.position = pos;
                    break;
                }
            }
        }

        private void HandleScroll()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (!(Mathf.Abs(scroll) > 0.0001f)) return;
            var pos = transform.position;
            pos.y -= scroll * scrollSpeed * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }

        private void ApplyBounds()
        {
            var pos = transform.position;

            var hasXBounds = boundaryLeft != null || boundaryRight != null;
            var hasZBounds = boundaryTop != null || boundaryBottom != null;

            if (hasXBounds)
            {
                var leftX = boundaryLeft != null ? boundaryLeft.position.x : float.NegativeInfinity;
                var rightX = boundaryRight != null ? boundaryRight.position.x : float.PositiveInfinity;

                var minX = Mathf.Min(leftX, rightX);
                var maxX = Mathf.Max(leftX, rightX);

                minX += padding.x;
                maxX -= padding.x;

                switch (float.IsNegativeInfinity(minX))
                {
                    case false when !float.IsPositiveInfinity(maxX):
                        pos.x = Mathf.Clamp(pos.x, minX, maxX);
                        break;
                    case false:
                        pos.x = Mathf.Max(pos.x, minX);
                        break;
                    default:
                    {
                        if (!float.IsPositiveInfinity(maxX))
                            pos.x = Mathf.Min(pos.x, maxX);
                        break;
                    }
                }
            }

            if (hasZBounds)
            {
                var bottomZ = boundaryBottom != null ? boundaryBottom.position.z : float.NegativeInfinity;
                var topZ = boundaryTop != null ? boundaryTop.position.z : float.PositiveInfinity;

                var minZ = Mathf.Min(bottomZ, topZ);
                var maxZ = Mathf.Max(bottomZ, topZ);

                minZ += padding.y;
                maxZ -= padding.y;

                switch (float.IsNegativeInfinity(minZ))
                {
                    case false when !float.IsPositiveInfinity(maxZ):
                        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
                        break;
                    case false:
                        pos.z = Mathf.Max(pos.z, minZ);
                        break;
                    default:
                    {
                        if (!float.IsPositiveInfinity(maxZ))
                            pos.z = Mathf.Min(pos.z, maxZ);
                        break;
                    }
                }
            }

            transform.position = pos;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.8f, 0.6f);

            if (boundaryLeft != null && boundaryRight != null)
            {
                var a = new Vector3(boundaryLeft.position.x, transform.position.y, boundaryLeft.position.z);
                var b = new Vector3(boundaryRight.position.x, transform.position.y, boundaryRight.position.z);
                Gizmos.DrawLine(a, b);
            }

            if (boundaryTop == null || boundaryBottom == null) return;
            {
                var a = new Vector3(boundaryTop.position.x, transform.position.y, boundaryTop.position.z);
                var b = new Vector3(boundaryBottom.position.x, transform.position.y, boundaryBottom.position.z);
                Gizmos.DrawLine(a, b);
            }
        }
#endif
    }
}
