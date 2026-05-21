using System;
using UnityEngine;

namespace CleaningPOC.Player
{
    public sealed class PlayerModeController : MonoBehaviour
    {
        public event Action<PlayerMode> ModeChanged;

        [Header("Initial State")]
        [SerializeField] private PlayerMode _initialMode = PlayerMode.Normal;

        public PlayerMode CurrentMode { get; private set; }

        public bool IsTelekinesisMode => CurrentMode == PlayerMode.Telekinesis;

        private void Awake()
        {
            // 초기 모드 설정
            CurrentMode = _initialMode;
        }

        private void Start()
        {
            // 초기 모드 이벤트 발행
            ModeChanged?.Invoke(CurrentMode);
        }

        public void ToggleMode()
        {
            // 일반 모드와 염력 모드 전환
            PlayerMode nextMode = CurrentMode == PlayerMode.Normal
                ? PlayerMode.Telekinesis
                : PlayerMode.Normal;

            SetMode(nextMode);
        }

        public void SetMode(PlayerMode mode)
        {
            if (CurrentMode == mode)
            {
                return;
            }

            // 현재 모드 갱신
            CurrentMode = mode;

            // 모드 변경 이벤트 발행
            ModeChanged?.Invoke(CurrentMode);

            Debug.Log($"Player Mode Changed: {CurrentMode}", this);
        }
    }
}