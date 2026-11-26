using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField, Min(0.1f)] private float _distance = 4f;
    [SerializeField] private float _height = 1.2f;
    [SerializeField] private float _mouseSensitivity = 0.15f;
    [SerializeField] private float _stickSpeed = 150f;

    private InputAction _look;
    private InputAction _orbit;
    private Transform _target;
    private Pose _menuPose;
    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        _look = _inputActions.FindAction("Player/Look", throwIfNotFound: true);
        _orbit = _inputActions.FindAction("Player/OrbitCamera", throwIfNotFound: true);
        _menuPose = new Pose(transform.position, transform.rotation);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        _yaw = target != null ? target.eulerAngles.y : 0f;
        _pitch = 25f;
        SetCursorCaptured(false);

        if (target == null)
        {
            transform.SetPositionAndRotation(_menuPose.position, _menuPose.rotation);
        }
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            SetTarget(null);
            return;
        }

        if (!Application.isFocused)
            return;

        bool dragging = _orbit.IsPressed();
        bool justCaptured = dragging && Cursor.lockState != CursorLockMode.Locked;
        SetCursorCaptured(dragging);

        Vector2 look = _look.ReadValue<Vector2>();
        bool isPointer = _look.activeControl?.device is Pointer;

        // Ignore pointer movement while using menus or entering cursor capture.
        if (isPointer && (!dragging || justCaptured))
        {
            look = Vector2.zero;
        }

        float sensitivity = isPointer ? _mouseSensitivity : _stickSpeed * Time.unscaledDeltaTime;

        _yaw = Mathf.Repeat(_yaw + look.x * sensitivity, 360f);
        _pitch = Mathf.Clamp(_pitch - look.y * sensitivity, 10f, 75f);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivot = _target.position + Vector3.up * _height;

        transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * _distance, rotation);
    }

    private void OnDisable()
    {
        SetCursorCaptured(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SetCursorCaptured(false);
        }
    }

    private static void SetCursorCaptured(bool captured)
    {
        Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !captured;
    }
}