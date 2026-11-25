using Fusion;
using UnityEngine;

//NOTE: DefaultExecutionOrder is necessary to ensure that the Render() runs after the player's interpolation
//has been applied, so that the item's visuals are rendered in the correct position.
[DefaultExecutionOrder(2000)]
public class Item : NetworkBehaviour
{
    [Networked] public Player Owner { get; set; }

    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider _collider;
    [SerializeField] private Transform _visual;

    public bool TryGrab(Player player)
    {
        if (!HasStateAuthority || Owner != null)
            return false;

        Owner = player;
        ApplyColliderState();
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        return true;
    }

    public bool TryRelease(Player player)
    {
        if (!HasStateAuthority || Owner != player)
            return false;

        Owner = null;
        _rigidbody.isKinematic = false;
        ApplyColliderState();
        return true;
    }
    
    public bool TryThrow(Player player, Vector3 throwDirection, float throwForce)
    {
        if (!HasStateAuthority || Owner != player)
            return false;

        Owner = null;
        _rigidbody.isKinematic = false;
        _rigidbody.AddForce(throwDirection.normalized * throwForce, ForceMode.Impulse);
        ApplyColliderState();
        return true;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        ApplyColliderState();

        if (!HasStateAuthority)
            return;
        
        if (Owner == null)
        {
            // Recover if the holding player disconnected
            if (_rigidbody.isKinematic)
            {
                _rigidbody.isKinematic = false;
            }
        }
        else
        {
            _rigidbody.MovePosition(Owner.transform.position + Owner.transform.rotation * _gameConfig.HoldOffset);
        }
    }

    public override void Render()
    {
        base.Render();

        if (Owner == null || _visual == null)
            return;

        Transform holderTransform = Owner.transform;
        _visual.position = holderTransform.position + holderTransform.rotation * _gameConfig.HoldOffset;
    }
    
    private void ApplyColliderState()
    {
        bool shouldBeEnabled = Owner == null;

        if (_collider.enabled != shouldBeEnabled)
        {
            _collider.enabled = shouldBeEnabled;
        }
    }
}
