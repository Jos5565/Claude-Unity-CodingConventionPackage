using UnityConv.Bases;
using UnityConv.DI;
using UnityEngine;

namespace UnityConv.Samples.DI
{
    /// <summary>
    /// DataSource를 SceneInstaller에서 주입받아 조회하고 UI에 전달 (샘플)
    /// </summary>
    public class RobotManager : ManagerBase
    {
        [Header("UI")]
        [SerializeField] private RobotInfoUI ui_robotInfo;

        [Header("조회 조건")]
        [SerializeField] private string m_RobotName = "AGV-01";

        // Sample Binder면 RobotSampleSource, Api Binder면 RobotApiSource
        [Inject] private IRobotDataSource _source;

        private void Awake()
        {
            BindViews(ui_robotInfo);
        }

        private void Start()
        {
            OpenRobotInfo(m_RobotName);
        }

        public void OpenRobotInfo(string robotName)
        {
            if (_source == null || string.IsNullOrEmpty(robotName)) return;
            Request(ui_robotInfo, token => _source.GetRobotAsync(robotName, token), robot => ui_robotInfo.Initialize(this, robot));
        }
    }
}
