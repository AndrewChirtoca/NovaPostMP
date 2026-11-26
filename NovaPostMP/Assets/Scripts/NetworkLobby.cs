using Fusion;
using UnityEngine.SceneManagement;
using System.Text;

public struct LobbyMember : INetworkStruct
{
    public NetworkString<_32> Name;
    public NetworkBool Loaded;
    public NetworkBool BlocksStart;
}

public class NetworkLobby : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, LobbyMember> Members => default;

    public string HostName { private get; set; }
    [Networked] public int GameplaySceneIndex { get; set; }
    [Networked] public bool GameRequested { get; private set; }
    [Networked] public bool GameStarted { get; private set; }
    private PlayerSpawner _spawner;
    private bool _reportedLoaded;

    public void BeginLoading()
    {
        if (!HasStateAuthority)
            return;

        GameRequested = true;
        foreach (var entry in Members)
        {
            var member = entry.Value;
            member.BlocksStart = true;
            Members.Set(entry.Key, member);
        }
    }

    public override void Render()
    {
        if (_reportedLoaded || !GameRequested)
            return;

        var scene = SceneManager.GetSceneByBuildIndex(GameplaySceneIndex);
        if (scene.IsValid() && scene.isLoaded && !Runner.IsSceneManagerBusy)
        {
            RPC_Loaded();
            _reportedLoaded = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !GameRequested)
            return;

        if (!GameStarted)
        {
            bool allLoaded = true;
            foreach (var entry in Members)
            {
                if (entry.Value.BlocksStart && !entry.Value.Loaded)
                    allLoaded = false;
            }

            if (allLoaded)
                GameStarted = true;
        }

        if (GameStarted)
        {
            foreach (var entry in Members)
            {
                if (entry.Value.Loaded)
                    _spawner.SpawnPlayer(entry.Key);
            }
        }
    }

    public bool CanPlay(PlayerRef player)
    {
        return GameStarted && Members.TryGet(player, out var member) && member.Loaded;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_Loaded(RpcInfo info = default)
    {
        if (!GameRequested || !Members.TryGet(info.Source, out var member))
            return;

        member.Loaded = true;
        Members.Set(info.Source, member);
    }

    public override void Spawned()
    {
        _spawner = Runner.GetComponent<PlayerSpawner>();
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