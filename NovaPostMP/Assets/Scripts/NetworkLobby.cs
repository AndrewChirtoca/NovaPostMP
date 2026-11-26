using Fusion;
using System.Text;

public struct LobbyMember : INetworkStruct
{
    public NetworkString<_32> Name;
}

public class NetworkLobby : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, LobbyMember> Members => default;

    public string HostName { private get; set; }

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

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
        if (Members.ContainsKey(player))
            return;

        var token = Runner.GetPlayerConnectionToken(player);
        string playerName = player == Runner.LocalPlayer ? HostName
            : (token == null ? null : Encoding.UTF8.GetString(token));
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = $"Player {player.RawEncoded}";
        }

        if (playerName.Length > 32)
        {
            playerName = playerName.Substring(0, 32);
        }

        Members.Add(player, new LobbyMember { Name = playerName });
    }
}