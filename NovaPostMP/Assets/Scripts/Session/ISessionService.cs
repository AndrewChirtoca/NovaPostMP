using System.Threading.Tasks;
using System.Collections.Generic;

public interface ISessionService
{
    bool IsBusy { get; }
    bool IsConnected { get; }
    bool CanStart { get; }
    bool IsLoading { get; }
    bool IsPlaying { get; }
    string ConnectionStatus { get; }
    IEnumerable<(string Name, bool Loaded, int RttMs)> Players { get; }

    Task<(bool Success, string Error)> HostAsync(string roomName, string playerName);
    Task<(bool Success, string Error)> JoinAsync(string roomName, string playerName);
    Task StartGameAsync();
    Task LeaveAsync();
}