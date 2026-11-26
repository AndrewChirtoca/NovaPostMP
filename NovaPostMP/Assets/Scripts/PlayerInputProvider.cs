using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkEvents))]
public class PlayerInputProvider : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    private ISessionService _session;
    private Vector2 _moveDirection;
    private bool _spawnItemAction;
    private bool _grabItemAction;
    private bool _throwItemAction;
    private bool _removeItemAction;

    private bool CanProvideInput => _session != null && _session.IsPlaying;

    public void Initialize(ISessionService session)
    {
        _session = session;
    }

    private void OnEnable()
    {
        GetComponent<NetworkEvents>().OnInput.AddListener(OnInput);
    }

    private void OnDisable()
    {
        GetComponent<NetworkEvents>().OnInput.RemoveListener(OnInput);
        ClearInput();
    }

    private void Update()
    {
        if (!CanProvideInput)
        {
            ClearInput();
            return;
        }

        _spawnItemAction = _spawnItemAction || _inputActions.FindAction("SpawnItem").WasPressedThisFrame();
        _grabItemAction = _grabItemAction || _inputActions.FindAction("GrabItem").WasPressedThisFrame();
        _throwItemAction = _throwItemAction || _inputActions.FindAction("ThrowItem").WasPressedThisFrame();
        _removeItemAction = _removeItemAction || _inputActions.FindAction("RemoveItem").WasPressedThisFrame();
        _moveDirection = _inputActions.FindAction("Move").ReadValue<Vector2>();
    }

    private void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!CanProvideInput)
        {
            ClearInput();
            input.Set(default(NetworkInputData));
            return;
        }

        var data = new NetworkInputData
        {
            Direction = new Vector3(_moveDirection.x, 0, _moveDirection.y)
        };
        data.Buttons.Set(NetworkInputData.SpawnItemActionBit, _spawnItemAction);
        data.Buttons.Set(NetworkInputData.GrabItemActionBit, _grabItemAction);
        data.Buttons.Set(NetworkInputData.ThrowItemActionBit, _throwItemAction);
        data.Buttons.Set(NetworkInputData.RemoveItemActionBit, _removeItemAction);
        input.Set(data);

        _spawnItemAction = _grabItemAction = _throwItemAction = _removeItemAction = false;
    }

    private void ClearInput()
    {
        _spawnItemAction = _grabItemAction = _throwItemAction = _removeItemAction = false;
        _moveDirection = Vector2.zero;
    }
}
