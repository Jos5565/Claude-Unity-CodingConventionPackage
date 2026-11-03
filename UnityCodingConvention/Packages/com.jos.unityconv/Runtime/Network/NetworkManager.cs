using System;
using UnityEngine;

namespace UnityConv.Network
{
    /// <summary>
    /// v2.5.0부터 필요 없음: APIService가 Resources/APIConfig를 직접 읽는다. 다음 버전에서 삭제
    /// </summary>
    [Obsolete("APIStore 에셋을 Resources/APIConfig로 두고 NetworkManager는 씬에서 지우세요.")]
    [AddComponentMenu("")]
    public class NetworkManager : MonoBehaviour
    {
        public APIStore store;

        private void Awake()
        {
            Debug.LogWarning("[UnityConv] NetworkManager는 더 이상 필요 없습니다. APIStore 에셋을 Resources/APIConfig로 두고 이 오브젝트를 지우세요.", this);
            // 기존 씬 호환: Resources에 APIConfig가 없을 때만 이 store를 씀
            if (store != null && APIService.Store == null)
            {
                APIService.Store = store;
            }
        }
    }
}
