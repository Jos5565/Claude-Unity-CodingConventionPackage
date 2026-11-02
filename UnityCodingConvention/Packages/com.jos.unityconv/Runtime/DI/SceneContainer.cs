using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityConv.DI
{
    /// <summary>
    /// 씬 하나의 인스턴스를 타입, 인터페이스별로 보관하고 [Inject] 필드에 넣음
    /// </summary>
    public class SceneContainer : IDisposable
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        // 타입별 [Inject] 필드 (부모 클래스 필드 포함)
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();

        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private readonly List<IDisposable> _owned = new List<IDisposable>();

        /// <summary>
        /// 등록 로그 출력 여부
        /// </summary>
        public static bool LogEnabled { get; set; }

        /// <summary>
        /// 실제 타입과 직접 만든 인터페이스로 등록, owned면 컨테이너가 정리할 때 Dispose
        /// </summary>
        public void Bind(object instance, bool owned = false)
        {
            if (IsNull(instance))
            {
                return;
            }

            Type type = instance.GetType();
            Register(type, instance);
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (IsBindableInterface(interfaceType))
                {
                    Register(interfaceType, instance);
                }
            }

            if (owned && instance is IDisposable disposable)
            {
                _owned.Add(disposable);
            }
        }

        public object Resolve(Type type)
        {
            return _instances.TryGetValue(type, out object instance) ? instance : null;
        }

        public T Resolve<T>() where T : class
        {
            return Resolve(typeof(T)) as T;
        }

        /// <summary>
        /// 대상의 [Inject] 필드를 채움, 등록이 없으면 경고
        /// </summary>
        public void Inject(object target)
        {
            if (IsNull(target))
            {
                return;
            }

            foreach (FieldInfo field in GetInjectFields(target.GetType()))
            {
                object value = Resolve(field.FieldType);
                if (value == null)
                {
                    Debug.LogWarning($"[DI] {target.GetType().Name}.{field.Name}에 넣을 {field.FieldType.Name}이(가) 등록돼 있지 않습니다.", target as UnityEngine.Object);
                    continue;
                }
                field.SetValue(target, value);
            }
        }

        public static bool HasInjectFields(Type type)
        {
            return GetInjectFields(type).Length > 0;
        }

        /// <summary>
        /// 컨테이너가 만든 인스턴스 정리 (등록 역순)
        /// </summary>
        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                try
                {
                    _owned[i].Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            _owned.Clear();
            _instances.Clear();
        }

        private void Register(Type type, object instance)
        {
            if (_instances.TryGetValue(type, out object existing) && !ReferenceEquals(existing, instance))
            {
                // 먼저 등록한 것을 유지
                Debug.LogWarning($"[DI] {type.Name}이(가) 이미 {existing.GetType().Name}(으)로 등록돼 있어 {instance.GetType().Name}은(는) 이 타입으로 등록하지 않습니다.");
                return;
            }

            _instances[type] = instance;
            if (LogEnabled)
            {
                Debug.Log($"[DI] {instance.GetType().Name} → {type.Name}");
            }
        }

        private static FieldInfo[] GetInjectFields(Type type)
        {
            if (FieldCache.TryGetValue(type, out FieldInfo[] cached))
            {
                return cached;
            }

            var fields = new List<FieldInfo>();
            for (Type current = type; current != null && current != typeof(MonoBehaviour) && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(FieldFlags))
                {
                    if (field.IsDefined(typeof(InjectAttribute), false))
                    {
                        fields.Add(field);
                    }
                }
            }

            FieldInfo[] result = fields.ToArray();
            FieldCache[type] = result;
            return result;
        }

        // System, Unity 인터페이스(IDisposable, ISerializationCallbackReceiver 등)는 등록하지 않음
        private static bool IsBindableInterface(Type interfaceType)
        {
            string ns = interfaceType.Namespace;
            if (string.IsNullOrEmpty(ns))
            {
                return true;
            }
            return !(ns == "System" || ns.StartsWith("System.") || ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor") || ns.StartsWith("Unity."));
        }

        // Unity 오브젝트는 fake null 확인
        private static bool IsNull(object value)
        {
            if (value is UnityEngine.Object unityObject)
            {
                return unityObject == null;
            }
            return value == null;
        }
    }
}
