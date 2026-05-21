using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private PlayerMovementSettings _settings;

        [SerializeField] private CharacterController _characterController;
        private Vector2 _moveInput;
        private Vector3 _currentHorizontalVelocity;
        private float _verticalVelocity;

        private void Awake()
        {
            // CharacterController 캐싱
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(PlayerMovement)} requires PlayerMovementSettings.", this);
                return;
            }

            Move();
        }

        public void SetMoveInput(Vector2 moveInput)
        {
            // 입력 벡터 저장
            _moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }

        private void Move()
        {
            // 입력을 실제 이동 방향으로 변환
            Vector3 targetDirection = ConvertInputToDirection(_moveInput);

            //Debug.Log($"MoveInput: {_moveInput}, TargetDirection: {targetDirection}", this);

            // 목표 속도 계산
            Vector3 targetVelocity = targetDirection * _settings.MoveSpeed;

            float smoothRate = targetDirection.sqrMagnitude > 0f
                ? _settings.Acceleration
                : _settings.Deceleration;

            _currentHorizontalVelocity = Vector3.MoveTowards(
                _currentHorizontalVelocity,
                targetVelocity,
                smoothRate * Time.deltaTime);

            ApplyGravity();

            Vector3 velocity = _currentHorizontalVelocity;
            velocity.y = _verticalVelocity;

            //Debug.Log($"Velocity: {velocity}", this);

            _characterController.Move(velocity * Time.deltaTime);
        }

        private Vector3 ConvertInputToDirection(Vector2 input)
        {
            return _settings.MovementPlane switch
            {
                MovementPlane.XY => new Vector3(input.x, input.y, 0f),
                _ => new Vector3(input.x, 0f, input.y)
            };
        }

        private void ApplyGravity()
        {
            if (_settings.MovementPlane == MovementPlane.XY)
            {
                _verticalVelocity = 0f;
                return;
            }

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                // 지면에 붙어 있도록 약한 중력 유지
                _verticalVelocity = _settings.GroundedGravity;
                return;
            }

            // 공중 상태 중력 누적
            _verticalVelocity += _settings.Gravity * Time.deltaTime;
        }
    }
}