using UnityEngine;
using UnityEngine.InputSystem;
public class CameraController : MonoBehaviour{
    public float moveSpeed = 10f;
    public float fastSpeed = 25f;
    public float sensitivity = 2f;

    CameraControls controls;
    Vector2 moveInput;
    Vector2 lookInput;
    Vector2 upDownInput;
    bool sprinting;

    float yaw;
    float pitch;

    void Awake()
    {
        controls = new CameraControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        controls.Player.UpDown.performed += ctx => upDownInput = ctx.ReadValue<Vector2>();
        controls.Player.UpDown.canceled += ctx => upDownInput = Vector2.zero;

        controls.Player.Sprint.performed += ctx => sprinting = true;
        controls.Player.Sprint.canceled += ctx => sprinting = false;
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Update()
    {
        // Look
        yaw += lookInput.x * sensitivity ;
        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);

        // Movement
        float speed = sprinting ? fastSpeed : moveSpeed;


        Vector3 move =
            transform.forward * moveInput.y +
            transform.right * moveInput.x +
            transform.up * upDownInput.y;

        transform.position += move * speed * Time.deltaTime;
    }
}