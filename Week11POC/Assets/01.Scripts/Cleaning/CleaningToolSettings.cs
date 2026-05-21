using UnityEngine;

namespace CleaningPOC.Player
{
    [CreateAssetMenu(
        fileName = "CleaningToolSettings",
        menuName = "CleaningPOC/Player/Cleaning Tool Settings")]
    public sealed class CleaningToolSettings : ScriptableObject
    {
        [Header("Follow")]
        [SerializeField] private float _maxDistanceFromPlayer = 2.2f;
        [SerializeField] private float _followSmoothTime = 0.04f;

        [Header("Follow Surface")]
        [SerializeField] private LayerMask _toolFollowLayerMask;
        [SerializeField] private float _toolFollowRaycastMaxDistance = 100f;
        [SerializeField] private float _surfaceOffset = 0.08f;

        [Header("Attraction")]
        [SerializeField] private LayerMask _dirtLayerMask;
        [SerializeField] private float _attractRadius = 1.4f;
        [SerializeField] private float _attachDistance = 0.18f;
        [SerializeField] private float _attractSpeed = 5f;
        [SerializeField] private int _maxAttachedDirtCount = 12;

        [Header("Cluster")]
        [SerializeField] private float _clusterBaseRadius = 0.18f;
        [SerializeField] private float _clusterRadiusPerDirt = 0.035f;
        [SerializeField] private float _clusterAttachDuration = 0.15f;
        [SerializeField] private float _attachedFollowSmoothTime = 0.06f;

        [Header("Release")]
        [SerializeField] private float _releaseScatterForce = 1.5f;
        [SerializeField] private float _releaseUpwardForce = 0.6f;

        [Header("Trash Bin")]
        [SerializeField] private LayerMask _trashBinLayerMask;
        [SerializeField] private float _trashBinRaycastMaxDistance = 100f;

        [Header("Dispose")]
        [SerializeField] private float _disposeDuration = 0.35f;
        [SerializeField] private float _disposeScaleDuration = 0.25f;

        public float MaxDistanceFromPlayer => _maxDistanceFromPlayer;
        public float FollowSmoothTime => _followSmoothTime;

        public LayerMask ToolFollowLayerMask => _toolFollowLayerMask;
        public float ToolFollowRaycastMaxDistance => _toolFollowRaycastMaxDistance;
        public float SurfaceOffset => _surfaceOffset;

        public LayerMask DirtLayerMask => _dirtLayerMask;
        public float AttractRadius => _attractRadius;
        public float AttachDistance => _attachDistance;
        public float AttractSpeed => _attractSpeed;
        public int MaxAttachedDirtCount => _maxAttachedDirtCount;

        public float ClusterBaseRadius => _clusterBaseRadius;
        public float ClusterRadiusPerDirt => _clusterRadiusPerDirt;
        public float ClusterAttachDuration => _clusterAttachDuration;
        public float AttachedFollowSmoothTime => _attachedFollowSmoothTime;

        public float ReleaseScatterForce => _releaseScatterForce;
        public float ReleaseUpwardForce => _releaseUpwardForce;

        public LayerMask TrashBinLayerMask => _trashBinLayerMask;
        public float TrashBinRaycastMaxDistance => _trashBinRaycastMaxDistance;

        public float DisposeDuration => _disposeDuration;
        public float DisposeScaleDuration => _disposeScaleDuration;
    }
}