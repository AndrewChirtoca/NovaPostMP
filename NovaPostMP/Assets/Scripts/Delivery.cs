using Fusion;
using UnityEngine;

//NOTE: DefaultExecutionOrder is necessary to ensure that the Render() runs after the player's interpolation
//has been applied, so that the delivery's visuals are rendered in the correct position.
[DefaultExecutionOrder(1000)]
public class Delivery : NetworkBehaviour
{
    [Networked] public Player HeldBy { get; set; }

    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider _collider;
    [SerializeField] private Transform _visual;

    public bool Grab(Player player)
    {
        if (!HasStateAuthority || HeldBy != null)
            return false;

        HeldBy = player;
        ApplyColliderState();
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;

        return true;
    }

    public void Follow(Player player, Vector3 position)
    {
        if (!HasStateAuthority || HeldBy != player)
            return;

        _rigidbody.MovePosition(position);
    }

    public void Release(Player player)
    {
        if (!HasStateAuthority || HeldBy != player)
            return;

        HeldBy = null;
        _rigidbody.isKinematic = false;
        ApplyColliderState();
    }

    public override void FixedUpdateNetwork()
    {
        ApplyColliderState();

        if (!HasStateAuthority)
            return;

        // Recover if the holding player disconnected
        if (HeldBy == null && _rigidbody.isKinematic)
        {
            _rigidbody.isKinematic = false;
        }
    }
    
    public override void Render()
    {
        if (HeldBy == null || _visual == null)
            return;

        Transform holderTransform = HeldBy.transform;
        _visual.position = holderTransform.position + holderTransform.rotation * _gameConfig.HoldOffset;
    }
    
    private void ApplyColliderState()
    {
        bool shouldBeEnabled = HeldBy == null;

        if (_collider.enabled != shouldBeEnabled)
        {
            _collider.enabled = shouldBeEnabled;
        }
    }
}
