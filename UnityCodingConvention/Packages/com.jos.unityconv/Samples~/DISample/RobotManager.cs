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
        [SerializeField] private RobotInfoUI robotInfoUI;

        [Header("조회 조건")]
        [SerializeField] private string defaultRobotName = "AGV-01";

        // Sample Binder면 RobotSampleSource, Api Binder면 RobotApiSource
        [Inject] private IRobotDataSource dataSource;

        private void Awake()
        {
            BindViews(robotInfoUI);
        }

        private void Start()
        {
            OpenRobotInfo(defaultRobotName);
        }

        public void OpenRobotInfo(string robotName)
        {
            if (dataSource == null || string.IsNullOrEmpty(robotName)) return;
            Request(robotInfoUI, token => dataSource.GetRobotAsync(robotName, token), robot => robotInfoUI.Initialize(this, robot));
        }
    }
}
