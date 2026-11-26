using Fusion;
using UnityEngine;

public class PlayerCameraBinding : NetworkBehaviour
{
    private OrbitCamera _orbitCamera;

    public override void Spawned()
    {
        if (!HasInputAuthority)
            return;

        Camera camera = Camera.main;

        if (camera != null && camera.TryGetComponent(out _orbitCamera))
        {
            _orbitCamera.SetTarget(transform);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_orbitCamera != null)
        {
            _orbitCamera.SetTarget(null);
            _orbitCamera = null;
        }
    }
}
