using Fusion;

public struct LobbyMember : INetworkStruct
{
    public NetworkString<_32> Name;
}

public class NetworkLobby : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, LobbyMember> Members => default;

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;
        
        UnityEngine.Debug.Log($"Lobby spawned in scene: {gameObject.scene.name}", gameObject);

        foreach (var player in Runner.ActivePlayers)
        {
            AddMember(player);
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        AddMember(player);
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        Members.Remove(player);
    }

    private void AddMember(PlayerRef player)
    {
        if (!Members.ContainsKey(player))
        {
            Members.Add(player, new LobbyMember { Name = $"Player {player.RawEncoded}" });
        }
    }
}