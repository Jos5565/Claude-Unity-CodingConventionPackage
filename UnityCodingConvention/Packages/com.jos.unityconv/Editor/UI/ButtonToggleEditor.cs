using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UnityConv.UI.EditorTools
{
    /// <summary>
    /// ButtonToggle 인스펙터, 모드에 따라 버튼 이벤트 / 토글 설정을 표시 (모든 변경은 SerializedProperty로 Undo, 저장)
    /// </summary>
    [CustomEditor(typeof(ButtonToggle), true)]
    [CanEditMultipleObjects]
    public class ButtonToggleEditor : SelectableEditor
    {
        private static readonly string[] ModeLabels = { "Button", "Toggle" };

        private SerializedProperty _isToggle;
        private SerializedProperty _isOn;
        private SerializedProperty _group;
        private SerializedProperty _offSprite;
        private SerializedProperty _onSprite;
        private SerializedProperty _offColor;
        private SerializedProperty _onColor;
        private SerializedProperty _onClick;
        private SerializedProperty _onValueChanged;

        protected override void OnEnable()
        {
            base.OnEnable();
            _isToggle = serializedObject.FindProperty("m_IsToggle");
            _isOn = serializedObject.FindProperty("m_IsOn");
            _group = serializedObject.FindProperty("m_Group");
            _offSprite = serializedObject.FindProperty("m_OffSprite");
            _onSprite = serializedObject.FindProperty("m_OnSprite");
            _offColor = serializedObject.FindProperty("m_OffColor");
            _onColor = serializedObject.FindProperty("m_OnColor");
            _onClick = serializedObject.FindProperty("m_OnClick");
            _onValueChanged = serializedObject.FindProperty("m_OnValueChanged");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            serializedObject.Update();
            DrawMode();

            if (_isToggle.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("선택한 오브젝트의 모드가 서로 다릅니다.", MessageType.Info);
            }
            else if (_isToggle.boolValue)
            {
                DrawToggle();
            }
            else
            {
                EditorGUILayout.PropertyField(_onClick);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMode()
        {
            EditorGUI.showMixedValue = _isToggle.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int mode = GUILayout.Toolbar(_isToggle.boolValue ? 1 : 0, ModeLabels);
            EditorGUI.showMixedValue = false;
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            // 속성으로 전환해야 버튼 모드에서 Off 모습 복원, 그룹 처리가 같이 동작 (그래픽 변경도 Undo에 기록)
            serializedObject.ApplyModifiedProperties();
            bool toToggle = mode == 1;
            foreach (Object t in targets)
            {
                ButtonToggle toggle = (ButtonToggle)t;
                Graphic graphic = toggle.toggleGraphic;
                Object[] records = graphic != null ? new Object[] { toggle, graphic } : new Object[] { toggle };
                Undo.RecordObjects(records, "Change Button Toggle Mode");
                toggle.isToggle = toToggle;
                foreach (Object record in records)
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(record);
                    EditorUtility.SetDirty(record);
                }
            }
            serializedObject.Update();
        }

        private void DrawToggle()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_isOn, new GUIContent("Is On"));
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                // 플레이 중에는 그룹 처리와 이벤트가 동작하도록 속성으로 적용
                bool value = _isOn.boolValue;
                serializedObject.ApplyModifiedProperties();
                foreach (Object t in targets)
                {
                    ButtonToggle toggle = (ButtonToggle)t;
                    toggle.SetIsOnWithoutNotify(!value);
                    toggle.isOn = value;
                }
                serializedObject.Update();
            }
            EditorGUILayout.PropertyField(_group, new GUIContent("Toggle Group"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Toggle Graphic", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_offColor, new GUIContent("Off Color"));
            EditorGUILayout.PropertyField(_onColor, new GUIContent("On Color"));
            EditorGUILayout.PropertyField(_offSprite, new GUIContent("Off Sprite"));
            EditorGUILayout.PropertyField(_onSprite, new GUIContent("On Sprite"));
            if (!_onSprite.hasMultipleDifferentValues && _onSprite.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("On Sprite가 비어 있으면 색만 바뀝니다.", MessageType.None);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_onValueChanged, new GUIContent("On Value Changed (Boolean)"));
        }
    }
}
