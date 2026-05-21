using DG.Tweening;
using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(LiftableHighlightView))]
    public sealed class LiftableObject : MonoBehaviour
    {
        private const int DROP_OVERLAP_BUFFER_SIZE = 8;

        private readonly Collider[] _dropOverlapResults = new Collider[DROP_OVERLAP_BUFFER_SIZE];

        private Collider _collider;
        private LiftableHighlightView _highlightView;

        private Tween _liftTween;
        private Tween _floatTween;
        private Tween _dropTween;
        private Sequence _dropBlockedSequence;

        private Vector3 _originalPosition;
        private Vector3 _colliderCenterOffset;
        private Vector3 _colliderHalfExtents;

        private float _floatingBaseY;

        public LiftableState CurrentState { get; private set; } = LiftableState.Idle;

        public bool IsSelectable => CurrentState == LiftableState.Idle || CurrentState == LiftableState.Selected;
        public bool IsLiftable => CurrentState == LiftableState.Idle || CurrentState == LiftableState.Selected;
        public bool IsDroppable => CurrentState == LiftableState.Lifted;

        private void Awake()
        {
            // 같은 GameObject 내부 컴포넌트 캐싱
            _collider = GetComponent<Collider>();
            _highlightView = GetComponent<LiftableHighlightView>();

            // 최초 위치 저장
            _originalPosition = transform.position;

            // Drop 검사에 사용할 Collider Bounds 정보 저장
            CacheDropCheckBounds();
        }

        private void OnDisable()
        {
            // 비활성화 시 모든 연출 정리
            KillAllTweens();
            _highlightView.StopHighlight();
        }

        private void OnDestroy()
        {
            // 파괴 시 모든 연출 정리
            KillAllTweens();
        }

        public bool ContainsCollider(Collider targetCollider)
        {
            // 현재 LiftableObject의 Collider와 비교
            return _collider == targetCollider;
        }

        public void SetSelected()
        {
            if (!IsSelectable)
            {
                return;
            }

            if (CurrentState == LiftableState.Selected)
            {
                return;
            }

            // 선택 상태로 변경
            CurrentState = LiftableState.Selected;

            // 선택 시각화 재생
            _highlightView.PlayHighlight();
        }

        public void SetIdle()
        {
            if (CurrentState != LiftableState.Selected)
            {
                return;
            }

            // 선택 해제 상태로 변경
            CurrentState = LiftableState.Idle;

            // 선택 시각화 종료
            _highlightView.StopHighlight();
        }

        public void Lift(TelekinesisSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError($"{nameof(LiftableObject)} requires {nameof(TelekinesisSettings)}.", this);
                return;
            }

            if (!IsLiftable)
            {
                return;
            }

            // 선택 연출 정리
            PrepareForLift();

            // 기존 이동/부유 Tween 정리
            KillAllTweens();

            // 리프트 상태로 변경
            CurrentState = LiftableState.Lifting;

            // 현재 X/Z 유지, Y만 목표 높이로 이동
            _liftTween = transform
                .DOMoveY(settings.MaxLiftHeightY, settings.LiftDuration)
                .SetEase(settings.LiftEase)
                .OnComplete(() => HandleLiftCompleted(settings));
        }

        public bool TryDrop(TelekinesisSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError($"{nameof(LiftableObject)} requires {nameof(TelekinesisSettings)}.", this);
                return false;
            }

            if (!IsDroppable)
            {
                return false;
            }

            if (!CanDrop(settings))
            {
                // Drop 불가 피드백 재생
                PlayDropBlockedFeedback(settings);
                return false;
            }

            // Drop 실행
            Drop(settings);
            return true;
        }

        public void PrepareForLift()
        {
            if (!IsLiftable)
            {
                return;
            }

            // 리프트 시작 전 선택 시각화 정리
            _highlightView.PrepareForLift();
        }

        private void Drop(TelekinesisSettings settings)
        {
            KillAllTweens();

            // Drop 상태로 변경
            CurrentState = LiftableState.Dropping;

            // X/Z는 유지하고 Y만 원래 높이로 이동
            _dropTween = transform
                .DOMoveY(_originalPosition.y, settings.DropDuration)
                .SetEase(settings.DropEase)
                .OnComplete(HandleDropCompleted);
        }

        private void HandleDropCompleted()
        {
            // 원래 Y 위치 보정
            Vector3 position = transform.position;
            position.y = _originalPosition.y;
            transform.position = position;

            // 다시 선택 가능한 상태로 복귀
            CurrentState = LiftableState.Idle;

            _dropTween = null;
        }

        private void HandleLiftCompleted(TelekinesisSettings settings)
        {
            // 최대 높이 도달 상태로 변경
            CurrentState = LiftableState.Lifted;

            // 부유 기준 Y값 저장
            _floatingBaseY = settings.MaxLiftHeightY;

            // 둥실둥실 부유 시작
            StartFloating(settings);
        }

        private void StartFloating(TelekinesisSettings settings)
        {
            KillFloatTween();

            // 최대 높이 기준으로 Y축만 둥실둥실 이동
            _floatTween = transform
                .DOMoveY(_floatingBaseY + settings.FloatOffsetY, settings.FloatDuration)
                .SetEase(settings.FloatEase)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private bool CanDrop(TelekinesisSettings settings)
        {
            Vector3 dropCheckCenter = _originalPosition + _colliderCenterOffset;
            Vector3 dropCheckHalfExtents = _colliderHalfExtents + settings.DropCheckPadding;

            // 원래 위치 범위에 Player가 있는지 검사
            int hitCount = Physics.OverlapBoxNonAlloc(
                dropCheckCenter,
                dropCheckHalfExtents,
                _dropOverlapResults,
                transform.rotation,
                settings.PlayerLayerMask,
                QueryTriggerInteraction.Ignore);

            return hitCount == 0;
        }

        private void PlayDropBlockedFeedback(TelekinesisSettings settings)
        {
            KillDropBlockedSequence();
            KillFloatTween();

            float baseY = _floatingBaseY;

            // 현재 위치를 부유 기준 높이로 부드럽게 되돌린 뒤 위아래로 짧게 흔듦
            _dropBlockedSequence = DOTween.Sequence();

            _dropBlockedSequence.Append(
                transform
                    .DOMoveY(baseY + settings.DropBlockedShakeHeight, settings.DropBlockedShakeDuration)
                    .SetEase(settings.DropBlockedShakeEase));

            for (int i = 0; i < settings.DropBlockedShakeCount; i++)
            {
                _dropBlockedSequence.Append(
                    transform
                        .DOMoveY(baseY - settings.DropBlockedShakeHeight, settings.DropBlockedShakeDuration)
                        .SetEase(settings.DropBlockedShakeEase));

                _dropBlockedSequence.Append(
                    transform
                        .DOMoveY(baseY + settings.DropBlockedShakeHeight, settings.DropBlockedShakeDuration)
                        .SetEase(settings.DropBlockedShakeEase));
            }

            _dropBlockedSequence.Append(
                transform
                    .DOMoveY(baseY, settings.DropBlockedShakeDuration)
                    .SetEase(settings.DropBlockedShakeEase));

            _dropBlockedSequence.OnComplete(() =>
            {
                _dropBlockedSequence = null;

                if (CurrentState == LiftableState.Lifted)
                {
                    // 실패 피드백 후 다시 부유 연출 시작
                    StartFloating(settings);
                }
            });

            Debug.Log($"Drop blocked: Player is inside drop area of {name}.", this);
        }

        private void CacheDropCheckBounds()
        {
            Bounds bounds = _collider.bounds;

            // 현재 Transform 위치 기준 Collider 중심 offset 저장
            _colliderCenterOffset = bounds.center - transform.position;

            // 원래 위치에 복원했을 때 사용할 Bounds 크기 저장
            _colliderHalfExtents = bounds.extents;
        }

        private void KillAllTweens()
        {
            KillLiftTween();
            KillFloatTween();
            KillDropTween();
            KillDropBlockedSequence();
        }

        private void KillLiftTween()
        {
            if (_liftTween == null)
            {
                return;
            }

            // 상승 Tween 정리
            _liftTween.Kill();
            _liftTween = null;
        }

        private void KillFloatTween()
        {
            if (_floatTween == null)
            {
                return;
            }

            // 부유 Tween 정리
            _floatTween.Kill();
            _floatTween = null;
        }

        private void KillDropTween()
        {
            if (_dropTween == null)
            {
                return;
            }

            // Drop Tween 정리
            _dropTween.Kill();
            _dropTween = null;
        }

        private void KillDropBlockedSequence()
        {
            if (_dropBlockedSequence == null)
            {
                return;
            }

            // Drop 실패 피드백 Tween 정리
            _dropBlockedSequence.Kill();
            _dropBlockedSequence = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider>();
            }

            if (_collider == null)
            {
                return;
            }

            Bounds bounds = _collider.bounds;
            Vector3 centerOffset = Application.isPlaying
                ? _colliderCenterOffset
                : bounds.center - transform.position;

            Vector3 halfExtents = Application.isPlaying
                ? _colliderHalfExtents
                : bounds.extents;

            // Drop 검사 영역 시각화
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(_originalPosition + centerOffset, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        }
    }
}