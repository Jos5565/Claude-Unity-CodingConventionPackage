using UnityEngine;

namespace UnityConv.Samples.UI
{
    /// <summary>
    /// 샘플 씬 이벤트 확인용, ButtonToggle의 onClick / onValueChanged에 연결해 로그 출력
    /// </summary>
    public class ButtonToggleSampleLog : MonoBehaviour
    {
        public void Log(string message)
        {
            Debug.Log($"[ButtonToggleSample] {message}", this);
        }

        public void LogValue(bool isOn)
        {
            Debug.Log($"[ButtonToggleSample] isOn = {isOn}", this);
        }
    }
}
