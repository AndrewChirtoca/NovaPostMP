using System.Threading.Tasks;

public interface ISessionService
{
    bool IsBusy { get; }
    bool IsConnected { get; }
    string Status { get; }

    Task<(bool Success, string Error)> HostAsync(string roomName);
    Task<(bool Success, string Error)> JoinAsync(string roomName);
    Task LeaveAsync();
}