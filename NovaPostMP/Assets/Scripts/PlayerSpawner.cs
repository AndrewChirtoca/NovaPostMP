using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();

    public int MaxPlayers { get; set; } = 4;

    public void SpawnPlayer(PlayerRef player)
    {
        if (!Runner.IsServer || _spawnedCharacters.ContainsKey(player))
            return;

        var position = new Vector3((player.RawEncoded % MaxPlayers) * 3, 1, 0);
        var avatar = Runner.Spawn(_playerPrefab, position, Quaternion.identity, player);
        _spawnedCharacters.Add(player, avatar);
        Runner.SetPlayerObject(player, avatar);
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer && _spawnedCharacters.Remove(player, out var avatar) && avatar != null)
        {
            Runner.Despawn(avatar);
        }
    }
}