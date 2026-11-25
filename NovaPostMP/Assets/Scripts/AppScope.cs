using UnityEngine;
using VContainer;
using VContainer.Unity;
using Fusion;

public class AppScope : LifetimeScope
{
    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private SessionMenu menu;
    [SerializeField] private int _initialSceneIndex;

    [SerializeField, Range(2, 4)] private int _maxPlayers = 4;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(new FusionSessionSettings
        {
            RunnerPrefab = _runnerPrefab,
            InitialSceneIndex = _initialSceneIndex,
            MaxPlayers = _maxPlayers
        });

        builder.Register<FusionSessionService>(Lifetime.Singleton).As<ISessionService>();

        builder.RegisterComponent(menu);
    }
}