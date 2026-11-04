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
        [SerializeField] private Text infoText;

        [Header("문구")]
        [SerializeField] private string infoFormat = "{0}  /  {1}  /  배터리 {2}%";

        protected override bool IsPopup => false;

        public override void SetData(RobotData robot)
        {
            string info = string.Format(infoFormat, robot.robot_name, robot.status, robot.GetBatteryPercent());
            if (infoText != null) infoText.text = info;
            Debug.Log($"[DI Sample] {info}");
        }
    }
}
