using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class CleaningToolFollower : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CleaningToolSettings _settings;

        [Header("References")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Transform _toolTransform;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private PlayerInputReader _inputReader;
        private Vector2 _currentMouseScreenPosition;
        private Vector3 _followVelocity;

        public Vector3 ToolPosition => _toolTransform != null
            ? _toolTransform.position
            : transform.position;

        private void Awake()
        {
            // 같은 GameObject 내부 컴포넌트 캐싱
            _inputReader = GetComponent<PlayerInputReader>();
        }

        private void OnEnable()
        {
            // 마우스 위치 이벤트 구독
            _inputReader.MousePositionChanged += HandleMousePositionChanged;
        }

        private void OnDisable()
        {
            // 마우스 위치 이벤트 구독 해제
            _inputReader.MousePositionChanged -= HandleMousePositionChanged;
        }

        private void Update()
        {
            if (_settings == null || _targetCamera == null || _toolTransform == null)
            {
                return;
            }

            // 마우스 Raycast hit 위치를 기준으로 도구 위치 갱신
            UpdateToolPosition();
        }

        private void HandleMousePositionChanged(Vector2 mouseScreenPosition)
        {
            // 현재 마우스 화면 좌표 저장
            _currentMouseScreenPosition = mouseScreenPosition;
        }

        private void UpdateToolPosition()
        {
            if (!TryGetMouseWorldPosition(_currentMouseScreenPosition, out Vector3 mouseWorldPosition))
            {
                return;
            }

            Vector3 playerPosition = transform.position;

            // 수평 거리만 플레이어 기준 최대 반경으로 제한
            Vector3 horizontalOffset = mouseWorldPosition - playerPosition;
            horizontalOffset.y = 0f;

            if (horizontalOffset.magnitude > _settings.MaxDistanceFromPlayer)
            {
                horizontalOffset = horizontalOffset.normalized * _settings.MaxDistanceFromPlayer;
            }

            Vector3 targetPosition = playerPosition + horizontalOffset;

            // Y축은 Raycast hit 위치의 Y값을 사용
            targetPosition.y = mouseWorldPosition.y;

            // 도구가 부드럽게 마우스를 따라가도록 처리
            _toolTransform.position = Vector3.SmoothDamp(
                _toolTransform.position,
                targetPosition,
                ref _followVelocity,
                _settings.FollowSmoothTime);
        }

        private bool TryGetMouseWorldPosition(Vector2 mouseScreenPosition, out Vector3 mouseWorldPosition)
        {
            // 카메라에서 마우스 위치로 Ray 생성
            Ray ray = _targetCamera.ScreenPointToRay(mouseScreenPosition);

            bool hasHit = Physics.Raycast(
                ray,
                out RaycastHit hit,
                _settings.ToolFollowRaycastMaxDistance,
                _settings.ToolFollowLayerMask,
                QueryTriggerInteraction.Ignore);

            if (!hasHit)
            {
                mouseWorldPosition = default;
                return false;
            }

            // 표면에 박히지 않도록 hit.normal 방향으로 약간 띄움
            mouseWorldPosition = hit.point + hit.normal * _settings.SurfaceOffset;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos || _settings == null)
            {
                return;
            }

            // 청소 도구 최대 이동 반경 시각화
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _settings.MaxDistanceFromPlayer);

            if (_toolTransform == null)
            {
                return;
            }

            // 청소 도구 흡입 반경 시각화
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_toolTransform.position, _settings.AttractRadius);
        }
    }
}