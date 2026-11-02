using System.Threading;
using System.Threading.Tasks;

namespace UnityConv.Samples.DI
{
    /// <summary>
    /// 로봇 데이터 조회, 실패 / 취소 시 null (SceneBinder로 Sample / Api 구현을 고름)
    /// </summary>
    public interface IRobotDataSource
    {
        Task<RobotData> GetRobotAsync(string robotName, CancellationToken cancellationToken);
    }
}
