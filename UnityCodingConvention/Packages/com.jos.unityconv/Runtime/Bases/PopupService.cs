using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityConv.Bases
{
    /// <summary>
    /// 전역 팝업 스택 (씬 오브젝트 없이 사용), 새 팝업을 열면 이전 팝업은 숨기고 닫으면 다시 표시
    /// </summary>
    public static class PopupService
    {
        private static readonly Dictionary<Type, Component> _popups = new Dictionary<Type, Component>();
        private static readonly List<GameObject> _stack = new List<GameObject>();

        /// <summary>
        /// 스택 변경 로그 출력 여부
        /// </summary>
        public static bool LogEnabled { get; set; } = true;

        /// <summary>
        /// 열린 팝업 수 (외부에서 닫힌 팝업 제외)
        /// </summary>
        public static int Count
        {
            get
            {
                CleanUp();
                return _stack.Count;
            }
        }

        // * Enter Play Mode에서 도메인 리로드를 꺼도 이전 플레이의 스택이 남지 않게 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _popups.Clear();
            _stack.Clear();
        }

        /// <summary>
        /// 팝업 조회 (열지 않음), 로드된 씬에서 꺼진 오브젝트까지 찾고 결과는 캐시
        /// </summary>
        public static T Get<T>() where T : Component
        {
            if (_popups.TryGetValue(typeof(T), out Component popup) && popup != null)
            {
                return (T)popup;
            }

            T found = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (found == null)
            {
                Debug.LogWarning($"PopupService: {typeof(T).Name} not found in loaded scenes.");
                return null;
            }

            _popups[typeof(T)] = found;
            return found;
        }

        /// <summary>
        /// 맨 위에 쌓고 열기 (이미 열려 있으면 맨 위로), 이전 팝업은 숨김
        /// </summary>
        public static void Push(Component popup)
        {
            if (popup == null)
            {
                return;
            }

            CleanUp();
            GameObject target = popup.gameObject;
            _stack.Remove(target);
            if (_stack.Count > 0)
            {
                _stack[_stack.Count - 1].SetActive(false);
            }
            _stack.Add(target);

            target.transform.SetAsLastSibling();
            target.SetActive(true);
            LogStack($"Push {target.name}");
        }

        /// <summary>
        /// 해당 팝업과 그 위에 쌓인 팝업 닫기 (한 겹 복귀), 이전 팝업은 다시 표시
        /// </summary>
        public static void Close(Component popup)
        {
            if (popup == null)
            {
                return;
            }

            CleanUp();
            GameObject target = popup.gameObject;
            int index = _stack.IndexOf(target);
            if (index < 0)
            {
                // 쌓지 않고 연 팝업은 자기만 닫음
                target.SetActive(false);
                LogStack($"Close {target.name} (not stacked)");
                return;
            }

            for (int i = _stack.Count - 1; i >= index; i--)
            {
                GameObject item = _stack[i];
                _stack.RemoveAt(i);
                item.SetActive(false);
            }
            ShowTop();
            LogStack($"Close {target.name}");
        }

        /// <summary>
        /// 해당 팝업 바로 아래에 쌓인 팝업 (없으면 null)
        /// </summary>
        public static GameObject GetPrevious(Component popup)
        {
            if (popup == null)
            {
                return null;
            }

            int index = _stack.IndexOf(popup.gameObject);
            return index > 0 ? _stack[index - 1] : null;
        }

        /// <summary>
        /// 맨 위 팝업 닫기
        /// </summary>
        public static void Back()
        {
            CleanUp();
            if (_stack.Count > 0)
            {
                Close(_stack[_stack.Count - 1].transform);
            }
        }

        /// <summary>
        /// 열린 팝업 모두 닫기 (헤더 X)
        /// </summary>
        public static void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i] != null)
                {
                    _stack[i].SetActive(false);
                }
            }
            _stack.Clear();
            LogStack("CloseAll");
        }

        /// <summary>
        /// 삭제된 팝업 제거 (씬 전환 등), 맨 위가 외부에서 닫혔으면 제거 후 이전 팝업 다시 표시
        /// </summary>
        private static void CleanUp()
        {
            _stack.RemoveAll(item => item == null);
            if (_stack.Count > 0 && !_stack[_stack.Count - 1].activeSelf)
            {
                _stack.RemoveAt(_stack.Count - 1);
                ShowTop();
            }
        }

        private static void ShowTop()
        {
            if (_stack.Count > 0)
            {
                _stack[_stack.Count - 1].SetActive(true);
            }
        }

        /// <summary>
        /// debug 로그
        /// </summary>
        private static void LogStack(string action)
        {
            if (!LogEnabled)
            {
                return;
            }

            int count = _stack.Count;
            string current = count > 0 ? _stack[count - 1].name : "없음";
            string previous = count > 1 ? _stack[count - 2].name : "없음";
            string stack = count > 0 ? string.Join(" > ", _stack.ConvertAll(item => item.name)) : "-";
            Debug.Log($"<color=yellow>PopupService: {action} | 현재 {current} | 이전 {previous} | 스택 {stack}</color>");
        }
    }
}
