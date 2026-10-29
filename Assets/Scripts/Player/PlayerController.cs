using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CharacterController))]
public sealed partial class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    private CharacterController characterController;
    private InputAction moveAction;
    private PlayerAvatar avatar;
    private PlaygroundCamera viewCamera;
    public Vector3 LastTravelDirection { get; private set; } = Vector3.forward;
    public float SpeedMultiplier { get; set; } = 1f;
    public Vector2 MoveInput => isActiveAndEnabled && moveAction != null
        ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        avatar = GetComponent<PlayerAvatar>();
        viewCamera = FindFirstObjectByType<PlaygroundCamera>();
        // Own the action lifetime so death can disable movement without disabling restart.
        moveAction = InputSystem.actions.FindAction("Player/Move", true).Clone();
        InitializeDash();
    }

    private void OnEnable() { moveAction.Enable(); dashAction.Enable(); dashHeld=true; }
    private void OnDisable() { moveAction.Disable(); dashAction.Disable(); SuspendDashFeedback(); }
    private void OnDestroy() { moveAction.Dispose(); DisposeDash(); }

    private void Update()
    {
        PollMovement(Time.deltaTime);
    }

    private Vector3 ReadMoveDirection()
    {
        // Stick deadzone comes from the Input System's Gamepad layout; retain analog speed.
        Vector2 input = MoveInput;
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        return viewCamera != null && viewCamera.ThirdPerson ? Quaternion.Euler(0,viewCamera.Yaw,0) * direction : direction;
    }
}
