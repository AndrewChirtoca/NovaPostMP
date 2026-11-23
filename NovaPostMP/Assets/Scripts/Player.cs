using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private NetworkCharacterController _networkCharacterController;

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.Direction.Normalize();
            _networkCharacterController.Move(_gameConfig.PlayerMoveSpeed * data.Direction * Runner.DeltaTime);
        }
    }
}
