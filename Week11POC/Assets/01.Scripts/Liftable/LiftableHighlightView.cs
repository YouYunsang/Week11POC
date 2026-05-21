using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CleaningPOC.Player
{
    public sealed class LiftableHighlightView : MonoBehaviour
    {
        private const string OUTLINE_COLOR_PROPERTY = "_OutlineColor";

        [Header("Highlight")]
        [SerializeField] private Color _selectedOutlineColor = Color.white;

        [Header("Wiggle")]
        [SerializeField] private float _wiggleHeight = 0.08f;
        [SerializeField] private float _wiggleDuration = 0.18f;
        [SerializeField] private Ease _wiggleEase = Ease.InOutSine;

        [Header("Renderer")]
        [SerializeField] private bool _includeChildren = true;

        private readonly List<Material> _outlineMaterials = new List<Material>();
        private readonly Dictionary<Material, Color> _originalOutlineColors = new Dictionary<Material, Color>();

        private Tween _wiggleTween;
        private Vector3 _basePosition;
        private bool _isHighlighted;

        private void Awake()
        {
            // 현재 위치를 선택 연출 기준 위치로 저장
            _basePosition = transform.position;

            // Outline 색을 제어할 수 있는 머티리얼 캐싱
            CacheOutlineMaterials();
        }

        private void OnDisable()
        {
            // 비활성화 시 Tween 정리
            StopHighlight();
        }

        private void OnDestroy()
        {
            // 파괴 시 Tween 정리
            StopHighlight();
        }

        public void PlayHighlight()
        {
            if (_isHighlighted)
            {
                return;
            }

            // 현재 위치를 들썩임 기준 위치로 갱신
            _basePosition = transform.position;

            // Outline 색상 변경
            ApplySelectedOutlineColor();

            // 들썩임 시작
            StartWiggle();

            _isHighlighted = true;
        }

        public void StopHighlight()
        {
            if (!_isHighlighted && _wiggleTween == null)
            {
                return;
            }

            // 들썩임 종료
            KillWiggleTween();

            // 선택 전 Outline 색상으로 복구
            RestoreOriginalOutlineColor();

            // 들썩임으로 바뀐 위치 복구
            transform.position = _basePosition;

            _isHighlighted = false;
        }

        public void PrepareForLift()
        {
            // 리프트 시작 전 선택 연출 정리
            KillWiggleTween();
            RestoreOriginalOutlineColor();

            // 들썩임으로 변경된 위치를 기준 위치로 복구
            transform.position = _basePosition;

            _isHighlighted = false;
        }

        private void CacheOutlineMaterials()
        {
            _outlineMaterials.Clear();
            _originalOutlineColors.Clear();

            Renderer[] renderers = _includeChildren
                ? GetComponentsInChildren<Renderer>()
                : GetComponents<Renderer>();

            foreach (Renderer targetRenderer in renderers)
            {
                // renderer.materials는 런타임용 Material 인스턴스를 생성함
                Material[] materials = targetRenderer.materials;

                foreach (Material material in materials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    if (!material.HasProperty(OUTLINE_COLOR_PROPERTY))
                    {
                        continue;
                    }

                    // Outline 제어 대상 머티리얼 저장
                    _outlineMaterials.Add(material);

                    if (!_originalOutlineColors.ContainsKey(material))
                    {
                        // 원래 Outline 색상 저장
                        _originalOutlineColors.Add(material, material.GetColor(OUTLINE_COLOR_PROPERTY));
                    }
                }
            }
        }

        private void ApplySelectedOutlineColor()
        {
            foreach (Material material in _outlineMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                // 선택 색상 적용
                material.SetColor(OUTLINE_COLOR_PROPERTY, _selectedOutlineColor);
            }
        }

        private void RestoreOriginalOutlineColor()
        {
            foreach (KeyValuePair<Material, Color> pair in _originalOutlineColors)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                // 원래 색상 복구
                pair.Key.SetColor(OUTLINE_COLOR_PROPERTY, pair.Value);
            }
        }

        private void StartWiggle()
        {
            KillWiggleTween();

            // 기준 위치에서 Y축으로만 들썩이는 연출
            _wiggleTween = transform
                .DOMoveY(_basePosition.y + _wiggleHeight, _wiggleDuration)
                .SetEase(_wiggleEase)
                .SetLoops(-1, LoopType.Yoyo);
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