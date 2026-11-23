using UnityEngine;
using Fusion;

//TODO assign the network character controller through a refernce instead of GetComponent
// make MoveSpeed a configurable parameter through a scriptable object
public class Player : NetworkBehaviour
{
    private const float MoveSpeed = 5f;
    private NetworkCharacterController _cc;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.Direction.Normalize();
            _cc.Move(MoveSpeed * data.Direction * Runner.DeltaTime);
        }
    }
}
