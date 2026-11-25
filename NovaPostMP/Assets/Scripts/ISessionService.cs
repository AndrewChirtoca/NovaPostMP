using System.Threading.Tasks;
using Fusion;

public interface ISessionService
{
    bool IsBusy { get; }
    bool IsConnected { get; }
    string Status { get; }

    Task<(bool Success, string Error)> HostAsync(string roomName);
    Task<(bool Success, string Error)> JoinAsync(string roomName);
    Task LeaveAsync();
}

public class FusionSessionSettings
{
    public NetworkRunner RunnerPrefab;
    public int InitialSceneIndex;
}