using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace Resources.Scripts
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int PickUp = Animator.StringToHash("PickUp");

        [Header("References")]
        public Camera mainCamera;
        public Animator animator;
        public ResourceManager resourceManager;
        public PopupManager popupManager;

        [Header("Movement")]
        public float approachDistance = 1.2f;
        public float rotationSpeed = 8f;
        public float pickUpDuration = 1.0f;

        private NavMeshAgent _agent;
        private bool _isCollecting;

        // Новые поля для корректного управления сбором
        private Coroutine _collectRoutine;
        private Building _currentTargetBuilding;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void Update()
        {
            var isMoving = _agent != null && _agent.pathPending == false && _agent.remainingDistance > _agent.stoppingDistance + 0.05f && !_isCollecting;
            animator?.SetBool(Move, isMoving);

            // Touch input first (mobile)
            if (HandleTouchInput())
                return;

            // Mouse
            if (Input.GetMouseButtonDown(0) && !_isCollecting)
            {
                // ignore clicks over UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                HandleClick(Input.mousePosition);
            }
        }

        private bool HandleTouchInput()
        {
            if (Input.touchCount == 0) return false;

            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && !_isCollecting)
            {
                // ignore touch over UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return true;

                HandleClick(touch.position);
                return true;
            }

            return false;
        }

        private void HandleClick(Vector2 screenPosition)
        {
            var ray = mainCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;
            var clickPoint = hit.point;

            var building = hit.collider.GetComponentInParent<Building>();
            if (building)
            {
                var dest = building.GetApproachPoint(clickPoint, approachDistance);
                MoveToAndCollect(building, dest);
            }
            else
            {
                // при клике по пустому месту — отменяем текущий сбор
                CancelCollection();

                if (_agent == null) return;
                _agent.stoppingDistance = 0.2f;
                _agent.SetDestination(clickPoint);
            }
        }

        private void MoveToAndCollect(Building building, Vector3 destination)
        {
            if (_agent == null || building == null) return;

            // Если уже собираем к этой же цели — ничего не делаем
            if (_currentTargetBuilding == building)
            {
                // но всё равно обновим destination на тот, который нам нужен
                _agent.SetDestination(destination);
                return;
            }
            
            CancelCollection();

            _currentTargetBuilding = building;

            _agent.stoppingDistance = 0.05f;
            _agent.SetDestination(destination);

            _collectRoutine = StartCoroutine(WaitAndCollectRoutine(building, destination));
        }

        private void CancelCollection()
        {
            if (_collectRoutine != null)
            {
                try { StopCoroutine(_collectRoutine); }
                catch { /* safe */ }
                _collectRoutine = null;
            }

            _currentTargetBuilding = null;
            _isCollecting = false;
        }

        private IEnumerator WaitAndCollectRoutine(Building building, Vector3 expectedDestination)
        {
            while (_agent.pathPending)
                yield return null;
            
            while (!_agent.pathPending && _agent.remainingDistance > Mathf.Max(0.05f, _agent.stoppingDistance))
            {
                if (_currentTargetBuilding != building)
                    yield break;

                if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                    yield break;

                yield return null;
            }

            if (_currentTargetBuilding != building)
                yield break;

            _agent.ResetPath();

            var lookDir = building.transform.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                var targetRot = Quaternion.LookRotation(lookDir);
                var t = 0f;
                const float duration = 0.25f;
                var startRot = transform.rotation;
                while (t < duration)
                {
                    if (_currentTargetBuilding != building)
                        yield break;

                    t += Time.deltaTime * rotationSpeed;
                    transform.rotation = Quaternion.Slerp(startRot, targetRot, Mathf.Clamp01(t / duration));
                    yield return null;
                }
                transform.rotation = targetRot;
            }

            _isCollecting = true;
            animator?.SetBool(Move, false);
            animator?.SetTrigger(PickUp);

            var waited = 0f;
            while (waited < pickUpDuration)
            {
                if (_currentTargetBuilding != building)
                {
                    _isCollecting = false;
                    yield break;
                }

                waited += Time.deltaTime;
                yield return null;
            }

            if (_currentTargetBuilding != building)
            {
                _isCollecting = false;
                yield break;
            }

            var taken = building.CollectAll();
            if (resourceManager != null && taken > 0)
            {
                resourceManager.AddCollected(building.resourceName, taken);
            }

            if (popupManager != null && resourceManager != null)
            {
                popupManager.ShowCollected(resourceManager.GetTotalCollected());
            }

            SaveSystem.Instance?.MarkDirty();

            _isCollecting = false;
            _currentTargetBuilding = null;
            _collectRoutine = null;
        }
    }
}
