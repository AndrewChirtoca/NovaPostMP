using System;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;

public class SessionMenu : MonoBehaviour
{
    private ISessionService _session;
    private string _roomName = "TestRoom";
    private string _playerName = "Player";
    private string _error = "";

    [Inject]
    public void Construct(ISessionService session)
    {
        _session = session;
    }

    private void OnGUI()
    {
        if (_session == null)
            return;

        GUILayout.Label(_session.Status);
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
            foreach (var player in _session.Players)
            {
                GUILayout.Label(player);
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