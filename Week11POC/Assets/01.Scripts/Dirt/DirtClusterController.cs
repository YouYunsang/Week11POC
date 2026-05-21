using System.Collections.Generic;
using UnityEngine;

namespace CleaningPOC.Player
{
    public sealed class DirtClusterController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CleaningToolSettings _settings;

        [Header("References")]
        [SerializeField] private Transform _clusterRoot;

        private readonly List<DirtObject> _attachedDirtObjects = new List<DirtObject>();

        public int AttachedCount => _attachedDirtObjects.Count;
        public bool HasAttachedDirt => _attachedDirtObjects.Count > 0;
        public bool CanAttachMore => _settings != null && _attachedDirtObjects.Count < _settings.MaxAttachedDirtCount;

        public DirtAttachResult TryAttach(DirtObject dirtObject)
        {
            if (_settings == null || _clusterRoot == null || dirtObject == null)
            {
                return DirtAttachResult.Rejected;
            }

            if (!dirtObject.IsAttractable)
            {
                return DirtAttachResult.Rejected;
            }

            if (!CanAttachMore)
            {
                return DirtAttachResult.CapacityExceeded;
            }

            // 구형 덩어리의 다음 부착 위치 계산
            Vector3 localPosition = CalculateNextLocalPosition();

            // Dirt를 도구 주변에 부착
            dirtObject.AttachTo(_clusterRoot, localPosition, _settings);

            // 부착 목록에 추가
            _attachedDirtObjects.Add(dirtObject);

            return DirtAttachResult.Attached;
        }

        public void ReleaseAll()
        {
            if (_settings == null)
            {
                return;
            }

            if (_attachedDirtObjects.Count == 0)
            {
                return;
            }

            List<DirtObject> releaseTargets = new List<DirtObject>(_attachedDirtObjects);
            _attachedDirtObjects.Clear();

            foreach (DirtObject dirtObject in releaseTargets)
            {
                if (dirtObject == null)
                {
                    continue;
                }

                // 각 Dirt에 랜덤한 초기 속도 부여
                Vector3 releaseVelocity = CreateReleaseVelocity();
                dirtObject.ReleaseToFloor(releaseVelocity);
            }
        }

        public void DisposeAll(TrashBin trashBin)
        {
            if (_settings == null || trashBin == null)
            {
                return;
            }

            if (_attachedDirtObjects.Count == 0)
            {
                return;
            }

            List<DirtObject> disposeTargets = new List<DirtObject>(_attachedDirtObjects);
            _attachedDirtObjects.Clear();

            foreach (DirtObject dirtObject in disposeTargets)
            {
                if (dirtObject == null)
                {
                    continue;
                }

                // Dirt를 쓰레기통 중심으로 빨려 들어가게 처리
                dirtObject.DisposeTo(trashBin.DisposePoint, _settings);
            }
        }

        public void ClearInvalidReferences()
        {
            for (int i = _attachedDirtObjects.Count - 1; i >= 0; i--)
            {
                if (_attachedDirtObjects[i] != null)
                {
                    continue;
                }

                // null 참조 제거
                _attachedDirtObjects.RemoveAt(i);
            }
        }

        private Vector3 CalculateNextLocalPosition()
        {
            int index = _attachedDirtObjects.Count;

            // 개수가 늘수록 구 반지름 증가
            float radius = _settings.ClusterBaseRadius + index * _settings.ClusterRadiusPerDirt;

            // 랜덤 구 표면 방향 생성
            Vector3 direction = Random.onUnitSphere;

            // 너무 아래쪽으로만 붙는 것을 방지
            direction.y = Mathf.Clamp(direction.y, -0.35f, 1f);
            direction.Normalize();

            return direction * radius;
        }

        private Vector3 CreateReleaseVelocity()
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;

            Vector3 scatterDirection = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 releaseVelocity = scatterDirection * _settings.ReleaseScatterForce;
            releaseVelocity.y = _settings.ReleaseUpwardForce;

            return releaseVelocity;
        }

        private void OnDrawGizmosSelected()
        {
            if (_settings == null || _clusterRoot == null)
            {
                return;
            }

            // 현재 최대 덩어리 예상 크기 시각화
            float maxRadius = _settings.ClusterBaseRadius
                + Mathf.Max(0, _settings.MaxAttachedDirtCount - 1) * _settings.ClusterRadiusPerDirt;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_clusterRoot.position, maxRadius);
        }
    }
}