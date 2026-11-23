using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private NetworkCharacterController _networkCharacterController;
    
    [Networked] private Item HeldItem { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.Direction.Normalize();
            _networkCharacterController.Move(_gameConfig.PlayerMoveSpeed * data.Direction * Runner.DeltaTime);
        }

        if (HasStateAuthority)
        {
            if (data.Buttons.IsSet(NetworkInputData.GrabItemActionBit))
            {
                if (HeldItem == null)
                {
                    Item closestInteractable = TryFindClosestFreeItem();
                    closestInteractable.Grab(this);
                    HeldItem = closestInteractable;
                }
                else
                {
                    HeldItem.Release(this);
                    HeldItem = null;
                }
            }

            if (data.Buttons.IsSet(NetworkInputData.ThrowItemActionBit))
            {
                if (HeldItem != null)
                {
                    HeldItem.Throw(this, transform.forward, _gameConfig.ThrowForce);
                    HeldItem = null;
                }
            }
        }
    }

    private Item TryFindClosestFreeItem()
    {
        //TODO: move this declaration to class-level to avoid allocations
        var hits = new Collider[10];
        Physics.OverlapSphereNonAlloc(transform.position, _gameConfig.GrabRange, hits);
        float closestDistance = _gameConfig.GrabRange * _gameConfig.GrabRange;
        Item closestFreeItem = null;

        foreach (Collider hit in hits)
        {
            if(hit == null)
                continue;

            var item = hit.GetComponent<Item>();

            if (item == null || item.Owner != null)
                continue;

            Vector3 direction = hit.transform.position - transform.position;
            float distance = direction.sqrMagnitude;

            if (distance >= closestDistance)
                continue;

            // Only grab items roughly in front of the player.
            Vector3 horizontal = direction;
            horizontal.y = 0f;

            if (horizontal.sqrMagnitude > 0.001f)
            {
                float facing = Vector3.Dot(horizontal.normalized, transform.forward.normalized);

                if (facing < 0.25f)
                    continue;
            }

            closestFreeItem = item;
            closestDistance = distance;
        }
        
        return closestFreeItem;
    }
}
