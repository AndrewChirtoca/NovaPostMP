using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

/// <summary>
/// Presents connection controls, loading feedback and the player roster through IMGUI.
/// Delegates session operations to ISessionService and displays and logs errors.
/// </summary>
public class SessionMenu : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    private ISessionService _session;
    private InputAction _togglePlayers;
    private string _roomName = "TestRoom";
    private string _playerName = "Player";
    private string _error = "";
    private bool _showPlayers;

    [Inject]
    public void Construct(ISessionService session)
    {
        _session = session;
    }

    private void Awake()
    {
        _togglePlayers = _inputActions.FindAction("Player/TogglePlayers", throwIfNotFound: true);
    }

    private void Update()
    {
        if (_session == null || !_session.IsPlaying)
            return;

        if (_togglePlayers.WasPressedThisFrame())
        {
            _showPlayers = !_showPlayers;
        }
    }

    private void OnGUI()
    {
        if (_session == null)
            return;

        if (_session.IsLoading)
        {
            var color = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = color;
        }

        GUILayout.Label(_session.ConnectionStatus);
        GUILayout.Label(_error);

        GUI.enabled = !_session.IsBusy;

        if (!_session.IsConnected)
        {
            GUILayout.Label("Player name");
            _playerName = GUILayout.TextField(_playerName, 32);
            GUILayout.Label("Room name");
            _roomName = GUILayout.TextField(_roomName);

            if (GUILayout.Button("Host"))
            {
                Run(async () =>
                {
                    _error = (await _session.HostAsync(_roomName, _playerName)).Error ?? "";
                });
            }

            if (GUILayout.Button("Join"))
            {
                Run(async () =>
                {
                    _error = (await _session.JoinAsync(_roomName, _playerName)).Error ?? "";
                });
            }
        }
        else
        {
            if (!_session.IsPlaying || _showPlayers)
            {
                foreach (var player in _session.Players)
                {
                    string rtt = player.RttMs < 0 ? "--" : $"{player.RttMs} ms";
                    string loading = _session.IsLoading ? (player.Loaded ? " - loaded" : " - loading") : "";

                    GUILayout.Label($"{player.Name} - {rtt}{loading}");
                }
            }

            if (_session.IsPlaying)
            {
                GUILayout.Label($"{_togglePlayers.GetBindingDisplayString()}: show/hide players");
            }

            if (_session.CanStart && GUILayout.Button("Start"))
            {                
                Run(() => _session.StartGameAsync());
            }
            if (_session.IsLoading)
            {                
                GUILayout.Label("Loading...");
            }

            if (GUILayout.Button("Leave"))
            {
                Run(() => _session.LeaveAsync());
            }
        }

        GUI.enabled = true;
    }

    private async void Run(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            _error = exception.Message;
            Debug.LogError(exception.Message);
        }
    }
}