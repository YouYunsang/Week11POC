using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerLookController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private PlayerLookSettings _settings;

        [Header("References")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private PlayerInputReader _inputReader;
        private Vector2 _currentMouseScreenPosition;
        private Vector3 _currentMouseWorldPosition;

        public Vector3 CurrentMouseWorldPosition => _currentMouseWorldPosition;

        private void Awake()
        {
            // 같은 GameObject 내부 컴포넌트 캐싱
            _inputReader = GetComponent<PlayerInputReader>();
        }

        private void OnEnable()
        {
            // 마우스 위치 변경 이벤트 구독
            _inputReader.MousePositionChanged += HandleMousePositionChanged;
        }

        private void OnDisable()
        {
            // 마우스 위치 변경 이벤트 구독 해제
            _inputReader.MousePositionChanged -= HandleMousePositionChanged;
        }

        private void Update()
        {
            if (_settings == null || _targetCamera == null || _spriteRenderer == null)
            {
                return;
            }

            // 매 프레임 현재 마우스 위치 기준으로 바라보기 처리
            UpdateLookDirection();
        }

        private void HandleMousePositionChanged(Vector2 mouseScreenPosition)
        {
            // 현재 마우스 화면 좌표 저장
            _currentMouseScreenPosition = mouseScreenPosition;
        }

        private void UpdateLookDirection()
        {
            if (!TryGetMouseWorldPosition(_currentMouseScreenPosition, out Vector3 mouseWorldPosition))
            {
                return;
            }

            // 현재 마우스 월드 위치 저장
            _currentMouseWorldPosition = mouseWorldPosition;

            // 마우스가 플레이어 왼쪽에 있는지 판단
            bool isMouseOnLeft = mouseWorldPosition.x < transform.position.x;

            // 스프라이트 기본 방향에 따라 Flip 기준 결정
            _spriteRenderer.flipX = _settings.DefaultFacingRight
                ? isMouseOnLeft
                : !isMouseOnLeft;
        }

        private bool TryGetMouseWorldPosition(Vector2 mouseScreenPosition, out Vector3 mouseWorldPosition)
        {
            // 카메라에서 마우스 위치로 Ray 생성
            Ray ray = _targetCamera.ScreenPointToRay(mouseScreenPosition);

            // 바닥 기준 Y 평면 생성
            Plane lookPlane = new Plane(Vector3.up, new Vector3(0f, _settings.LookPlaneY, 0f));

            if (!lookPlane.Raycast(ray, out float enter))
            {
                mouseWorldPosition = default;
                return false;
            }

            // Ray와 평면이 만나는 월드 좌표 계산
            mouseWorldPosition = ray.GetPoint(enter);
            return true;
        }
    }
}