using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private NetworkCharacterController _networkCharacterController;
    
    [Networked] public TickTimer StartAt { get; set; }
    [Networked] private Item HeldItem { get; set; }
    private int _itemLayerMask;
    
    private void Awake()
    {
        _itemLayerMask = 1 << LayerMask.NameToLayer("Item");
    }

    public override void FixedUpdateNetwork()
    {
        if (!StartAt.IsRunning || !StartAt.Expired(Runner))
            return;

        if (GetInput(out NetworkInputData data))
        {
            data.Direction.Normalize();
            _networkCharacterController.Move(_gameConfig.PlayerMoveSpeed * data.Direction * Runner.DeltaTime);
        }

        if (!HasStateAuthority)
            return;

        if (data.Buttons.IsSet(PlayerButton.SpawnItem))
        {
            Runner.Spawn(_gameConfig.ItemPrefab, transform.position + transform.forward + Vector3.up);
        }

        if (data.Buttons.IsSet(PlayerButton.GrabItem))
        {
            if (HeldItem == null)
            {
                if (TryFindClosestFreeItem(out Item closestFreeItem, _itemLayerMask))
                {
                    if (closestFreeItem.TryGrab(this))
                    {
                        HeldItem = closestFreeItem;
                    }
                }
            }
            else
            {
                if (HeldItem.TryRelease(this))
                {
                    HeldItem = null;
                }
            }
        }

        if (data.Buttons.IsSet(PlayerButton.ThrowItem))
        {
            if (HeldItem != null)
            {
                if (HeldItem.TryThrow(this, transform.forward, _gameConfig.ThrowForce))
                {
                    HeldItem = null;
                }
            }
        }

        if (data.Buttons.IsSet(PlayerButton.RemoveItem))
        {
            if (HeldItem != null)
            {
                Runner.Despawn(HeldItem.Object);
                HeldItem = null;
            }
        }
    }

    private bool TryFindClosestFreeItem(out Item closestFreeItem, int layerMask = -1)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _gameConfig.GrabRange, layerMask);
        float closestDistance = _gameConfig.GrabRange * _gameConfig.GrabRange;
        closestFreeItem = null;

        foreach (Collider hit in hits)
        {
            if(hit == null)
                continue;

            var item = hit.GetComponent<Item>();

            if (item == null || item.Holder != null)
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

        return closestFreeItem != null;
    }
}
