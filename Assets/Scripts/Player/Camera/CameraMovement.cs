using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Sensitivity")]
    [Range(0.1f, 4f)] private float _sensitivityX = 2f;
    [Range(0.1f, 4f)] private float _sensitivityY = 2f;

    [Header("Clamping")]
    [SerializeField] private float _topClamp = -85f;
    [SerializeField] private float _bottomClamp = 90f;

    [Header("Tilting")]
    [SerializeField] private float _tiltSmoothing = 0.15f;
    [SerializeField] private float _tiltAmount = 5.0f;

    [Header("Bobbing")]
    [SerializeField] private float _bobbingAmount = 2.5f;
    [SerializeField] private float _bobbingSpeed = 15.0f;
    private float _tiltDampVelocity = 0.0f; // я в душе не ебу нахуя оно нужно но оно нужно

    [Header("Focus")]
    [SerializeField] private float _fov = 75;
    [SerializeField] private float _fovSmoothing = 0.5f;
    private float _fovDampVelocity = 0.0f;

    [Header("Other")]
    public Transform Orientation;
    public Transform CamHolder;
    public NetworkPlayer Player;

    private float _rotX;
    private float _rotY;
    private float _rotZ;
    private Vector3 _inputVector;
    private Vector3 _startPos;
    [HideInInspector] public bool BlockMovement;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!BlockMovement)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * _sensitivityX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * _sensitivityY;

            _rotY += mouseX;
            _rotX -= mouseY;
            _rotX = Mathf.Clamp(_rotX, _topClamp, _bottomClamp);

            Tilt();

            CamHolder.rotation = Quaternion.Euler(CamHolder.eulerAngles.x, _rotY, CamHolder.eulerAngles.z);
            transform.rotation = Quaternion.Euler(_rotX, _rotY, _rotZ);
            
            Focus();
            Orientation.localRotation = Quaternion.Euler(transform.localRotation.x, _rotY, transform.localRotation.z);
        }
    }

    private void Tilt()
    {
        float grounded = Player.CheckIsGrounded() ? 1.0f : 0.3f;
        _rotZ = Mathf.SmoothDamp(_rotZ, (Player.GetAxisInputs().x * -_tiltAmount) + ((Player.GetAxisInputs().y * (Mathf.Cos((Time.time * _bobbingSpeed)) * _bobbingAmount) * grounded)), ref _tiltDampVelocity, _tiltSmoothing);
    }

    private void Focus()
    {
        if (Player.GetAxisInputs().y > 0)
            this.transform.GetComponent<Camera>().fieldOfView = Mathf.SmoothDamp(this.transform.GetComponent<Camera>().fieldOfView, _fov + 16.5f, ref _fovDampVelocity, _fovSmoothing);
        else
            this.transform.GetComponent<Camera>().fieldOfView = Mathf.SmoothDamp(this.transform.GetComponent<Camera>().fieldOfView, _fov, ref _fovDampVelocity, _fovSmoothing);
    }
}
