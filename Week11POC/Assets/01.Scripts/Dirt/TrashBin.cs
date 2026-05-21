using UnityEngine;

namespace CleaningPOC.Player
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(TrashBinHighlightView))]
    public sealed class TrashBin : MonoBehaviour
    {
        [Header("Dispose Point")]
        [SerializeField] private Transform _disposePoint;

        private TrashBinHighlightView _highlightView;

        public Transform DisposePoint => _disposePoint != null
            ? _disposePoint
            : transform;

        private void Awake()
        {
            // 같은 GameObject 내부 컴포넌트 캐싱
            _highlightView = GetComponent<TrashBinHighlightView>();
        }

        public void SetHighlighted(bool isHighlighted)
        {
            if (isHighlighted)
            {
                // 버리기 가능 상태 시각화
                _highlightView.PlayHighlight();
                return;
            }

            // 버리기 가능 상태 해제
            _highlightView.StopHighlight();
        }
    }
}