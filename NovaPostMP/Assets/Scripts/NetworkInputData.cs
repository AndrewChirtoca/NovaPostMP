using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public const byte SpawnItemActionBit = 1 << 0;
    public const byte GrabItemActionBit = 1 << 1;
    public const byte ThrowItemActionBit = 1 << 2;
    public const byte RemoveItemActionBit = 1 << 3;

    public NetworkButtons Buttons;
    public Vector3 Direction;
}