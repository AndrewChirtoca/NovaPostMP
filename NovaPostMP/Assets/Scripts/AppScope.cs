using UnityEngine;
using VContainer;
using VContainer.Unity;
using Fusion;

public class AppScope : LifetimeScope
{
    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private SessionMenu menu;
    [SerializeField] private int _initialSceneIndex;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(new FusionSessionSettings
        {
            RunnerPrefab = _runnerPrefab,
            InitialSceneIndex = _initialSceneIndex
        });

        builder.Register<FusionSessionService>(Lifetime.Singleton).As<ISessionService>();

        builder.RegisterComponent(menu);
    }
}