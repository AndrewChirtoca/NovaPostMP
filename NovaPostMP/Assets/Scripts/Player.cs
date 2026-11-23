using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private NetworkCharacterController _networkCharacterController;
    
    [Networked] private Delivery HeldDelivery { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.Direction.Normalize();
            _networkCharacterController.Move(_gameConfig.PlayerMoveSpeed * data.Direction * Runner.DeltaTime);
        }
        
        // New grabbing code
        if (HasStateAuthority)
        {
            if (data.Buttons.IsSet(NetworkInputData.InteractActionBit))
            {
                if (HeldDelivery == null)
                {
                    TryGrabDelivery();
                }
                else
                {
                    HeldDelivery.Release(this);
                    HeldDelivery = null;
                }
            }

            // Update the held delivery every network tick.
            if (HeldDelivery != null)
            {
                Vector3 holdPosition = transform.position + transform.rotation * _gameConfig.HoldOffset;
                HeldDelivery.Follow(this, holdPosition);
            }
        }
    }
    
    private void TryGrabDelivery()
    {
        if (!HasStateAuthority || HeldDelivery != null)
            return;

        var hits = new Collider[10];
        Physics.OverlapSphereNonAlloc(transform.position, _gameConfig.GrabRange, hits);
        float closestDistance = _gameConfig.GrabRange * _gameConfig.GrabRange;
        Delivery closestDelivery = null;

        foreach (Collider hit in hits)
        {
            if(hit == null)
                continue;

            Delivery delivery = hit.GetComponent<Delivery>();

            if (delivery == null || delivery.HeldBy != null)
                continue;

            Vector3 direction = delivery.transform.position - transform.position;
            float distance = direction.sqrMagnitude;

            if (distance >= closestDistance)
                continue;

            // Only grab deliveries roughly in front of the player.
            Vector3 horizontal = direction;
            horizontal.y = 0f;

            if (horizontal.sqrMagnitude > 0.001f)
            {
                float facing = Vector3.Dot(horizontal.normalized, transform.forward.normalized);

                if (facing < 0.25f)
                    continue;
            }

            closestDelivery = delivery;
            closestDistance = distance;
        }

        if (closestDelivery != null && closestDelivery.Grab(this))
        {
            HeldDelivery = closestDelivery;
        }
    }
}
