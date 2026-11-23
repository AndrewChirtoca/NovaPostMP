using UnityEngine;
using Fusion;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
public class GameConfig : ScriptableObject
{
    [field : SerializeField][field : Range(0f, 10f)] public float PlayerMoveSpeed { get; private set; } = 5f;
    [field : SerializeField][field : Range(0f, 5f)] public float GrabRange { get; private set; } = 2f;
    [field : SerializeField] public Vector3 HoldOffset { get; private set; } = new (0f, 1f, 1f);
    [field : SerializeField][field : Range(0f, 20f)] public float ThrowForce { get; private set; } = 10f;
    [field : SerializeField] public NetworkPrefabRef ItemPrefab { get; private set; }
}