using UnityEditor;

namespace UnityConv.Network.EditorTools
{
    /// <summary>
    /// APIStore 인스펙터, 기본 서버를 서버 목록 이름으로 고르게 함
    /// </summary>
    [CustomEditor(typeof(APIStore))]
    public class APIStoreEditor : UnityEditor.Editor
    {
        private static readonly string[] ExcludedProperties = { "m_Script", "selectedServer" };

        private SerializedProperty _servers;
        private SerializedProperty _selectedServer;

        private void OnEnable()
        {
            _servers = serializedObject.FindProperty("servers");
            _selectedServer = serializedObject.FindProperty("selectedServer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            int count = _servers.arraySize;
            if (count > 0)
            {
                var names = new string[count];
                for (int i = 0; i < count; i++)
                {
                    string serverName = _servers.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue;
                    names[i] = string.IsNullOrEmpty(serverName) ? $"(이름 없음 {i})" : serverName;
                }
                _selectedServer.intValue = EditorGUILayout.Popup("Selected Server", _selectedServer.intValue, names);
            }
            else
            {
                EditorGUILayout.HelpBox("서버를 하나 이상 추가하세요 (예: Dev / Release).", MessageType.Warning);
            }

            DrawPropertiesExcluding(serializedObject, ExcludedProperties);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
