using System.Collections.Generic;
using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerLookController))]
    [RequireComponent(typeof(PlayerFootOverlapDetector))]
    [RequireComponent(typeof(LiftableSelectionController))]
    public sealed class PlayerTelekinesisController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private TelekinesisSettings _settings;

        [Header("References")]
        [SerializeField] private Camera _targetCamera;

        private PlayerInputReader _inputReader;
        private PlayerLookController _lookController;
        private PlayerFootOverlapDetector _footOverlapDetector;
        private LiftableSelectionController _selectionController;

        private bool _isTelekinesisHeld;

        private void Awake()
        {
            // 같은 GameObject 내부 컴포넌트 캐싱
            _inputReader = GetComponent<PlayerInputReader>();
            _lookController = GetComponent<PlayerLookController>();
            _footOverlapDetector = GetComponent<PlayerFootOverlapDetector>();
            _selectionController = GetComponent<LiftableSelectionController>();
        }

        private void OnEnable()
        {
            // 입력 이벤트 구독
            _inputReader.PrimaryActionPressed += HandlePrimaryActionPressed;
            _inputReader.TelekinesisHoldChanged += HandleTelekinesisHoldChanged;
        }

        private void OnDisable()
        {
            // 입력 이벤트 구독 해제
            _inputReader.PrimaryActionPressed -= HandlePrimaryActionPressed;
            _inputReader.TelekinesisHoldChanged -= HandleTelekinesisHoldChanged;
        }

        private void Update()
        {
            if (!CanProcessTelekinesisSelection())
            {
                return;
            }

            // Alt 유지 중 마우스 경로상의 LiftableObject 누적 선택
            UpdateTelekinesisSelection();
        }

        private void UpdateTelekinesisSelection()
        {
            LiftableObject target = RaycastLiftableObject();

            if (!IsValidSelectionTarget(target))
            {
                return;
            }

            // 염력 활성화 중에는 항상 경로 누적 선택
            _selectionController.AddMultiSelection(target);
        }

        private void HandlePrimaryActionPressed()
        {
            if (_settings == null || _targetCamera == null)
            {
                return;
            }

            // Drop은 Alt 여부와 무관하게 가장 먼저 처리
            if (TryDropHoveredLiftable())
            {
                return;
            }

            if (!CanProcessTelekinesisSelection())
            {
                return;
            }

            LiftSelectedObjects();
        }

        private bool TryDropHoveredLiftable()
        {
            LiftableObject hoveredLiftable = RaycastLiftableObject();

            if (hoveredLiftable == null || !hoveredLiftable.IsDroppable)
            {
                return false;
            }

            // 떠 있는 오브젝트는 모드와 무관하게 Drop 시도
            hoveredLiftable.TryDrop(_settings);
            return true;
        }

        private void LiftSelectedObjects()
        {
            List<LiftableObject> targets = _selectionController.GetCurrentLiftTargets();

            if (targets.Count == 0)
            {
                return;
            }

            foreach (LiftableObject target in targets)
            {
                if (target == null || !target.IsLiftable)
                {
                    continue;
                }

                // 선택된 오브젝트 리프트 실행
                target.Lift(_settings);
            }

            // 리프트 실행 후 선택 목록 정리
            _selectionController.ClearAllSelections();
        }

        private void HandleTelekinesisHoldChanged(bool isHeld)
        {
            _isTelekinesisHeld = isHeld;

            if (_isTelekinesisHeld)
            {
                // 염력 활성화 시작
                _selectionController.ClearAllSelections();
                return;
            }

            // Alt를 떼면 어떤 경우든 선택 상태 정리
            _selectionController.ClearAllSelections();
        }

        private LiftableObject RaycastLiftableObject()
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
                _settings.RaycastMaxDistance,
                _settings.LiftableLayerMask);

            if (!hasHit)
            {
                return null;
            }

            if (!hit.collider.TryGetComponent(out LiftableObject liftableObject))
            {
                return null;
            }

            return liftableObject;
        }

        private bool IsValidSelectionTarget(LiftableObject target)
        {
            if (target == null)
            {
                return false;
            }

            if (!target.IsSelectable)
            {
                return false;
            }

            if (_footOverlapDetector.IsStandingOn(target))
            {
                return false;
            }

            return true;
        }

        private bool CanProcessTelekinesisSelection()
        {
            return _settings != null
                && _targetCamera != null
                && _isTelekinesisHeld;
        }
    }
}