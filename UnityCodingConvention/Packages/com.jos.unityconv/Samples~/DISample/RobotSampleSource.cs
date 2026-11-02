using System;
using System.Threading;
using System.Threading.Tasks;
using UnityConv.Network;
using UnityEngine;

namespace UnityConv.Samples.DI
{
    /// <summary>
    /// 서버와 같은 응답 형식의 샘플 JSON으로 응답 (Sample Binder에 등록)
    /// </summary>
    public class RobotSampleSource : IRobotDataSource
    {
        private const string SampleJson = "{\"success\":true,\"code\":\"200\",\"message\":\"\",\"data\":{\"robot_name\":\"{0}\",\"status\":\"RUN\",\"battery_rate\":0.82}}";

        public async Task<RobotData> GetRobotAsync(string robotName, CancellationToken cancellationToken)
        {
            try
            {
                // 서버 지연 흉내
                await Task.Delay(300, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            APIResponse<RobotData> response = JsonUtility.FromJson<APIResponse<RobotData>>(SampleJson.Replace("{0}", robotName));
            return response != null && response.success ? response.data : null;
        }
    }
}
