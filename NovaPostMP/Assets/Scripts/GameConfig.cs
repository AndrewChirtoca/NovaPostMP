using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
public class GameConfig : ScriptableObject
{
    [field : SerializeField][field : Range(0f, 10f)] public float PlayerMoveSpeed { get; private set; } = 5f;
}