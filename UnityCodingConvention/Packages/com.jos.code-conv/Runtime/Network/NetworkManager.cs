using UnityEngine;

namespace UnityConv.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public APIStore store;
        private static NetworkManager instance;

        private void Awake()
        {
            APIService.store = store;
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
