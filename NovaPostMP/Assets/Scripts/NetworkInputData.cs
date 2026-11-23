using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public const byte InteractActionBit = 1 << 0;

    public NetworkButtons Buttons;
    public Vector3 Direction;
}
