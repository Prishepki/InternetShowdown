using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
public class NetworkPlayer : NetworkBehaviour
{
    // тут константы на случай если одни и те же значения будут использоваться несколько раз в коде
    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";

    [Header("Components")]
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private CapsuleCollider _cc;

    [Header("Movement")]
    [SerializeField, Tooltip("Стартовая скорость игрока без акселерации")] private float _startSpeed = 50f;

    [Space(9)]

    [SerializeField, Tooltip("Каждые 20 милисекунд акселерация будет увеличиваться на (0.02 * значение этого поля)")] private float _accelerationFactor = 10f;
    [SerializeField, Tooltip("Максимальное значение акселерации")] private float _maximumAcceleration = 30f;

    [Space(9)]

    [SerializeField, Tooltip("Чем меньше значение, тем больше скольжение у игрока. Так же наоборот"), Min(0)] private float _dragOnGround = 8.25f;

    [Header("Jumping Control")]
    [SerializeField, Tooltip("Сила прыжка"), Min(0)] private float _jumpForce = 11.25f;
    [SerializeField, Tooltip("Когда игрок не на земле, его скорость будет поделена на значение этого поля"), Min(1)] private float _airSpeedDivider = 6.25f;

    [Space(9)]

    [SerializeField, Tooltip("Когда игрок банихопит, его скорость постоянно плюсуется с этим значением каждый прыжок"), Min(0)] private float _bunnyHopFactor = 0.5f;
    [SerializeField, Tooltip("Если блять \nпосле прыжка проходит столько времени, то вся скорость с банихопа сбрасывается"), Min(0)] private float _bunnyHopTimeout = 0.35f;


    [Header("Property Checking")]
    [SerializeField] private LayerMask _mapLayers;

    [Header("Inputs")]
    public KeyCode JumpKey = KeyCode.Space;
    public KeyCode DashKey = KeyCode.LeftShift;

    private (Vector3 center, float radius) _groundChecking;

    private float _accel;
    private float _bhop;
    private float _bhopTimer;
    private Vector2 _inputs;

    private bool _isMoving;
    private bool _wantToJump;

    [Header("Objects")]
    [SerializeField, Tooltip("Не меняй")] private Transform _orientation;

    private Camera _playerCamera;
    private CameraMovement _playerMoveCamera;

    private void OnValidate() // этот метод вызывается когда в инспекторе меняется поле или после компиляции скрипта
    {
        TryGetRequiredComponents();
        SetVariables();

        if (_rb != null) // важные параметры для ригидбоди
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate; // чтоб было плавно
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // чтоб была не лагучая коллизия
            _rb.freezeRotation = true; // чтоб игрока не вертело как ебанутого
        }
    }

    public override void OnStartLocalPlayer()
    {
        Initialize();
    }

    private void Initialize()
    {
        Camera[] otherCameras = FindObjectsOfType<Camera>(true);
        
        foreach (Camera camera in otherCameras)
        {
            Destroy(camera.gameObject);
        }

        GameObject newCamera = new GameObject("Player Camera", (typeof(CameraMovement)));

        _playerCamera = newCamera.AddComponent<Camera>();

        InitializeCamera();
    }

    private void InitializeCamera()
    {
        _playerCamera.fieldOfView = 75;
        _playerCamera.nearClipPlane = 0.01f;

        GameObject camera = _playerCamera.gameObject;

        _playerMoveCamera = camera.GetComponent<CameraMovement>();
        camera.AddComponent<AudioListener>();

        Transform cameraHolder = new GameObject("Camera Holder").transform;

        cameraHolder.SetParent(gameObject.transform);
        cameraHolder.localPosition = Vector3.zero;

        camera.transform.SetParent(cameraHolder);
        camera.transform.localPosition = Vector3.up * 0.5f;

        _playerMoveCamera.Orientation = _orientation;
    }

    private void SetVariables()
    {
        _groundChecking = (transform.position + Vector3.down * (_cc.height / 2), _cc.radius / 3);
    }

    private void TryGetRequiredComponents()
    {
        // тут мы получаем нужные компоненты и записываем их

        TryGetComponent<Rigidbody>(out _rb);
        TryGetComponent<CapsuleCollider>(out _cc);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        RecieveInputs();
        SetVariables();

        if (_wantToJump && CheckIsGrounded())
        {
            Jump();
        }
    }

    private void Jump()
    {
        _rb.velocity = new Vector3(_rb.velocity.x, _jumpForce, _rb.velocity.z);

        if (_bhopTimer > 0)
        {
            _bhop += _bunnyHopFactor;
        }
        else
        {
            _bhop = 0;
        }

        _bhopTimer = _bunnyHopTimeout;
    }

    private void RecieveInputs() // ТУТ мы записываем значения в переменые связаные с инпутом, кнопками на клавиатуре, мышке и прочей хуйне
    {
        _inputs = GetAxisInputs();
        _isMoving = _inputs.magnitude > 0;

        _wantToJump = Input.GetKeyDown(JumpKey);
    }

    private Vector2 GetAxisInputs() // можно было и без этого метода но тут чисто ради выебонов
    {
        return new Vector2(Input.GetAxisRaw(_horizontal), Input.GetAxisRaw(_vertical));
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        RigidbodyMovement();

        if (_bhopTimer > 0)
        {
            _bhopTimer -= Time.fixedDeltaTime;
        }
    }

    private bool CheckIsGrounded()
    {
        bool grounded = Physics.CheckSphere(_groundChecking.center, _groundChecking.radius, _mapLayers.value, QueryTriggerInteraction.Ignore);

        return grounded;
    }

    private void RigidbodyMovement() // тут мы двигаем перса по оси X и Z
    {
        if (!_isMoving)
        {
            _bhop = 0;
            _accel = 0;
        }
        else
        {
            // а вот как просчитывается акселерация
            _accel += Time.deltaTime * _accelerationFactor;
            _accel = Mathf.Clamp(_accel, 0, _maximumAcceleration);
        }

        Vector3 playerDiretcion = _orientation.forward * _inputs.y + _orientation.right * _inputs.x;

        if (CheckIsGrounded())
        {
            _rb.AddForce(playerDiretcion * (_startSpeed + _accel + _bhop));
            _rb.drag = _dragOnGround;
        }
        else
        {
            _rb.AddForce((playerDiretcion * (_startSpeed + _accel + _bhop)) / _airSpeedDivider);
            _rb.drag = 0;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(_groundChecking.center, _groundChecking.radius);
    }
}
