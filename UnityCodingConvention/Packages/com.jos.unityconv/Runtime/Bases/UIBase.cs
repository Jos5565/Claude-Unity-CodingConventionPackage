using UnityEngine;

namespace UnityConv.Bases
{
    /// <summary>
    /// UI 공통 부모, Manager가 Initialize로 열고 데이터 적용, 닫히면 Manager에 알려 요청 취소
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        private bool _isOpening; // Initialize가 여는 중 (Manager에 다시 알리지 않음)
        private bool _needsReset = true; // 닫힌 뒤 처음 Initialize면 화면 상태 리셋
        private bool _isCovered; // 위 팝업에 가려져 꺼짐 (닫힘 아님, 요청과 화면 상태 유지)

        /// <summary>
        /// 이 UI를 초기화한 Manager
        /// </summary>
        protected ManagerBase Owner { get; private set; }

        /// <summary>
        /// 지금 화면에 켜져 있는지
        /// </summary>
        public bool IsShown { get; private set; }

        /// <summary>
        /// 위 팝업에 가려져 꺼져 있는지 (닫힌 것은 아님)
        /// </summary>
        public bool IsCovered => _isCovered;

        /// <summary>
        /// 팝업이면 Initialize 때 PopupService로 열고 리셋, 위젯은 데이터만 적용
        /// </summary>
        protected virtual bool IsPopup => true;

        // * 자식은 OnEnable/OnDisable 대신 OnOpened/OnClosed에서 구독, 해제
        private void OnEnable()
        {
            IsShown = true;
            bool isReturning = _isCovered; // 가려졌다 돌아옴, 다시 조회하지 않음
            _isCovered = false;
            OnOpened();
            if (!_isOpening && !isReturning && Owner != null)
            {
                Owner.HandleUIOpened(this);
            }
        }

        private void OnDisable()
        {
            IsShown = false;
            OnClosed();
            if (PopupService.IsCovered(gameObject))
            {
                _isCovered = true;
                return;
            }
            Close();
        }

        /// <summary>
        /// 가려진 채로 스택에서 닫힘 (OnDisable이 오지 않아 PopupService가 호출)
        /// </summary>
        internal void HandleClosedWhileCovered()
        {
            _isCovered = false;
            Close();
        }

        private void Close()
        {
            _needsReset = true;
            if (Owner != null)
            {
                Owner.CancelRequests(this);
            }
        }

        /// <summary>
        /// Manager 연결 (Manager 초기화 때 호출)
        /// </summary>
        public void Bind(ManagerBase owner)
        {
            Owner = owner;
        }

        protected virtual void OnOpened()
        {
        }

        protected virtual void OnClosed()
        {
        }

        /// <summary>
        /// 팝업 열기, 기본은 PopupService 스택 (스택 없이 켜는 팝업은 오버라이드, 같은 프레임에 켤 것)
        /// </summary>
        protected virtual void Show()
        {
            PopupService.Push(this);
        }

        /// <summary>
        /// 닫힌 뒤 처음 열 때 화면 상태 초기화 (스크롤, 접힘 등)
        /// </summary>
        protected virtual void ResetView()
        {
        }

        /// <summary>
        /// 닫혀 있으면 열고, 닫힌 뒤 처음이면 리셋
        /// </summary>
        protected void PrepareOpen(ManagerBase owner)
        {
            Owner = owner;
            if (!IsPopup)
            {
                return;
            }

            // 가려진 팝업은 앞으로 꺼내지 않고 데이터만 적용
            if (!IsShown && !_isCovered)
            {
                _isOpening = true;
                Show();
                _isOpening = false;
            }
            if (_needsReset)
            {
                _needsReset = false;
                ResetView();
            }
        }
    }

    /// <summary>
    /// 받은 데이터로 그리는 UI
    /// </summary>
    public abstract class UIBase<TData> : UIBase where TData : class
    {
        /// <summary>
        /// Manager가 데이터를 다 받은 뒤 호출, 닫혀 있으면 열기 + 리셋 후 SetData, 열려 있으면 SetData만
        /// </summary>
        public void Initialize(ManagerBase owner, TData data)
        {
            PrepareOpen(owner);
            SetData(data);
        }

        public abstract void SetData(TData data);
    }
}
