using UnityConv.Bases;
using UnityEngine;
using UnityEngine.UI;

namespace UnityConv.Samples.DI
{
    /// <summary>
    /// 로봇 상태 위젯 (받은 Data로 그리기만, [Inject] 쓰지 않음)
    /// </summary>
    public class RobotInfoUI : UIBase<RobotData>
    {
        [Header("UI")]
        [SerializeField] private Text text_info;

        [Header("문구")]
        [SerializeField] private string m_Format = "{0}  /  {1}  /  배터리 {2}%";

        protected override bool IsPopup => false;

        public override void SetData(RobotData robot)
        {
            string info = string.Format(m_Format, robot.robot_name, robot.status, robot.GetBatteryPercent());
            if (text_info != null) text_info.text = info;
            Debug.Log($"[DI Sample] {info}");
        }
    }
}
