using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkEvents))]
public class PlayerInputProvider : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    private ISessionService _session;
    private InputAction _moveInput;
    private InputAction _spawnItemInput;
    private InputAction _grabItemInput;
    private InputAction _throwItemInput;
    private InputAction _removeItemInput;
    private Vector2 _moveDirection;
    private bool _spawnItemAction;
    private bool _grabItemAction;
    private bool _throwItemAction;
    private bool _removeItemAction;

    private bool CanProvideInput => _session != null && _session.IsPlaying;

    private void Awake()
    {
        _moveInput = _inputActions.FindAction("Player/Move", throwIfNotFound: true);
        _spawnItemInput = _inputActions.FindAction("Player/SpawnItem", throwIfNotFound: true);
        _grabItemInput = _inputActions.FindAction("Player/GrabItem", throwIfNotFound: true);
        _throwItemInput = _inputActions.FindAction("Player/ThrowItem", throwIfNotFound: true);
        _removeItemInput = _inputActions.FindAction("Player/RemoveItem", throwIfNotFound: true);
    }

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

        _spawnItemAction = _spawnItemAction || _spawnItemInput.WasPressedThisFrame();
        _grabItemAction = _grabItemAction || _grabItemInput.WasPressedThisFrame();
        _throwItemAction = _throwItemAction || _throwItemInput.WasPressedThisFrame();
        _removeItemAction = _removeItemAction || _removeItemInput.WasPressedThisFrame();
        _moveDirection = _moveInput.ReadValue<Vector2>();
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
        data.Buttons.Set(PlayerButton.SpawnItem, _spawnItemAction);
        data.Buttons.Set(PlayerButton.GrabItem, _grabItemAction);
        data.Buttons.Set(PlayerButton.ThrowItem, _throwItemAction);
        data.Buttons.Set(PlayerButton.RemoveItem, _removeItemAction);
        input.Set(data);

        _spawnItemAction = _grabItemAction = _throwItemAction = _removeItemAction = false;
    }

    private void ClearInput()
    {
        _spawnItemAction = _grabItemAction = _throwItemAction = _removeItemAction = false;
        _moveDirection = Vector2.zero;
    }
}
