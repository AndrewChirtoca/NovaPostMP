using System.Threading.Tasks;
using System.Collections.Generic;

public interface ISessionService
{
    bool IsBusy { get; }
    bool IsConnected { get; }
    string Status { get; }
    IEnumerable<string> Players { get; }

    Task<(bool Success, string Error)> HostAsync(string roomName);
    Task<(bool Success, string Error)> JoinAsync(string roomName);
    Task LeaveAsync();
}