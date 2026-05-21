using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerController : MonoBehaviour
    {
        private PlayerInputReader _inputReader;
        private PlayerMovement _movement;

        private void Awake()
        {
            // 같은 GameObject 내부 컴포넌트 캐싱
            _inputReader = GetComponent<PlayerInputReader>();
            _movement = GetComponent<PlayerMovement>();
        }

        private void OnEnable()
        {
            // 이동 입력 이벤트 구독
            _inputReader.MoveInputChanged += HandleMoveInputChanged;
        }

        private void OnDisable()
        {
            // 이동 입력 이벤트 구독 해제
            _inputReader.MoveInputChanged -= HandleMoveInputChanged;
        }

        private void HandleMoveInputChanged(Vector2 moveInput)
        {
            // 이동 입력을 Movement 컴포넌트에 전달
            _movement.SetMoveInput(moveInput);
        }
    }
}