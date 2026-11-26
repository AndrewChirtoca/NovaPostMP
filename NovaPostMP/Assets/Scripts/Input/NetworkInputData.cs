using UnityEngine;
using Fusion;

public enum PlayerButton
{
    SpawnItem,
    GrabItem,
    ThrowItem,
    RemoveItem
}

public struct NetworkInputData : INetworkInput
{
    public NetworkButtons Buttons;
    public Vector3 Direction;
}