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

        /// <summary>
        /// 이 UI를 초기화한 Manager
        /// </summary>
        protected ManagerBase Owner { get; private set; }

        /// <summary>
        /// 지금 화면에 켜져 있는지
        /// </summary>
        public bool IsShown { get; private set; }

        /// <summary>
        /// 팝업이면 Initialize 때 PopupService로 열고 리셋, 위젯은 데이터만 적용
        /// </summary>
        protected virtual bool IsPopup => true;

        // * 자식은 OnEnable/OnDisable 대신 OnOpened/OnClosed에서 구독, 해제
        private void OnEnable()
        {
            IsShown = true;
            OnOpened();
            if (!_isOpening && Owner != null)
            {
                Owner.HandleUIOpened(this);
            }
        }

        private void OnDisable()
        {
            IsShown = false;
            _needsReset = true;
            OnClosed();
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

            if (!IsShown)
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
