using System.Threading.Tasks;
using Fusion;
using UnityEngine.SceneManagement;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public class FusionSessionService : ISessionService
{
    private readonly FusionSessionSettings _settings;
    private NetworkRunner _runner;
    private NetworkLobby _lobby;
    private string _playerName;

    private NetworkLobby Lobby => !IsConnected ? null : _lobby != null && _lobby.IsValid ? _lobby
        : _lobby = _runner.GetAllBehaviours<NetworkLobby>().FirstOrDefault(lobby => lobby.IsValid);

    public IEnumerable<(string Name, bool Loaded)> Players => Lobby == null
        ? Enumerable.Empty<(string, bool)>()
        : Lobby.Members.Select(entry => (entry.Value.Name.ToString(), (bool)entry.Value.Loaded));

    public bool IsBusy { get; private set; }
    public bool IsConnected => _runner != null && _runner.IsRunning;
    public bool CanStart => !IsBusy && Lobby != null && _runner.IsServer && !Lobby.GameRequested;
    public bool IsPlaying => !IsBusy && Lobby != null && Lobby.IsValid && Lobby.CanPlay(_runner.LocalPlayer)
        && _runner.TryGetPlayerObject(_runner.LocalPlayer, out var avatar) && avatar != null;
    public bool IsLoading => Lobby != null && Lobby.GameRequested && !IsPlaying;
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
            _runner.GetComponent<PlayerSpawner>().MaxPlayers = _settings.MaxPlayers;
            _runner.GetComponent<PlayerInputProvider>().Initialize(this);
            _runner.GetComponent<NetworkEvents>().OnShutdown.AddListener(OnShutdown);
            _runner.GetComponent<NetworkEvents>().OnSceneLoadDone.AddListener(OnSceneLoadDone);
            _runner.GetComponent<NetworkEvents>().OnConnectRequest.AddListener(OnConnectRequest);

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

    public async Task StartGameAsync()
    {
        if (!CanStart)
            return;

        Lobby.BeginLoading();
        await _runner.LoadScene(SceneRef.FromIndex(_settings.GameplaySceneIndex), LoadSceneMode.Additive);
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
            try
            {
                await UnloadGameplayAsync();
            }
            finally
            {
                Status = status;
                IsBusy = false;
            }
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

    private async Task UnloadGameplayAsync()
    {
        var scene = SceneManager.GetSceneByBuildIndex(_settings.GameplaySceneIndex);
        var bootstrap = SceneManager.GetSceneByBuildIndex(_settings.InitialSceneIndex);
        if (bootstrap.IsValid() && bootstrap.isLoaded)
        {
            SceneManager.SetActiveScene(bootstrap);
        }

        if (scene.IsValid() && scene.isLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(scene);
            while (unload != null && !unload.isDone)
            {
                await Task.Yield();
            }
        }
    }

    private void Cleanup()
    {
        if (_runner != null)
        {
            _runner.GetComponent<NetworkEvents>().OnShutdown.RemoveListener(OnShutdown);
            _runner.GetComponent<NetworkEvents>().OnSceneLoadDone.RemoveListener(OnSceneLoadDone);
            _runner.GetComponent<NetworkEvents>().OnConnectRequest.RemoveListener(OnConnectRequest);
            UnityEngine.Object.Destroy(_runner.gameObject);
        }

        _runner = null;
        _lobby = null;
    }
    
    private void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        if (runner.ActivePlayers.Count() < _settings.MaxPlayers)
            request.Accept();
        else
            request.Refuse();
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
        _lobby.GameplaySceneIndex = _settings.GameplaySceneIndex;
    }
}