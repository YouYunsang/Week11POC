using DG.Tweening;
using UnityEngine;

namespace CleaningPOC.Player
{
    [CreateAssetMenu(
        fileName = "TelekinesisSettings",
        menuName = "CleaningPOC/Player/Telekinesis Settings")]
    public sealed class TelekinesisSettings : ScriptableObject
    {
        [Header("Raycast")]
        [SerializeField] private LayerMask _liftableLayerMask;
        [SerializeField] private float _raycastMaxDistance = 100f;

        [Header("Drop Collision")]
        [SerializeField] private LayerMask _playerLayerMask;
        [SerializeField] private Vector3 _dropCheckPadding = new Vector3(0.05f, 0.05f, 0.05f);

        [Header("Lift")]
        [SerializeField] private float _maxLiftHeightY = 4f;
        [SerializeField] private float _liftDuration = 0.6f;
        [SerializeField] private Ease _liftEase = Ease.OutCubic;

        [Header("Drop")]
        [SerializeField] private float _dropDuration = 0.5f;
        [SerializeField] private Ease _dropEase = Ease.InCubic;

        [Header("Drop Blocked Feedback")]
        [SerializeField] private float _dropBlockedShakeHeight = 0.08f;
        [SerializeField] private float _dropBlockedShakeDuration = 0.18f;
        [SerializeField] private int _dropBlockedShakeCount = 2;
        [SerializeField] private Ease _dropBlockedShakeEase = Ease.InOutSine;

        [Header("Floating")]
        [SerializeField] private float _floatOffsetY = 0.12f;
        [SerializeField] private float _floatDuration = 0.8f;
        [SerializeField] private Ease _floatEase = Ease.InOutSine;

        public LayerMask LiftableLayerMask => _liftableLayerMask;
        public float RaycastMaxDistance => _raycastMaxDistance;

        public LayerMask PlayerLayerMask => _playerLayerMask;
        public Vector3 DropCheckPadding => _dropCheckPadding;

        public float MaxLiftHeightY => _maxLiftHeightY;
        public float LiftDuration => _liftDuration;
        public Ease LiftEase => _liftEase;

        public float DropDuration => _dropDuration;
        public Ease DropEase => _dropEase;

        public float DropBlockedShakeHeight => _dropBlockedShakeHeight;
        public float DropBlockedShakeDuration => _dropBlockedShakeDuration;
        public int DropBlockedShakeCount => _dropBlockedShakeCount;
        public Ease DropBlockedShakeEase => _dropBlockedShakeEase;

        public float FloatOffsetY => _floatOffsetY;
        public float FloatDuration => _floatDuration;
        public Ease FloatEase => _floatEase;
    }
}