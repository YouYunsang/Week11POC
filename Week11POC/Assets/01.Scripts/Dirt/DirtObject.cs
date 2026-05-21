using DG.Tweening;
using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class DirtObject : MonoBehaviour
    {
        [Header("Scale Distortion")]
        [SerializeField] private Vector3 _attractScale = new Vector3(1.2f, 0.8f, 1.2f);
        [SerializeField] private float _scaleDistortionDuration = 0.16f;
        [SerializeField] private Ease _scaleDistortionEase = Ease.InOutSine;

        private Rigidbody _rigidbody;
        private Collider _collider;

        private Transform _attractTarget;
        private CleaningToolSettings _settings;

        private Tween _scaleTween;
        private Tween _attachMoveTween;

        private Vector3 _originalScale;
        private Vector3 _attachedTargetLocalPosition;
        private Vector3 _attachedLocalVelocity;

        private bool _wasGravityEnabled;

        public DirtState CurrentState { get; private set; } = DirtState.Idle;

        public bool IsAttractable => CurrentState == DirtState.Idle || CurrentState == DirtState.Attracting;
        public bool IsAttached => CurrentState == DirtState.Attached;

        private void Awake()
        {
            // 자기 컴포넌트 캐싱
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            // 원래 Scale과 Gravity 설정 저장
            _originalScale = transform.localScale;
            _wasGravityEnabled = _rigidbody.useGravity;
        }

        private void FixedUpdate()
        {
            if (CurrentState != DirtState.Attracting)
            {
                return;
            }

            if (_attractTarget == null || _settings == null)
            {
                StopAttracting();
                return;
            }

            // Rigidbody 기반으로 청소 도구 방향 이동
            MoveTowardAttractTarget();
        }

        private void LateUpdate()
        {
            if (CurrentState != DirtState.Attached)
            {
                return;
            }

            if (_settings == null)
            {
                return;
            }

            // Attached 상태에서도 목표 부착 위치를 계속 부드럽게 추적
            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition,
                _attachedTargetLocalPosition,
                ref _attachedLocalVelocity,
                _settings.AttachedFollowSmoothTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (CurrentState != DirtState.Dropping)
            {
                return;
            }

            // 바닥이나 다른 Collider에 닿으면 다시 흡입 가능한 상태로 복귀
            CurrentState = DirtState.Idle;
            _rigidbody.useGravity = _wasGravityEnabled;
        }

        private void OnDisable()
        {
            // 비활성화 시 Tween 정리
            KillTweens();
        }

        private void OnDestroy()
        {
            // 파괴 시 Tween 정리
            KillTweens();
        }

        public void StartAttracting(Transform attractTarget, CleaningToolSettings settings)
        {
            if (!IsAttractable)
            {
                return;
            }

            if (attractTarget == null || settings == null)
            {
                return;
            }

            // 끌림 대상과 설정 저장
            _attractTarget = attractTarget;
            _settings = settings;

            // 물리 이동 가능 상태로 설정
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = false;
            _collider.enabled = true;

            // 끌림 상태로 변경
            CurrentState = DirtState.Attracting;

            // 끌림 Scale 왜곡 연출 시작
            PlayAttractScaleDistortion();
        }

        public void StopAttracting()
        {
            if (CurrentState != DirtState.Attracting)
            {
                return;
            }

            // 끌림 정보 초기화
            _attractTarget = null;
            _settings = null;

            // 속도 정지
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            // 중력 복구
            _rigidbody.useGravity = _wasGravityEnabled;

            // Scale 복구
            StopScaleDistortion();

            // 기본 상태로 복귀
            CurrentState = DirtState.Idle;
        }

        public void AttachTo(Transform parent, Vector3 localPosition, CleaningToolSettings settings)
        {
            if (parent == null || settings == null)
            {
                return;
            }

            if (CurrentState != DirtState.Attracting && CurrentState != DirtState.Idle)
            {
                return;
            }

            // 끌림 정보 초기화
            _attractTarget = null;
            _settings = settings;

            // Attached 상태에서도 Scale 왜곡은 계속 유지
            PlayAttractScaleDistortion();

            // Kinematic 전환 전에 속도 정리
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            // 물리 비활성화
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;

            // 붙어 있는 동안 충돌 방지
            _collider.enabled = false;

            // 도구 하위로 붙이기
            transform.SetParent(parent, true);

            // Attached 상태에서 계속 추적할 목표 localPosition 저장
            _attachedTargetLocalPosition = localPosition;
            _attachedLocalVelocity = Vector3.zero;

            // 위치 Tween은 사용하지 않음. LateUpdate에서 계속 추적.
            KillAttachMoveTween();

            CurrentState = DirtState.Attached;
        }

        public void ReleaseToFloor(Vector3 releaseVelocity)
        {
            if (CurrentState != DirtState.Attached && CurrentState != DirtState.Attracting)
            {
                return;
            }

            // 끌림 정보 초기화
            _attractTarget = null;
            _settings = null;

            // 부모 해제
            transform.SetParent(null, true);

            // Tween과 Scale 정리
            KillTweens();
            transform.localScale = _originalScale;

            // 충돌과 물리 재활성화
            _collider.enabled = true;

            // Kinematic 해제 후 물리 속도 부여
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = releaseVelocity;
            _rigidbody.angularVelocity = Random.insideUnitSphere * 2f;

            CurrentState = DirtState.Dropping;
        }

        public void DisposeTo(Transform disposeTarget, CleaningToolSettings settings)
        {
            if (disposeTarget == null || settings == null)
            {
                return;
            }

            if (CurrentState != DirtState.Attached)
            {
                return;
            }

            // 부모 해제
            transform.SetParent(null, true);

            // Dispose 중에는 DOTween이 위치를 제어하므로 기존 Tween 정리
            KillTweens();
            transform.localScale = _originalScale;

            // Kinematic 전환 전에 Rigidbody 속도 정리
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            // 충돌과 물리 시뮬레이션 비활성화
            _collider.enabled = false;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;

            // Dispose 상태로 변경
            CurrentState = DirtState.Disposing;

            // 쓰레기통 중심으로 빨려 들어간 뒤 비활성화
            Sequence disposeSequence = DOTween.Sequence();

            disposeSequence.Join(
                transform
                    .DOMove(disposeTarget.position, settings.DisposeDuration)
                    .SetEase(Ease.InCubic));

            disposeSequence.Join(
                transform
                    .DOScale(Vector3.zero, settings.DisposeScaleDuration)
                    .SetEase(Ease.InBack));

            disposeSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }

        private void MoveTowardAttractTarget()
        {
            Vector3 direction = _attractTarget.position - transform.position;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                return;
            }

            // 도구 방향으로 일정 속도 이동
            _rigidbody.linearVelocity = direction.normalized * _settings.AttractSpeed;
        }

        private void PlayAttractScaleDistortion()
        {
            if (_scaleTween != null)
            {
                return;
            }

            // 끌려오는 동안 및 Attached 상태에서 압축/팽창 Scale 연출
            _scaleTween = DOTween.Sequence()
                .Append(transform.DOScale(_attractScale, _scaleDistortionDuration).SetEase(_scaleDistortionEase))
                .Append(transform.DOScale(_originalScale, _scaleDistortionDuration).SetEase(_scaleDistortionEase))
                .SetLoops(-1, LoopType.Restart);
        }

        private void StopScaleDistortion()
        {
            KillScaleTween();

            // 원래 Scale로 복구
            transform.localScale = _originalScale;
        }

        private void KillTweens()
        {
            KillScaleTween();
            KillAttachMoveTween();
        }

        private void KillScaleTween()
        {
            if (_scaleTween == null)
            {
                return;
            }

            // Scale Tween 정리
            _scaleTween.Kill();
            _scaleTween = null;
        }

        private void KillAttachMoveTween()
        {
            if (_attachMoveTween == null)
            {
                return;
            }

            // 부착 이동 Tween 정리
            _attachMoveTween.Kill();
            _attachMoveTween = null;
        }
    }
}