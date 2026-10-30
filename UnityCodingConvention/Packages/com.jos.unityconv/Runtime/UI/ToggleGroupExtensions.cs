using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityConv.UI
{
    /// <summary>
    /// ToggleGroup 하나당 ButtonToggle 목록 (ToggleGroup은 Toggle만 관리하므로 따로 보관)
    /// </summary>
    internal static class ButtonToggleGroupRegistry
    {
        private static readonly Dictionary<ToggleGroup, List<ButtonToggle>> _groups = new Dictionary<ToggleGroup, List<ButtonToggle>>();

        // * Enter Play Mode에서 도메인 리로드를 꺼도 이전 플레이의 목록이 남지 않게 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _groups.Clear();
        }

        public static void Register(ToggleGroup group, ButtonToggle toggle)
        {
            if (!_groups.TryGetValue(group, out List<ButtonToggle> toggles))
            {
                toggles = new List<ButtonToggle>();
                _groups[group] = toggles;
            }
            if (!toggles.Contains(toggle))
            {
                toggles.Add(toggle);
            }
        }

        public static void Unregister(ToggleGroup group, ButtonToggle toggle)
        {
            if (!_groups.TryGetValue(group, out List<ButtonToggle> toggles))
            {
                return;
            }
            toggles.Remove(toggle);
            if (toggles.Count == 0)
            {
                _groups.Remove(group);
            }
        }

        /// <summary>
        /// 켜진 토글 외에 같은 그룹의 토글을 모두 끔
        /// </summary>
        public static void NotifyToggleOn(ToggleGroup group, ButtonToggle toggle, bool sendCallback)
        {
            if (!_groups.TryGetValue(group, out List<ButtonToggle> toggles))
            {
                return;
            }

            // 끄는 중 콜백에서 목록이 바뀔 수 있어 복사본으로 순회
            foreach (ButtonToggle other in toggles.ToArray())
            {
                // 버튼 모드 토글은 그룹 처리에서 빠짐
                if (other == toggle || other == null || !other.isToggle)
                {
                    continue;
                }

                if (sendCallback)
                {
                    other.isOn = false;
                }
                else
                {
                    other.SetIsOnWithoutNotify(false);
                }
            }
        }

        public static IReadOnlyList<ButtonToggle> GetToggles(ToggleGroup group)
        {
            if (group == null || !_groups.TryGetValue(group, out List<ButtonToggle> toggles))
            {
                return System.Array.Empty<ButtonToggle>();
            }
            return toggles;
        }
    }

    /// <summary>
    /// ToggleGroup에서 ButtonToggle 조회
    /// </summary>
    public static class ToggleGroupExtensions
    {
        /// <summary>
        /// 그룹에 켜진 ButtonToggle이 있는지
        /// </summary>
        public static bool AnyButtonTogglesOn(this ToggleGroup toggleGroup)
        {
            foreach (ButtonToggle toggle in ButtonToggleGroupRegistry.GetToggles(toggleGroup))
            {
                if (toggle != null && toggle.isToggle && toggle.isOn)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 그룹에서 켜진 ButtonToggle 목록
        /// </summary>
        public static IEnumerable<ButtonToggle> ActiveButtonToggles(this ToggleGroup toggleGroup)
        {
            foreach (ButtonToggle toggle in ButtonToggleGroupRegistry.GetToggles(toggleGroup))
            {
                if (toggle != null && toggle.isToggle && toggle.isOn)
                {
                    yield return toggle;
                }
            }
        }

        /// <summary>
        /// 그룹에서 처음 켜진 ButtonToggle (없으면 null)
        /// </summary>
        public static ButtonToggle GetFirstActiveButtonToggle(this ToggleGroup toggleGroup)
        {
            foreach (ButtonToggle toggle in toggleGroup.ActiveButtonToggles())
            {
                return toggle;
            }
            return null;
        }
    }
}
