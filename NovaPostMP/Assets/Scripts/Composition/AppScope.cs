using UnityEngine;
using VContainer;
using VContainer.Unity;
using Fusion;

public class AppScope : LifetimeScope
{
    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private NetworkPrefabRef _sessionStatePrefab;
    [SerializeField] private SessionMenu menu;
    [SerializeField] private int _initialSceneIndex;
    [SerializeField] private int _gameplaySceneIndex = 1;

    [SerializeField, Range(2, 4)] private int _maxPlayers = 4;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(new FusionSessionSettings
        {
            RunnerPrefab = _runnerPrefab,
            SessionStatePrefab = _sessionStatePrefab,
            InitialSceneIndex = _initialSceneIndex,
            GameplaySceneIndex = _gameplaySceneIndex,
            MaxPlayers = _maxPlayers
        });

        builder.Register<FusionSessionService>(Lifetime.Singleton).As<ISessionService>();

        builder.RegisterComponent(menu);
    }
}