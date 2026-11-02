using System.Collections.Generic;
using UnityConv.Bases;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityConv.DI
{
    /// <summary>
    /// 씬마다 하나, 다른 스크립트의 Awake 전에 씬 컨테이너를 만들고 이 씬 오브젝트의 [Inject] 필드를 채움
    /// 씬이 내려가면 컨테이너도 정리 (DontDestroyOnLoad 하지 않음)
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("UnityConv/DI/Scene Installer")]
    public class SceneInstaller : MonoBehaviour
    {
        private static readonly Dictionary<Scene, SceneContainer> Containers = new Dictionary<Scene, SceneContainer>();

        [Tooltip("등록할 클래스, 에셋 목록 (Sample / Api 바인더를 바꿔 끼움)")]
        [SerializeField] private SceneBinder m_Binder;
        [Tooltip("그대로 등록할 이 씬의 컴포넌트")]
        [SerializeField] private List<Component> m_SceneObjects = new List<Component>();

        private SceneContainer _container;
        private Transform _spawnRoot; // Instantiate 시 Awake 전에 주입하려고 비활성으로 둔 부모

        public SceneContainer Container => _container;

        private void Awake()
        {
            Scene scene = gameObject.scene;
            if (Containers.ContainsKey(scene))
            {
                Debug.LogError($"[DI] {scene.name} 씬에 SceneInstaller가 둘 이상 있습니다. 하나만 두세요.", this);
                return;
            }

            _container = new SceneContainer();
            Containers[scene] = _container;

            if (m_Binder != null)
            {
                m_Binder.Bind(_container);
            }
            foreach (Component sceneObject in m_SceneObjects)
            {
                _container.Bind(sceneObject);
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                InjectHierarchy(_container, root);
            }
        }

        private void OnDestroy()
        {
            if (_container == null)
            {
                return;
            }

            Scene scene = gameObject.scene;
            if (Containers.TryGetValue(scene, out SceneContainer current) && current == _container)
            {
                Containers.Remove(scene);
            }
            _container.Dispose();
            _container = null;
        }

        /// <summary>
        /// 실행 중에 만든 오브젝트에 주입 (오브젝트가 속한 씬의 컨테이너 사용), Awake에서 써야 하면 InstantiateAndInject를 쓴다
        /// </summary>
        public static void Inject(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            if (!Containers.TryGetValue(root.scene, out SceneContainer container))
            {
                Debug.LogWarning($"[DI] {root.scene.name} 씬에 SceneInstaller가 없어 {root.name}에 주입하지 않았습니다.", root);
                return;
            }
            InjectHierarchy(container, root);
        }

        /// <summary>
        /// 프리팹을 만들고 Awake 전에 주입, parent가 없으면 지금 활성 씬에 만듦
        /// </summary>
        public static GameObject InstantiateAndInject(GameObject prefab, Transform parent)
        {
            Scene scene = parent != null ? parent.gameObject.scene : SceneManager.GetActiveScene();
            SceneInstaller installer = Find(scene);
            if (installer == null)
            {
                Debug.LogWarning($"[DI] {scene.name} 씬에 SceneInstaller가 없어 주입 없이 만듭니다.", prefab);
                return Object.Instantiate(prefab, parent);
            }

            // 비활성 부모 아래에서 만들면 Awake가 미뤄짐 → 주입 → 원래 부모로 옮기면 Awake
            GameObject instance = Object.Instantiate(prefab, installer.GetSpawnRoot(), false);
            InjectHierarchy(installer._container, instance);
            instance.transform.SetParent(parent, false);
            if (parent == null)
            {
                SceneManager.MoveGameObjectToScene(instance, scene);
            }
            return instance;
        }

        public static T InstantiateAndInject<T>(T prefab, Transform parent) where T : Component
        {
            GameObject instance = InstantiateAndInject(prefab.gameObject, parent);
            return instance.GetComponent<T>();
        }

        private static SceneInstaller Find(Scene scene)
        {
            if (!Containers.ContainsKey(scene))
            {
                return null;
            }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SceneInstaller installer = root.GetComponentInChildren<SceneInstaller>(true);
                if (installer != null && installer._container != null)
                {
                    return installer;
                }
            }
            return null;
        }

        private Transform GetSpawnRoot()
        {
            if (_spawnRoot == null)
            {
                var spawnRoot = new GameObject("[DI Spawn]");
                spawnRoot.SetActive(false);
                spawnRoot.transform.SetParent(transform, false);
                _spawnRoot = spawnRoot.transform;
            }
            return _spawnRoot;
        }

        private static void InjectHierarchy(SceneContainer container, GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                // Missing Script는 null
                if (behaviour == null || !SceneContainer.HasInjectFields(behaviour.GetType()))
                {
                    continue;
                }

                if (behaviour is UIBase)
                {
                    Debug.LogError($"[DI] UI({behaviour.GetType().Name})에는 [Inject]를 쓰지 않습니다. 필요한 값은 Manager가 받아 Initialize로 넘기세요.", behaviour);
                    continue;
                }
                container.Inject(behaviour);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Containers.Clear();
        }
    }
}
