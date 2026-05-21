using System.Collections.Generic;
using UnityEngine;

namespace CleaningPOC.Player
{
    public sealed class LiftableSelectionController : MonoBehaviour
    {
        private readonly HashSet<LiftableObject> _multiSelectedLiftables = new HashSet<LiftableObject>();

        private LiftableObject _hoveredLiftable;

        public LiftableObject HoveredLiftable => _hoveredLiftable;
        public IReadOnlyCollection<LiftableObject> MultiSelectedLiftables => _multiSelectedLiftables;

        public bool HasMultiSelection => _multiSelectedLiftables.Count > 0;

        public void AddMultiSelection(LiftableObject liftableObject)
        {
            if (liftableObject == null || !liftableObject.IsSelectable)
            {
                return;
            }

            if (_multiSelectedLiftables.Contains(liftableObject))
            {
                return;
            }

            // 다중 선택 목록에 추가
            _multiSelectedLiftables.Add(liftableObject);
            liftableObject.SetSelected();
        }

        public void ClearMultiSelection()
        {
            foreach (LiftableObject liftableObject in _multiSelectedLiftables)
            {
                if (liftableObject == null)
                {
                    continue;
                }

                // 다중 선택된 대상 선택 해제
                liftableObject.SetIdle();
            }

            _multiSelectedLiftables.Clear();
        }

        public List<LiftableObject> GetCurrentLiftTargets()
        {
            List<LiftableObject> targets = new List<LiftableObject>();

            if (_multiSelectedLiftables.Count > 0)
            {
                foreach (LiftableObject liftableObject in _multiSelectedLiftables)
                {
                    if (liftableObject == null || !liftableObject.IsLiftable)
                    {
                        continue;
                    }

                    // 다중 선택 대상 추가
                    targets.Add(liftableObject);
                }

                return targets;
            }

            if (_hoveredLiftable != null && _hoveredLiftable.IsLiftable)
            {
                // 단일 호버 대상 추가
                targets.Add(_hoveredLiftable);
            }

            return targets;
        }

        public void ClearAllSelections()
        {
            // 호버와 다중 선택 모두 해제
            ClearMultiSelection();
        }
    }
}