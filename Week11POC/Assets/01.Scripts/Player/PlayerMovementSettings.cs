using UnityEngine;

namespace CleaningPOC.Player
{
    [CreateAssetMenu(
        fileName = "PlayerMovementSettings",
        menuName = "CleaningPOC/Player/Movement Settings")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _acceleration = 20f;
        [SerializeField] private float _deceleration = 24f;

        [Header("Plane")]
        [SerializeField] private MovementPlane _movementPlane = MovementPlane.XZ;

        [Header("Gravity")]
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _groundedGravity = -2f;

        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public MovementPlane MovementPlane => _movementPlane;
        public float Gravity => _gravity;
        public float GroundedGravity => _groundedGravity;
    }
}