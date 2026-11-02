using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityConv.DI
{
    /// <summary>
    /// 씬 컨테이너에 등록할 목록, 클래스는 기본 생성자로 만들고 에셋(APIStore 등)은 그대로 등록
    /// Sample용 / Api용 바인더를 따로 만들어 SceneInstaller에서 바꿔 끼운다.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneBinder", menuName = "UnityConv/DI/Scene Binder")]
    public class SceneBinder : ScriptableObject
    {
#if UNITY_EDITOR
        [Tooltip("new로 만들 클래스 (기본 생성자, MonoBehaviour / ScriptableObject 아님). 예: XxxSampleSource")]
        [SerializeField] private List<UnityEditor.MonoScript> m_Classes = new List<UnityEditor.MonoScript>();
#endif
        [SerializeField, HideInInspector] private List<string> m_ClassNames = new List<string>();

        [Tooltip("그대로 등록할 에셋 (ScriptableObject 등)")]
        [SerializeField] private List<ScriptableObject> m_Assets = new List<ScriptableObject>();

        /// <summary>
        /// 에셋과 클래스 인스턴스를 등록하고, 만든 인스턴스끼리의 [Inject] 필드도 채움
        /// </summary>
        public void Bind(SceneContainer container)
        {
            foreach (ScriptableObject asset in m_Assets)
            {
                container.Bind(asset);
            }

            var created = new List<object>();
            foreach (string className in m_ClassNames)
            {
                Type type = FindType(className);
                if (type == null)
                {
                    Debug.LogError($"[DI] {name}: 클래스를 찾지 못했습니다 ({className}). 바인더를 열어 목록을 확인하세요.", this);
                    continue;
                }

                try
                {
                    object instance = Activator.CreateInstance(type);
                    container.Bind(instance, true);
                    created.Add(instance);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DI] {name}: {type.Name}을(를) 만들지 못했습니다. 기본 생성자가 있는 일반 클래스여야 합니다. ({e.Message})", this);
                }
            }

            foreach (object instance in created)
            {
                container.Inject(instance);
            }
        }

        /// <summary>
        /// "FullName, Assembly"로 찾고, 어셈블리가 바뀌었으면 FullName으로 다시 찾음
        /// </summary>
        private static Type FindType(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return null;
            }

            Type type = Type.GetType(className);
            if (type != null)
            {
                return type;
            }

            string fullName = className.Split(',')[0].Trim();
            // Unity 6000.0도 지원하려고 AppDomain 사용 (어셈블리 이동 시 한 번만 거치는 경로)
#pragma warning disable UAC0005
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
#pragma warning restore UAC0005
            {
                type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터의 스크립트 목록을 런타임용 이름 목록으로 저장 (빌드에는 MonoScript가 없음)
        /// </summary>
        private void OnValidate()
        {
            var names = new List<string>();
            foreach (UnityEditor.MonoScript script in m_Classes)
            {
                if (script == null)
                {
                    continue;
                }

                Type type = script.GetClass();
                if (type == null || type.IsAbstract || typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"[DI] {name}: {script.name}은(는) 등록할 수 없습니다. 기본 생성자가 있는 일반 클래스만 넣으세요 (에셋은 Assets 목록에).", this);
                    continue;
                }
                names.Add($"{type.FullName}, {type.Assembly.GetName().Name}");
            }

            if (!names.SequenceEqual(m_ClassNames))
            {
                m_ClassNames = names;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
