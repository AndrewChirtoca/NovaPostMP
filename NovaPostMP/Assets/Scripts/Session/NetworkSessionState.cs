using Fusion;
using UnityEngine.SceneManagement;
using System.Text;

public struct SessionMember : INetworkStruct
{
    public NetworkString<_32> Name;
    public NetworkBool Loaded;
    public NetworkBool BlocksStart;
    public int RttMs;
}

public class NetworkSessionState : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, SessionMember> Members => default;

    public string HostName { private get; set; }
    [Networked] public int GameplaySceneIndex { get; set; }
    [Networked] public bool GameRequested { get; private set; }
    [Networked] public TickTimer StartAt { get; private set; }
    public bool GameStarted => StartAt.IsRunning && StartAt.Expired(Runner);
    private PlayerSpawner _spawner;
    private bool _reportedLoaded;
    private TickTimer _nextRttUpdate;

    public void BeginLoading()
    {
        if (!HasStateAuthority || GameRequested)
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
        if (!HasStateAuthority)
            return;

        UpdateRtt();

        if (!GameRequested)
            return;

        if (!StartAt.IsRunning)
        {
            bool allLoaded = true;
            foreach (var entry in Members)
            {
                if (entry.Value.BlocksStart && !entry.Value.Loaded)
                {
                    allLoaded = false;
                }
            }

            if (allLoaded)
            {
                StartAt = TickTimer.CreateFromSeconds(Runner, 1f);
            }
        }

        foreach (var entry in Members)
        {
            if (entry.Value.Loaded)
            {
                _spawner.PreparePlayer(entry.Key, StartAt);
            }
        }
    }

    public bool CanPlay(PlayerRef player)
    {
        return GameStarted && Members.TryGet(player, out var member) && member.Loaded;
    }

    private void UpdateRtt()
    {
        if (!_nextRttUpdate.ExpiredOrNotRunning(Runner))
            return;

        _nextRttUpdate = TickTimer.CreateFromSeconds(Runner, 1f);

        foreach (var entry in Members)
        {
            int rttMs = entry.Key == Runner.LocalPlayer
                ? 0
                : (int)System.Math.Round(Runner.GetPlayerRtt(entry.Key) * 1000.0);

            var member = entry.Value;
            if (member.RttMs == rttMs)
                continue;

            member.RttMs = rttMs;
            Members.Set(entry.Key, member);
        }
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

        Members.Add(player, new SessionMember { Name = playerName, RttMs = -1 });
    }
}