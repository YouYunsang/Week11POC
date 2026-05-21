using DG.Tweening;
using UnityEngine;

namespace CleaningPOC.Player
{
    public sealed class TrashBinHighlightView : MonoBehaviour
    {
        [Header("Wiggle")]
        [SerializeField] private float _wiggleHeight = 0.08f;
        [SerializeField] private float _wiggleDuration = 0.18f;
        [SerializeField] private Ease _wiggleEase = Ease.InOutSine;

        private Tween _wiggleTween;
        private Vector3 _basePosition;
        private bool _isHighlighted;

        private void Awake()
        {
            // 들썩임 기준 위치 저장
            _basePosition = transform.position;
        }

        private void OnDisable()
        {
            // 비활성화 시 연출 정리
            StopHighlight();
        }

        private void OnDestroy()
        {
            // 파괴 시 연출 정리
            StopHighlight();
        }

        public void PlayHighlight()
        {
            if (_isHighlighted)
            {
                return;
            }

            // 현재 위치를 기준 위치로 갱신
            _basePosition = transform.position;

            // 들썩임 연출 시작
            _wiggleTween = transform
                .DOMoveY(_basePosition.y + _wiggleHeight, _wiggleDuration)
                .SetEase(_wiggleEase)
                .SetLoops(-1, LoopType.Yoyo);

            _isHighlighted = true;
        }

        public void StopHighlight()
        {
            if (!_isHighlighted && _wiggleTween == null)
            {
                return;
            }

            KillWiggleTween();

            // 원래 위치 복구
            transform.position = _basePosition;

            _isHighlighted = false;
        }

        private void KillWiggleTween()
        {
            if (_wiggleTween == null)
            {
                return;
            }

            // DOTween 정리
            _wiggleTween.Kill();
            _wiggleTween = null;
        }
    }
}