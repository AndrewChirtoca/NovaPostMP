using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new ();

    public int MaxPlayers { get; set; } = 4;

    public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer || _spawnedCharacters.ContainsKey(player))
            return;

        var position = new Vector3((player.RawEncoded % MaxPlayers) * 3, 1, 0);
        var avatar = runner.Spawn(_playerPrefab, position, Quaternion.identity, player);
        _spawnedCharacters.Add(player, avatar);
        runner.SetPlayerObject(player, avatar);
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        SpawnPlayer(runner, player);
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && _spawnedCharacters.Remove(player, out var avatar) && avatar != null)
        {
            runner.Despawn(avatar);
        }
    }

    #region Input handling
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        data.Direction = new Vector3(_moveDirection.x, 0, _moveDirection.y);
        
        data.Buttons.Set(NetworkInputData.SpawnItemActionBit, _spawnItemAction);
        _spawnItemAction = false;
        
        data.Buttons.Set(NetworkInputData.GrabItemActionBit, _grabItemAction);
        _grabItemAction = false;
        
        data.Buttons.Set(NetworkInputData.ThrowItemActionBit, _throwItemAction);
        _throwItemAction = false;
        
        data.Buttons.Set(NetworkInputData.RemoveItemActionBit, _removeItemAction);
        _removeItemAction = false;

        input.Set(data);
    }

    [SerializeField] private InputActionAsset _inputActions;
    private bool _spawnItemAction;
    private bool _grabItemAction;
    private bool _throwItemAction;
    private bool _removeItemAction;
    private Vector2 _moveDirection;

    private void Update()
    {
        _spawnItemAction = _spawnItemAction || _inputActions.FindAction("SpawnItem").WasPressedThisFrame();
        _grabItemAction = _grabItemAction || _inputActions.FindAction("GrabItem").WasPressedThisFrame();
        _throwItemAction = _throwItemAction || _inputActions.FindAction("ThrowItem").WasPressedThisFrame();
        _removeItemAction = _removeItemAction || _inputActions.FindAction("RemoveItem").WasPressedThisFrame();
        _moveDirection = _inputActions.FindAction("Move").ReadValue<Vector2>();
    }
    #endregion

    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        int count = 0;
        foreach (var player in runner.ActivePlayers)
            count++;

        if (count < MaxPlayers)
            request.Accept();
        else
            request.Refuse();
    }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
