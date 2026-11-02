using System.Threading;
using System.Threading.Tasks;
using UnityConv.Network;

namespace UnityConv.Samples.DI
{
    /// <summary>
    /// 서버 조회 (Api Binder에 등록), APIStore에 "robotDetail" 경로 등록 필요 (예: /api/robots/{0}.json)
    /// </summary>
    public class RobotApiSource : IRobotDataSource
    {
        public Task<RobotData> GetRobotAsync(string robotName, CancellationToken cancellationToken)
        {
            return APIService.GetAsync<RobotData>("robotDetail", new object[] { robotName }, cancellationToken);
        }
    }
}
