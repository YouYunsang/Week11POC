using System.Collections.Generic;
using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(DirtClusterController))]
    public sealed class CleaningAttractor : MonoBehaviour
    {
        private const int DEFAULT_DIRT_BUFFER_SIZE = 64;

        [Header("Settings")]
        [SerializeField] private CleaningToolSettings _settings;

        [Header("References")]
        [SerializeField] private Transform _toolTransform;
        [SerializeField] private Camera _targetCamera;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private readonly Collider[] _dirtOverlapResults = new Collider[DEFAULT_DIRT_BUFFER_SIZE];
        private readonly HashSet<DirtObject> _attractingDirtObjects = new HashSet<DirtObject>();

        private PlayerInputReader _inputReader;
        private DirtClusterController _clusterController;

        private TrashBin _hoveredTrashBin;
        private bool _isAttractionLockedUntilRelease;

        public bool IsAttracting { get; private set; }

        private void Awake()
        {
            // 같은 GameObject 내부 컴포넌트 캐싱
            _inputReader = GetComponent<PlayerInputReader>();
            _clusterController = GetComponent<DirtClusterController>();
        }

        private void OnEnable()
        {
            // 좌클릭 및 Alt 상태 변경 이벤트 구독
            _inputReader.PrimaryActionHoldChanged += HandlePrimaryActionHoldChanged;
            _inputReader.PrimaryActionReleased += HandlePrimaryActionReleased;
            _inputReader.TelekinesisHoldChanged += HandleTelekinesisHoldChanged;
        }

        private void OnDisable()
        {
            // 좌클릭 및 Alt 상태 변경 이벤트 구독 해제
            _inputReader.PrimaryActionHoldChanged -= HandlePrimaryActionHoldChanged;
            _inputReader.PrimaryActionReleased -= HandlePrimaryActionReleased;
            _inputReader.TelekinesisHoldChanged -= HandleTelekinesisHoldChanged;

            ClearTrashBinHover();
        }

        private void Update()
        {
            // 쓰레기통 호버는 흡입 가능 여부와 별개로 매 프레임 갱신
            UpdateTrashBinHover();

            if (!CanAttract())
            {
                return;
            }

            // 좌클릭 유지 + Normal 상태일 때 Dirt 탐색
            AttractNearbyDirt();
        }

        private void AttractNearbyDirt()
        {
            Vector3 toolPosition = _toolTransform.position;

            // 청소 도구 주변 Dirt Collider 탐색
            int hitCount = Physics.OverlapSphereNonAlloc(
                toolPosition,
                _settings.AttractRadius,
                _dirtOverlapResults,
                _settings.DirtLayerMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _dirtOverlapResults[i];

                if (hitCollider == null)
                {
                    continue;
                }

                if (!hitCollider.TryGetComponent(out DirtObject dirtObject))
                {
                    continue;
                }

                if (!dirtObject.IsAttractable)
                {
                    continue;
                }

                // Dirt를 청소 도구 방향으로 끌어당김
                dirtObject.StartAttracting(_toolTransform, _settings);
                _attractingDirtObjects.Add(dirtObject);

                if (!IsInsideAttachDistance(dirtObject))
                {
                    continue;
                }

                // 도구 근처에 도달한 Dirt를 구형 덩어리에 부착 시도
                HandleAttachAttempt(dirtObject);
            }
        }

        private void HandleAttachAttempt(DirtObject dirtObject)
        {
            DirtAttachResult result = _clusterController.TryAttach(dirtObject);

            if (result == DirtAttachResult.Attached)
            {
                // 부착 성공 시 끌림 목록에서 제거
                _attractingDirtObjects.Remove(dirtObject);
                return;
            }

            if (result != DirtAttachResult.CapacityExceeded)
            {
                return;
            }

            // 최대 수집 개수 초과 처리
            HandleCapacityExceeded(dirtObject);
        }

        private void HandleCapacityExceeded(DirtObject overflowDirt)
        {
            Debug.Log($"Cleaning tool capacity exceeded. Max Count: {_settings.MaxAttachedDirtCount}", this);

            // 기존에 붙어 있던 Dirt 전부 바닥으로 떨어뜨림
            _clusterController.ReleaseAll();

            if (overflowDirt != null)
            {
                // 초과 Dirt는 StopAttracting 전에 먼저 떨어뜨려야 함
                _attractingDirtObjects.Remove(overflowDirt);
                overflowDirt.ReleaseToFloor(CreateOverflowReleaseVelocity());
            }

            // 나머지 끌려오던 Dirt들은 일반 상태로 복귀
            StopAttractingDirtObjects();

            // 좌클릭을 떼기 전까지 흡입 잠금
            _isAttractionLockedUntilRelease = true;
            IsAttracting = false;

            ClearTrashBinHover();
        }

        private void UpdateTrashBinHover()
        {
            TrashBin nextTrashBin = RaycastTrashBin();

            bool canHighlightTrashBin = _inputReader.IsPrimaryActionHeld
                && !_inputReader.IsTelekinesisHeld
                && _clusterController.HasAttachedDirt
                && nextTrashBin != null;

            if (_hoveredTrashBin == nextTrashBin)
            {
                if (_hoveredTrashBin != null)
                {
                    // 같은 쓰레기통을 계속 보고 있어도 가능 상태가 바뀔 수 있으므로 갱신
                    _hoveredTrashBin.SetHighlighted(canHighlightTrashBin);
                }

                return;
            }

            if (_hoveredTrashBin != null)
            {
                // 이전 쓰레기통 하이라이트 해제
                _hoveredTrashBin.SetHighlighted(false);
            }

            _hoveredTrashBin = nextTrashBin;

            if (_hoveredTrashBin != null)
            {
                // 새 쓰레기통 하이라이트 적용
                _hoveredTrashBin.SetHighlighted(canHighlightTrashBin);
            }
        }

        private TrashBin RaycastTrashBin()
        {
            if (_settings == null || _targetCamera == null)
            {
                return null;
            }

            // 현재 마우스 화면 좌표 기준 Ray 생성
            Ray ray = _targetCamera.ScreenPointToRay(_inputReader.CurrentMousePosition);

            bool hasHit = Physics.Raycast(
                ray,
                out RaycastHit hit,
                _settings.TrashBinRaycastMaxDistance,
                _settings.TrashBinLayerMask,
                QueryTriggerInteraction.Ignore);

            if (!hasHit)
            {
                return null;
            }

            if (!hit.collider.TryGetComponent(out TrashBin trashBin))
            {
                return null;
            }

            return trashBin;
        }

        private bool IsInsideAttachDistance(DirtObject dirtObject)
        {
            if (dirtObject == null)
            {
                return false;
            }

            float sqrDistance = (_toolTransform.position - dirtObject.transform.position).sqrMagnitude;
            float attachDistance = _settings.AttachDistance;

            // AttachDistance 안에 들어왔는지 검사
            return sqrDistance <= attachDistance * attachDistance;
        }

        private bool CanAttract()
        {
            if (_settings == null || _toolTransform == null)
            {
                return false;
            }

            if (_isAttractionLockedUntilRelease)
            {
                return false;
            }

            if (!_inputReader.IsPrimaryActionHeld)
            {
                return false;
            }

            if (_inputReader.IsTelekinesisHeld)
            {
                return false;
            }

            return IsAttracting;
        }

        private void HandlePrimaryActionHoldChanged(bool isHeld)
        {
            if (!isHeld)
            {
                IsAttracting = false;
                return;
            }

            // Alt를 누르고 있지 않은 Normal 상태에서만 인력 활성화
            IsAttracting = !_inputReader.IsTelekinesisHeld && !_isAttractionLockedUntilRelease;
        }

        private void HandlePrimaryActionReleased()
        {
            // 좌클릭 해제 시 인력 종료
            IsAttracting = false;

            // 끌려오던 Dirt 정리
            StopAttractingDirtObjects();

            if (_hoveredTrashBin != null && _clusterController.HasAttachedDirt)
            {
                // 쓰레기통 호버 중이면 버리기
                _clusterController.DisposeAll(_hoveredTrashBin);
                ClearTrashBinHover();
            }
            else
            {
                // 쓰레기통이 아니면 바닥으로 떨어뜨리기
                _clusterController.ReleaseAll();
            }

            // 좌클릭을 떼면 수용량 초과 잠금 해제
            _isAttractionLockedUntilRelease = false;
        }

        private void HandleTelekinesisHoldChanged(bool isHeld)
        {
            if (!isHeld)
            {
                return;
            }

            // Alt를 누르는 순간 청소 인력 강제 종료
            IsAttracting = false;

            // 끌려오던 Dirt 정리
            StopAttractingDirtObjects();

            // 이미 붙어 있던 Dirt도 바닥으로 떨어뜨림
            _clusterController.ReleaseAll();

            ClearTrashBinHover();

            // Alt로 중단한 경우 현재 좌클릭 홀드는 청소에 사용하지 않음
            _isAttractionLockedUntilRelease = true;
        }

        private void StopAttractingDirtObjects()
        {
            foreach (DirtObject dirtObject in _attractingDirtObjects)
            {
                if (dirtObject == null)
                {
                    continue;
                }

                if (dirtObject.CurrentState != DirtState.Attracting)
                {
                    continue;
                }

                // 끌림 중이던 Dirt를 일반 상태로 복귀
                dirtObject.StopAttracting();
            }

            _attractingDirtObjects.Clear();
        }

        private void ClearTrashBinHover()
        {
            if (_hoveredTrashBin == null)
            {
                return;
            }

            // 쓰레기통 하이라이트 해제
            _hoveredTrashBin.SetHighlighted(false);
            _hoveredTrashBin = null;
        }

        private Vector3 CreateOverflowReleaseVelocity()
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            Vector3 releaseVelocity = new Vector3(randomCircle.x, 0f, randomCircle.y) * _settings.ReleaseScatterForce;
            releaseVelocity.y = _settings.ReleaseUpwardForce;

            return releaseVelocity;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos || _settings == null || _toolTransform == null)
            {
                return;
            }

            // 청소 도구 인력 범위 시각화
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_toolTransform.position, _settings.AttractRadius);

            // 부착 거리 시각화
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_toolTransform.position, _settings.AttachDistance);
        }
    }
}