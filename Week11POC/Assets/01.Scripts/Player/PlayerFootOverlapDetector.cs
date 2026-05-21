using System.Collections.Generic;
using UnityEngine;

namespace CleaningPOC.Player
{
    public sealed class PlayerFootOverlapDetector : MonoBehaviour
    {
        private const int DEFAULT_OVERLAP_BUFFER_SIZE = 16;

        [Header("Overlap Settings")]
        [SerializeField] private LayerMask _liftableLayerMask;
        [SerializeField] private Vector3 _centerOffset = new Vector3(0f, 0.05f, 0f);
        [SerializeField] private Vector3 _halfExtents = new Vector3(0.35f, 0.1f, 0.35f);

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private readonly Collider[] _overlapResults = new Collider[DEFAULT_OVERLAP_BUFFER_SIZE];
        private readonly HashSet<LiftableObject> _standingLiftables = new HashSet<LiftableObject>();

        public bool IsStandingOn(LiftableObject liftableObject)
        {
            if (liftableObject == null)
            {
                return false;
            }

            // 현재 밟고 있는 LiftableObject인지 확인
            return _standingLiftables.Contains(liftableObject);
        }

        private void Update()
        {
            // 매 프레임 발밑 LiftableObject 목록 갱신
            RefreshStandingLiftables();
        }

        private void RefreshStandingLiftables()
        {
            _standingLiftables.Clear();

            Vector3 center = transform.position + _centerOffset;

            // 발밑 영역에 겹치는 Liftable Collider 검사
            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                _halfExtents,
                _overlapResults,
                Quaternion.identity,
                _liftableLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _overlapResults[i];

                if (hitCollider == null)
                {
                    continue;
                }

                if (!hitCollider.TryGetComponent(out LiftableObject liftableObject))
                {
                    continue;
                }

                // 밟고 있는 LiftableObject 캐싱
                _standingLiftables.Add(liftableObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos)
            {
                return;
            }

            // 발밑 감지 영역 시각화
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + _centerOffset, _halfExtents * 2f);
        }
    }
}