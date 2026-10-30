using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityConv.Bases
{
    /// <summary>
    /// Manager 공통 부모, UI별 요청 토큰 관리 (같은 UI의 새 요청, UI 닫힘, Manager 꺼짐이면 취소)
    /// </summary>
    public abstract class ManagerBase : MonoBehaviour
    {
        private readonly Dictionary<(UIBase ui, string channel), CancellationTokenSource> _requests = new Dictionary<(UIBase ui, string channel), CancellationTokenSource>();

        protected virtual void OnDisable()
        {
            CancelAll();
        }

        protected virtual void OnDestroy()
        {
            CancelAll();
        }

        /// <summary>
        /// 연결된 UI에 Manager 알림, 빠진 UI는 경고
        /// </summary>
        protected void BindViews(params UIBase[] views)
        {
            foreach (UIBase view in views)
            {
                if (view == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] 연결되지 않은 UI 있음", this);
                    continue;
                }
                view.Bind(this);
            }
        }

        /// <summary>
        /// 조회 후 적용, 그사이 같은 UI(채널)에 새 요청이 오거나 UI가 닫히면 버림
        /// </summary>
        protected async void Request<T>(UIBase ui, Func<CancellationToken, Task<T>> fetch, Action<T> apply, string channel = null) where T : class
        {
            if (ui == null)
            {
                return;
            }

            var key = (ui, channel ?? string.Empty);
            if (_requests.TryGetValue(key, out CancellationTokenSource previous))
            {
                previous.Cancel();
            }

            var cts = new CancellationTokenSource();
            _requests[key] = cts;

            T data = null;
            bool isCanceled;
            try
            {
                data = await fetch(cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                // 조회 실패는 로그만 남기고 적용하지 않음
                Debug.LogException(e, this);
                data = null;
            }
            finally
            {
                isCanceled = cts.IsCancellationRequested;
                if (_requests.TryGetValue(key, out CancellationTokenSource current) && current == cts)
                {
                    _requests.Remove(key);
                }
                cts.Dispose();
            }

            if (this == null || isCanceled || data == null)
            {
                return;
            }

            try
            {
                apply(data);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        /// <summary>
        /// 해당 UI의 진행 중 요청 취소 (UI가 닫힐 때 호출)
        /// </summary>
        public void CancelRequests(UIBase ui)
        {
            var keys = new List<(UIBase ui, string channel)>();
            foreach (var pair in _requests)
            {
                if (pair.Key.ui == ui)
                {
                    pair.Value.Cancel();
                    keys.Add(pair.Key);
                }
            }
            foreach (var key in keys)
            {
                _requests.Remove(key);
            }
        }

        /// <summary>
        /// Manager가 Initialize하지 않고 열린 UI (패널 상세 버튼 등), 필요한 Manager만 조회
        /// </summary>
        public virtual void HandleUIOpened(UIBase ui)
        {
        }

        private void CancelAll()
        {
            foreach (CancellationTokenSource cts in _requests.Values)
            {
                cts.Cancel();
            }
            _requests.Clear();
        }
    }
}
