using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UnityConv.UI
{
    /// <summary>
    /// Button과 Toggle을 함께 쓰는 Selectable, 버튼 모드는 onClick / 토글 모드는 onValueChanged와 On/Off 그래픽 교체
    /// </summary>
    [AddComponentMenu("UI/Button Toggle")]
    public class ButtonToggle : Selectable, IPointerClickHandler, ISubmitHandler
    {
        // * 인스펙터 구성(구역 제목, 모드별 표시)은 ButtonToggleEditor가 담당
        [FormerlySerializedAs("isToggle")]
        [SerializeField] private bool m_IsToggle;
        [SerializeField] private bool m_IsOn;
        [SerializeField] private ToggleGroup m_Group;

        // * 대부분 Target Graphic(자기 자신)을 쓰므로 인스펙터에 노출하지 않음, 다른 그래픽이 필요하면 코드에서 toggleGraphic 지정
        [HideInInspector]
        [SerializeField] private Graphic m_ToggleGraphic;
        [FormerlySerializedAs("toggleDefaultSprite")]
        [SerializeField] private Sprite m_OffSprite;
        [Tooltip("지정하면 On/Off 스프라이트 교체, 비우면 스프라이트는 그대로")]
        [FormerlySerializedAs("toggleCheckSprite")]
        [SerializeField] private Sprite m_OnSprite;
        [SerializeField] private Color m_OffColor = Color.white;
        [FormerlySerializedAs("toggleCheckColor")]
        [SerializeField] private Color m_OnColor = Color.white;

        [FormerlySerializedAs("OnClick")]
        [SerializeField] private UnityEvent m_OnClick = new UnityEvent();
        [FormerlySerializedAs("IsOn")]
        [SerializeField] private UnityEvent<bool> m_OnValueChanged = new UnityEvent<bool>();

        /// <summary>
        /// 버튼 모드 클릭 이벤트
        /// </summary>
        public UnityEvent onClick => m_OnClick;

        /// <summary>
        /// 토글 모드 값 변경 이벤트 (바뀐 값 전달)
        /// </summary>
        public UnityEvent<bool> onValueChanged => m_OnValueChanged;

        /// <summary>
        /// true면 토글 모드, 버튼 모드로 바꾸면 Off 모습(Normal Color 기준)으로 되돌리고 그룹 처리에서 빠짐
        /// </summary>
        public bool isToggle
        {
            get => m_IsToggle;
            set
            {
                if (m_IsToggle == value)
                {
                    return;
                }

                m_IsToggle = value;
                if (!m_IsToggle)
                {
                    ApplyOffGraphic();
                    return;
                }

                // 켜진 채로 토글 모드가 되면 같은 그룹의 다른 토글을 끔
                if (m_IsOn && m_Group != null && IsActive())
                {
                    ButtonToggleGroupRegistry.NotifyToggleOn(m_Group, this, true);
                }
                UpdateToggleGraphic();
            }
        }

        /// <summary>
        /// 토글 값, 바뀌면 그래픽 갱신 + onValueChanged
        /// </summary>
        public bool isOn
        {
            get => m_IsOn;
            set => Set(value, true);
        }

        /// <summary>
        /// 소속 ToggleGroup (같은 그룹 안에서 하나만 켜짐)
        /// </summary>
        public ToggleGroup group
        {
            get => m_Group;
            set => SetToggleGroup(value, true);
        }

        /// <summary>
        /// On/Off에 따라 바꿀 Graphic (Image / RoundImage), 지정하지 않으면 Target Graphic
        /// </summary>
        public Graphic toggleGraphic
        {
            get => m_ToggleGraphic != null ? m_ToggleGraphic : targetGraphic;
            set
            {
                m_ToggleGraphic = value;
                UpdateToggleGraphic();
            }
        }

        protected ButtonToggle()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetToggleGroup(m_Group, false);
            UpdateToggleGraphic();
        }

        protected override void OnDisable()
        {
            SetToggleGroup(null, false);
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            // 추가할 때 현재 모습을 Off 상태로 저장
            Graphic graphic = toggleGraphic;
            if (graphic != null)
            {
                m_OffColor = graphic.color;
                m_OffSprite = GetSprite(graphic);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateToggleGraphic();
        }
#endif

        /// <summary>
        /// 이벤트 없이 값만 변경 (그래픽은 갱신)
        /// </summary>
        public void SetIsOnWithoutNotify(bool value)
        {
            Set(value, false);
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            Press();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();

            if (!IsActive() || !IsInteractable())
            {
                return;
            }

            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(OnFinishSubmit());
        }

        private void Press()
        {
            if (!IsActive() || !IsInteractable())
            {
                return;
            }

            if (m_IsToggle)
            {
                isOn = !m_IsOn;
                return;
            }

            UISystemProfilerApi.AddMarker("ButtonToggle.onClick", this);
            m_OnClick.Invoke();
        }

        private IEnumerator OnFinishSubmit()
        {
            float fadeTime = colors.fadeDuration;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }

        private void Set(bool value, bool sendCallback)
        {
            if (m_IsOn == value)
            {
                return;
            }

            m_IsOn = value;
            // 버튼 모드는 그룹 처리에서 빠짐 (값만 보관, 토글 모드로 돌아오면 사용)
            if (m_IsToggle && m_Group != null && m_Group.isActiveAndEnabled && IsActive())
            {
                // 그룹이 끄기를 허용하지 않으면 마지막 하나는 꺼지지 않음
                if (m_IsOn || (!m_Group.AnyButtonTogglesOn() && !m_Group.allowSwitchOff))
                {
                    m_IsOn = true;
                    ButtonToggleGroupRegistry.NotifyToggleOn(m_Group, this, sendCallback);
                }
            }

            UpdateToggleGraphic();

            // 꺼지려다 그룹 규칙으로 다시 켜진 경우는 값이 그대로라 알리지 않음
            if (sendCallback && m_IsOn == value)
            {
                UISystemProfilerApi.AddMarker("ButtonToggle.onValueChanged", this);
                m_OnValueChanged.Invoke(m_IsOn);
            }
        }

        private void SetToggleGroup(ToggleGroup newGroup, bool setMemberValue)
        {
            // OnDisable에서는 IsActive가 false일 수 있어 확인 없이 해제
            if (m_Group != null)
            {
                ButtonToggleGroupRegistry.Unregister(m_Group, this);
            }

            // OnEnable / OnDisable에서는 필드를 유지하고 등록만 바꿈
            if (setMemberValue)
            {
                m_Group = newGroup;
            }

            if (newGroup != null && IsActive())
            {
                ButtonToggleGroupRegistry.Register(newGroup, this);
                if (m_IsToggle && m_IsOn)
                {
                    ButtonToggleGroupRegistry.NotifyToggleOn(newGroup, this, true);
                }
            }
        }

        private void UpdateToggleGraphic()
        {
            Graphic graphic = toggleGraphic;
            if (!m_IsToggle || graphic == null)
            {
                return;
            }

            graphic.color = m_IsOn ? m_OnColor : m_OffColor;
            if (m_OnSprite != null)
            {
                SetSprite(graphic, m_IsOn ? m_OnSprite : m_OffSprite);
            }
        }

        /// <summary>
        /// 버튼 모드 전환 시 Off 모습으로 복원 (Selectable 전환 색이 그 위에 곱해져 Normal Color로 보임)
        /// </summary>
        private void ApplyOffGraphic()
        {
            Graphic graphic = toggleGraphic;
            if (graphic == null)
            {
                return;
            }

            graphic.color = m_OffColor;
            if (m_OnSprite != null)
            {
                SetSprite(graphic, m_OffSprite);
            }
        }

        private static Sprite GetSprite(Graphic graphic)
        {
            if (graphic is Image image)
            {
                return image.sprite;
            }
            if (graphic is RoundImage roundImage)
            {
                return roundImage.sprite;
            }
            return null;
        }

        private static void SetSprite(Graphic graphic, Sprite sprite)
        {
            if (graphic is Image image)
            {
                image.sprite = sprite;
            }
            else if (graphic is RoundImage roundImage)
            {
                roundImage.sprite = sprite;
            }
        }
    }
}
