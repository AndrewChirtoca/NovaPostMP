using System.Threading.Tasks;
using Fusion;

public class FusionSessionService : ISessionService
{
    private readonly FusionSessionSettings _settings;
    private NetworkRunner _runner;

    public bool IsBusy { get; private set; }
    public bool IsConnected => _runner != null && _runner.IsRunning;
    public string Status { get; private set; } = "Disconnected";

    public FusionSessionService(FusionSessionSettings settings)
    {
        _settings = settings;
    }
    
    public Task<(bool Success, string Error)> HostAsync(string room) => ConnectAsync(GameMode.Host, room);

    public Task<(bool Success, string Error)> JoinAsync(string room) => ConnectAsync(GameMode.Client, room);
    
    public Task LeaveAsync() => IsBusy ? Task.CompletedTask : StopAsync();

    private async Task<(bool Success, string Error)> ConnectAsync(GameMode mode, string room)
    {
        if (IsBusy || _runner != null)
            return (false, "A session is already active or connecting.");

        if (string.IsNullOrWhiteSpace(room))
            return (false, "Enter a room name.");

        IsBusy = true;
        Status = "Connecting...";
        bool connected = false;

        try
        {
            _runner = UnityEngine.Object.Instantiate(_settings.RunnerPrefab);
            UnityEngine.Object.DontDestroyOnLoad(_runner.gameObject);

            _runner.ProvideInput = true;
            _runner.GetComponent<BasicSpawner>().MaxPlayers = _settings.MaxPlayers;
            _runner.GetComponent<NetworkEvents>().OnShutdown.AddListener(OnShutdown);

            var args = new StartGameArgs
            {
                GameMode = mode,
                SessionName = room.Trim(),
                PlayerCount = _settings.MaxPlayers,
                EnableClientSessionCreation = false,
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
            };

            if (mode == GameMode.Host)
            {
                args.Scene = SceneRef.FromIndex(_settings.InitialSceneIndex);
            }

            var result = await _runner.StartGame(args);
            connected = result.Ok;

            if (!connected)
                return (false, result.ShutdownReason.ToString());

            Status = mode == GameMode.Host ? "Hosting" : "Connected";
            return (true, null);
        }
        finally
        {
            if (connected)
            {
                IsBusy = false;
            }
            else
            {
                await StopAsync();
            }
        }
    }

    private async Task StopAsync()
    {
        IsBusy = true;

        try
        {
            if (_runner != null)
            {
                await _runner.Shutdown(destroyGameObject: false);
            }
        }
        finally
        {
            Cleanup();
            Status = "Disconnected";
            IsBusy = false;
        }
    }

    private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        if (_runner != runner)
            return;

        Status = $"Disconnected: {reason}";
        Cleanup();
    }

    private void Cleanup()
    {
        if (_runner != null)
        {
            _runner.GetComponent<NetworkEvents>().OnShutdown.RemoveListener(OnShutdown);
            UnityEngine.Object.Destroy(_runner.gameObject);
        }

        _runner = null;
    }
}