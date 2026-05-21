using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleaningPOC.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public event Action<Vector2> MoveInputChanged;
        public event Action<Vector2> MousePositionChanged;

        public event Action PrimaryActionPressed;
        public event Action PrimaryActionReleased;
        public event Action<bool> PrimaryActionHoldChanged;

        public event Action<bool> TelekinesisHoldChanged;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference _moveActionReference;
        [SerializeField] private InputActionReference _mousePositionActionReference;
        [SerializeField] private InputActionReference _primaryActionReference;
        [SerializeField] private InputActionReference _telekinesisHoldActionReference;

        private Vector2 _currentMoveInput;
        private Vector2 _previousMoveInput;

        private Vector2 _currentMousePosition;
        private Vector2 _previousMousePosition;

        private bool _previousPrimaryActionHeld;
        private bool _previousTelekinesisHeld;

        public Vector2 CurrentMoveInput => _currentMoveInput;
        public Vector2 CurrentMousePosition => _currentMousePosition;

        public bool IsPrimaryActionHeld { get; private set; }
        public bool IsTelekinesisHeld { get; private set; }

        private void OnEnable()
        {
            // 모든 입력 액션 활성화
            EnableAction(_moveActionReference, nameof(_moveActionReference));
            EnableAction(_mousePositionActionReference, nameof(_mousePositionActionReference));
            EnableAction(_primaryActionReference, nameof(_primaryActionReference));
            EnableAction(_telekinesisHoldActionReference, nameof(_telekinesisHoldActionReference));
        }

        private void OnDisable()
        {
            // 모든 입력 액션 비활성화
            DisableAction(_moveActionReference);
            DisableAction(_mousePositionActionReference);
            DisableAction(_primaryActionReference);
            DisableAction(_telekinesisHoldActionReference);
        }

        private void Update()
        {
            // 입력은 Update에서만 읽음
            ReadMoveInput();
            ReadMousePositionInput();
            ReadPrimaryActionInput();
            ReadTelekinesisHoldInput();
        }

        private void ReadMoveInput()
        {
            if (!IsValidAction(_moveActionReference))
            {
                return;
            }

            // 현재 이동 입력값 읽기
            _currentMoveInput = _moveActionReference.action.ReadValue<Vector2>();

            if (_currentMoveInput == _previousMoveInput)
            {
                return;
            }

            // 이동 입력 변경 이벤트 발행
            MoveInputChanged?.Invoke(_currentMoveInput);
            _previousMoveInput = _currentMoveInput;
        }

        private void ReadMousePositionInput()
        {
            if (!IsValidAction(_mousePositionActionReference))
            {
                return;
            }

            // 현재 마우스 화면 좌표 읽기
            _currentMousePosition = _mousePositionActionReference.action.ReadValue<Vector2>();

            if (_currentMousePosition == _previousMousePosition)
            {
                return;
            }

            // 마우스 위치 변경 이벤트 발행
            MousePositionChanged?.Invoke(_currentMousePosition);
            _previousMousePosition = _currentMousePosition;
        }

        private void ReadPrimaryActionInput()
        {
            if (!IsValidAction(_primaryActionReference))
            {
                return;
            }

            // 좌클릭 유지 상태 확인
            IsPrimaryActionHeld = _primaryActionReference.action.IsPressed();

            if (IsPrimaryActionHeld == _previousPrimaryActionHeld)
            {
                return;
            }

            // 좌클릭 Hold 상태 변경 이벤트 발행
            PrimaryActionHoldChanged?.Invoke(IsPrimaryActionHeld);

            if (IsPrimaryActionHeld)
            {
                // 좌클릭을 누른 순간 이벤트 발행
                PrimaryActionPressed?.Invoke();
            }
            else
            {
                // 좌클릭을 뗀 순간 이벤트 발행
                PrimaryActionReleased?.Invoke();
            }

            _previousPrimaryActionHeld = IsPrimaryActionHeld;
        }

        private void ReadTelekinesisHoldInput()
        {
            if (!IsValidAction(_telekinesisHoldActionReference))
            {
                return;
            }

            // Alt 유지 상태 확인
            IsTelekinesisHeld = _telekinesisHoldActionReference.action.IsPressed();

            if (IsTelekinesisHeld == _previousTelekinesisHeld)
            {
                return;
            }

            // Alt 상태 변경 이벤트 발행
            TelekinesisHoldChanged?.Invoke(IsTelekinesisHeld);
            _previousTelekinesisHeld = IsTelekinesisHeld;
        }

        private void EnableAction(InputActionReference actionReference, string fieldName)
        {
            if (!IsValidAction(actionReference))
            {
                Debug.LogError($"{nameof(PlayerInputReader)} requires valid {fieldName}.", this);
                return;
            }

            // 액션 활성화
            actionReference.action.Enable();
        }

        private void DisableAction(InputActionReference actionReference)
        {
            if (!IsValidAction(actionReference))
            {
                return;
            }

            // 액션 비활성화
            actionReference.action.Disable();
        }

        private bool IsValidAction(InputActionReference actionReference)
        {
            return actionReference != null && actionReference.action != null;
        }
    }
}