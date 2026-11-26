using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Buffers local actions between Unity frames and Fusion input callbacks with camera-relative movement.
/// Submits neutral input until the game session starts.
/// </summary>
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
    private InputAction _changeColorInput;
    private Vector2 _moveDirection;
    private bool _spawnItemAction;
    private bool _grabItemAction;
    private bool _throwItemAction;
    private bool _removeItemAction;
    private bool _changeColorAction;
    private Transform _camera;

    private bool CanProvideInput => _session != null && _session.IsPlaying;

    private void Awake()
    {
        _camera = Camera.main.transform;
        _moveInput = _inputActions.FindAction("Player/Move", throwIfNotFound: true);
        _spawnItemInput = _inputActions.FindAction("Player/SpawnItem", throwIfNotFound: true);
        _grabItemInput = _inputActions.FindAction("Player/GrabItem", throwIfNotFound: true);
        _throwItemInput = _inputActions.FindAction("Player/ThrowItem", throwIfNotFound: true);
        _removeItemInput = _inputActions.FindAction("Player/RemoveItem", throwIfNotFound: true);
        _changeColorInput = _inputActions.FindAction("Player/ChangeColor", throwIfNotFound: true);
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
        _changeColorAction = _changeColorAction || _changeColorInput.WasPressedThisFrame();
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

        var yaw = Quaternion.Euler(0f, _camera.eulerAngles.y, 0f);
        var data = new NetworkInputData
        {
            Direction = yaw * new Vector3(_moveDirection.x, 0f, _moveDirection.y)
        };
        data.Buttons.Set(PlayerButton.SpawnItem, _spawnItemAction);
        data.Buttons.Set(PlayerButton.GrabItem, _grabItemAction);
        data.Buttons.Set(PlayerButton.ThrowItem, _throwItemAction);
        data.Buttons.Set(PlayerButton.RemoveItem, _removeItemAction);
        data.Buttons.Set(PlayerButton.ChangeColor, _changeColorAction);
        input.Set(data);

        _spawnItemAction = _grabItemAction = _throwItemAction = _removeItemAction = _changeColorAction = false;
    }

    private void ClearInput()
    {
        _spawnItemAction = _grabItemAction = _throwItemAction = _removeItemAction = _changeColorAction = false;
        _moveDirection = Vector2.zero;
    }
}
