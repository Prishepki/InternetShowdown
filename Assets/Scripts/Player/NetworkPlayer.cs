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

    [Space(9)]

    [SerializeField, Tooltip("Легкость использования прыжка (койоти тайм и джамп баффер). Менять не советую"), Min(0)] private float _jumpEasiness = 0.3f;

    [Header("Dashing Control")]
    [SerializeField, Tooltip("Сила рывка пока игрок на земле"), Min(0)] private float _dashGroundedForce = 50f;
    [SerializeField, Tooltip("Сила рывка пока игрок в воздухе"), Min(0)] private float _dashAirForce = 15f;
    [SerializeField, Tooltip("На сколько секунд игрок не сможет делать рывок после совершеного рывка"), Min(0)] private float _dashTimeout = 0.25f;

    private float? _lastGroundedTime;
    private float? _lastTryToJump;

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
    private Vector3 _playerDiretcion;

    [HideInInspector] public bool IsMoving;
    private bool _wantToJump;

    private bool _wantToDash;
    private bool _readyToDash;

    [Header("Particles")]
    [SerializeField, Tooltip("Не меняй")] private GameObject[] _particles;

    [Header("Objects")]
    [SerializeField, Tooltip("Не меняй")] private Transform _orientation;
    [SerializeField, Tooltip("Не меняй")] private GameObject _body;

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

    private void Start()
    {
        InitializeVariables();
    }

    private void InitializeVariables()
    {
        ResetDash();
    }

    public override void OnStartLocalPlayer() // то же самое что и старт, только для локального игрока
    {
        Initialize();
    }

    private void Initialize() // уничтожаем другие камеры на сцене и создаем себе новую
    {
        _body.SetActive(false);
        
        Camera[] otherCameras = FindObjectsOfType<Camera>(true);

        foreach (Camera camera in otherCameras)
        {
            Destroy(camera.gameObject);
        }

        GameObject newCamera = new GameObject("Player Camera", (typeof(CameraMovement)));

        _playerCamera = newCamera.AddComponent<Camera>();

        InitializeCamera();
    }

    private void InitializeCamera() // инициализируем камеру (задаем параметры по дефолту и добовляем нужные компоненты)
    {
        _playerCamera.fieldOfView = 80;
        _playerCamera.nearClipPlane = 0.01f;

        GameObject camera = _playerCamera.gameObject;
        GameObject cameraHolder = new GameObject("Camera Holder");

        _playerMoveCamera = camera.GetComponent<CameraMovement>();
        camera.AddComponent<AudioListener>();
        camera.GetComponent<CameraMovement>().Player = this;

        Transform cameraHolderTransform = cameraHolder.transform;
        camera.GetComponent<CameraMovement>().CamHolder = cameraHolderTransform;

        cameraHolderTransform.SetParent(gameObject.transform);
        cameraHolderTransform.localPosition = Vector3.zero;

        camera.transform.SetParent(cameraHolderTransform);
        camera.transform.localPosition = Vector3.up * 0.5f;

        _playerMoveCamera.Orientation = _orientation;
    }

    private void SetVariables() //реализация тупая да и хуй с ней хд (короче забей тебе не надо знать зачем это)
    {
        _groundChecking = (transform.position + Vector3.down * (_cc.height / 2), _cc.radius / 3);
    }

    private void TryGetRequiredComponents() // тут мы получаем нужные компоненты и записываем их
    {
        TryGetComponent<Rigidbody>(out _rb);
        TryGetComponent<CapsuleCollider>(out _cc);
    }

    private void Update()
    {
        if (!isLocalPlayer) return; // эта строка заставляет выйти из метода, если мы не являемся локальным игроком

        RecieveInputs();
        SetVariables();

        if (CheckIsGrounded())
        {
            _lastGroundedTime = Time.time;
        }

        if (_wantToJump)
        {
            _lastTryToJump = Time.time;
        }

        if (Time.time - _lastGroundedTime <= _jumpEasiness)
        {
            if (Time.time - _lastTryToJump <= _jumpEasiness)
            {
                Jump();
            }
        }

        if (_wantToDash && _readyToDash)
        {
            Dash();
        }
    }

    private void Jump()
    {
        _lastTryToJump = null;
        _lastGroundedTime = null;

        _rb.velocity = new Vector3(_rb.velocity.x, _jumpForce, _rb.velocity.z);

        if (_bhopTimer > 0) // если наш банихоп не в таймауте
        {
            _bhop += _bunnyHopFactor; // то при прыжке добовляем скорости
        }
        else
        {
            _bhop = 0; // иначе сбрасываем скорсоть
        }

        _bhopTimer = _bunnyHopTimeout;

        CmdSpawnParticle(0);
    }

    [Command]
    private void CmdSpawnParticle(int idx) // охуевший метод спавна партиклов спасибо миррор
    {
        GameObject particle = Instantiate(_particles[idx], transform.position, _particles[idx].transform.rotation);

        NetworkServer.Spawn(particle, gameObject);
    }

    private void RecieveInputs() // ТУТ мы записываем значения в переменые связаные с инпутом, кнопками на клавиатуре, мышке и прочей хуйне
    {
        _inputs = GetAxisInputs();
        IsMoving = _inputs.magnitude > 0;

        _wantToJump = Input.GetKeyDown(JumpKey);
        _wantToDash = Input.GetKeyDown(DashKey);

        _playerDiretcion = _orientation.forward * _inputs.y + _orientation.right * _inputs.x;
    }

    public Vector2 GetAxisInputs() // можно было и без этого метода но тут чисто ради выебонов
    {
        return new Vector2(Input.GetAxisRaw(_horizontal), Input.GetAxisRaw(_vertical));
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return; // эта строка заставляет выйти из метода, если мы не являемся локальным игроком

        RigidbodyMovement();

        if (_bhopTimer > 0)
        {
            _bhopTimer -= Time.fixedDeltaTime;
        }
    }

    public bool CheckIsGrounded() // у меня нет поля для проверки на земле ли игрок, пусть лучше метод будет бля)
    {
        bool grounded = Physics.CheckSphere(_groundChecking.center, _groundChecking.radius, _mapLayers.value, QueryTriggerInteraction.Ignore);

        return grounded;
    }

    public (bool sloped, float angle) CheckIsSloped() // тупле метод ахуевший просто
    {
        RaycastHit hit;

        Physics.Raycast(_groundChecking.center, Vector3.down, out hit, 1f, _mapLayers.value, QueryTriggerInteraction.Ignore);

        float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
        bool sloped = slopeAngle != 0 ? true : false;

        return (sloped, slopeAngle);
    }

    private void RigidbodyMovement() // тут мы двигаем перса по оси X и Z
    {
        if (!IsMoving)
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

        if (CheckIsGrounded())
        {
            float targetForce = (_startSpeed + _accel + _bhop) + Mathf.Abs(CheckIsSloped().angle); // ебать я слоуп хандлинг

            _rb.AddForce(_playerDiretcion * targetForce);
            _rb.drag = _dragOnGround;
        }
        else
        {
            _rb.AddForce((_playerDiretcion * (_startSpeed + _accel + _bhop)) / _airSpeedDivider);
            _rb.drag = 0;
        }
    }

    private void Dash() // АХАХАХАХАХАХАХАХАХАХАХАХА ДЕД С ЛЕСТНИЦЫ ЕБНУЛСЯ СМЕШНО АХАХАХАХАХАХХА
    {
        float targetForce = CheckIsGrounded() ? _dashGroundedForce : _dashAirForce;

        _rb.AddForce(_playerDiretcion * targetForce, ForceMode.Impulse);

        _readyToDash = false;
        Invoke(nameof(ResetDash), _dashTimeout);
    }

    private void ResetDash()
    {
        _readyToDash = true;
    }

    private void OnDrawGizmosSelected() // забей это не важно, это надо чтоб в эдиторе рисовались подсказки
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(_groundChecking.center, _groundChecking.radius);
    }
}
