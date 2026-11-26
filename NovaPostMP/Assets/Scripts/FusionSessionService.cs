using System.Threading.Tasks;
using Fusion;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public class FusionSessionService : ISessionService
{
    private readonly FusionSessionSettings _settings;
    private NetworkRunner _runner;
    private NetworkLobby _lobby;
    private string _playerName;

    private NetworkLobby Lobby => !IsConnected ? null : _lobby != null ? _lobby
        : _lobby = _runner.GetAllBehaviours<NetworkLobby>().FirstOrDefault();

    public IEnumerable<string> Players => Lobby == null ? Enumerable.Empty<string>()
        : Lobby.Members.Select(entry => entry.Value.Name.ToString());

    public bool IsBusy { get; private set; }
    public bool IsConnected => _runner != null && _runner.IsRunning;
    public string Status { get; private set; } = "Disconnected";

    public FusionSessionService(FusionSessionSettings settings)
    {
        _settings = settings;
    }
    
    public Task<(bool Success, string Error)> HostAsync(string room, string playerName)
        => ConnectAsync(GameMode.Host, room, playerName);

    public Task<(bool Success, string Error)> JoinAsync(string room, string playerName)
        => ConnectAsync(GameMode.Client, room, playerName);
    
    public Task LeaveAsync() => IsBusy ? Task.CompletedTask : StopAsync("Disconnected");

    private async Task<(bool Success, string Error)> ConnectAsync(GameMode mode, string room, string playerName)
    {
        if (IsBusy || _runner != null)
            return (false, "A session is already active or connecting.");

        if (string.IsNullOrWhiteSpace(room) || string.IsNullOrWhiteSpace(playerName))
            return (false, "Enter a room name and a player name.");

        _playerName = playerName.Trim();
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
            _runner.GetComponent<NetworkEvents>().OnSceneLoadDone.AddListener(OnSceneLoadDone);

            var args = new StartGameArgs
            {
                GameMode = mode,
                SessionName = room.Trim(),
                ConnectionToken = Encoding.UTF8.GetBytes(_playerName),
                PlayerCount = _settings.MaxPlayers,
                EnableClientSessionCreation = false,
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
            };

            if (mode == GameMode.Host)
            {
                args.Scene = SceneRef.FromIndex(_settings.InitialSceneIndex);
            }

            var result = await _runner.StartGame(args);
            if (!result.Ok)
                return (false, result.ShutdownReason.ToString());

            connected = true;
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
                await StopAsync("Disconnected");
            }
        }
    }

    private async Task StopAsync(string status, bool shutdown = true)
    {
        IsBusy = true;

        try
        {
            if (shutdown && _runner != null)
            {
                await _runner.Shutdown(destroyGameObject: false);
            }
        }
        finally
        {
            Cleanup();
            Status = status;
            IsBusy = false;
        }
    }

    private async void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        if (_runner != runner || IsBusy)
            return;

        try
        {
            await StopAsync($"Disconnected: {reason}", shutdown: false);
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError(exception.Message);
        }
    }

    private void Cleanup()
    {
        if (_runner != null)
        {
            _runner.GetComponent<NetworkEvents>().OnShutdown.RemoveListener(OnShutdown);
            _runner.GetComponent<NetworkEvents>().OnSceneLoadDone.RemoveListener(OnSceneLoadDone);
            UnityEngine.Object.Destroy(_runner.gameObject);
        }

        _runner = null;
        _lobby = null;
    }
    
    private void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer || _lobby != null)
            return;

        runner.Spawn(_settings.LobbyPrefab, onBeforeSpawned: InitializeLobby);
    }
    
    private void InitializeLobby(NetworkRunner runner, NetworkObject lobbyObject)
    {
        _lobby = lobbyObject.GetComponent<NetworkLobby>();
        _lobby.HostName = _playerName;
    }
}