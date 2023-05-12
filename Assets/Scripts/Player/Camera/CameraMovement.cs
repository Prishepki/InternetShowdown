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
    [SerializeField] private float _tiltSmoothing = 0.3f;
    [SerializeField] private float _tiltAmount = 5.0f;
    private float _dampVelocity = 0.0f; // я в душе не ебу нахуя оно нужно но оно нужно

    [Header("Other")]
    [SerializeField] public Transform Orientation;

    private float _rotx;
    private float _roty;
    private float _rotz;

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
            
            _roty += mouseX;
            _rotx -= mouseY;
            _rotz = Mathf.SmoothDamp(_rotz, Input.GetAxisRaw("Horizontal") * -_tiltAmount, ref _dampVelocity, _tiltSmoothing);

            _rotx = Mathf.Clamp(_rotx, _topClamp, _bottomClamp);

            transform.rotation = Quaternion.Euler(_rotx, _roty, _rotz);
            Orientation.localRotation = Quaternion.Euler(transform.localRotation.x, _roty, transform.localRotation.z);
        }
    }
}
