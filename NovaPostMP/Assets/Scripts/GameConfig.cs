using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
public class GameConfig : ScriptableObject
{
    [field : Header("Player Movement")]
    [field : SerializeField][field : Range(0f, 10f)] public float PlayerMoveSpeed { get; private set; } = 5f;

    [field : Header("Holding Settings")]
    [field : SerializeField][field : Range(0f, 5f)] public float GrabRange { get; private set; } = 2f;
    [field : SerializeField] public Vector3 HoldOffset { get; private set; } = new (0f, 0.7f, 0.85f);
}