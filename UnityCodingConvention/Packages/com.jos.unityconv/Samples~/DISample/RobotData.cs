using System;

namespace UnityConv.Samples.DI
{
    /// <summary>
    /// 로봇 상태 (서버 응답 형식, 샘플)
    /// </summary>
    [Serializable]
    public class RobotData
    {
        public string robot_name;
        public string status;
        public float battery_rate;

        public int GetBatteryPercent()
        {
            return (int)Math.Round(battery_rate * 100);
        }
    }
}
