using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    //Stole this code from FIFBOX :)
    [Range(0.1f, 4f)] public float SensitivityX = 2f;
    [Range(0.1f, 4f)] public float SensitivityY = 2f;

    public float TopClamp = -85f;
    public float BottomClamp = 90f;

    public Transform Orientation;

    private float _rotx;
    private float _roty;

    [HideInInspector] public bool BlockMovement;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!BlockMovement)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * SensitivityX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * SensitivityY;

            _roty += mouseX;
            _rotx -= mouseY;

            _rotx = Mathf.Clamp(_rotx, TopClamp, BottomClamp);

            transform.rotation = Quaternion.Euler(_rotx, _roty, 0);
            Orientation.localRotation = Quaternion.Euler(transform.localRotation.x, _roty, transform.localRotation.z);
        }
    }
}
