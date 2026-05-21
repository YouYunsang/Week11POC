using UnityEngine;

namespace CleaningPOC.Player
{
    [CreateAssetMenu(
        fileName = "PlayerLookSettings",
        menuName = "CleaningPOC/Player/Look Settings")]
    public sealed class PlayerLookSettings : ScriptableObject
    {
        [Header("World Plane")]
        [SerializeField] private float _lookPlaneY = 0f;

        [Header("Sprite Direction")]
        [SerializeField] private bool _defaultFacingRight = true;

        public float LookPlaneY => _lookPlaneY;
        public bool DefaultFacingRight => _defaultFacingRight;
    }
}