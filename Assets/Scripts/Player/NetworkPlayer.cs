using System;
using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
public class NetworkPlayer : NetworkBehaviour
{
    // тут константы на случай если одни и те же значения будут использоваться несколько раз в коде
    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";

    [Header("Components")] // лукашенко ебаный гит я тебя взорву говно хуйни
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private CapsuleCollider _cc;
    [SerializeField] private ItemsReader _ir;

    [Header("Player")]
    [SerializeField, Tooltip("Максимальное хп игрока, выше этого значения выйти невозможно")] private float _maxHealth = 100;

    [Header("Movement Control")]
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

    [SerializeField, Tooltip("Сколько прыжков доступно игроку в воздухе?"), Min(0)] private int _availableAirJumps = 1;

    private int _jumpedTimesInAir;
    private float _airJumpTimeout;

    [Space(9)]

    [SerializeField, Tooltip("Койоти тайм"), Min(0)] private float _coyoteTime = 0.2f;
    [SerializeField, Tooltip("Джамп Баффер"), Min(0)] private float _jumpBuffer = 0.2f;

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;

    [Header("Dashing Control")]
    [SerializeField, Tooltip("Сила рывка пока игрок на земле"), Min(0)] private float _dashGroundedForce = 50f;
    [SerializeField, Tooltip("Сила рывка пока игрок в воздухе"), Min(0)] private float _dashAirForce = 15f;
    [SerializeField, Tooltip("На сколько секунд игрок не сможет делать рывок после совершеного рывка"), Min(0)] private float _dashTimeout = 0.25f;

    [Header("Ground Dashing Control")]
    [SerializeField, Tooltip("Сила рывка вниз"), Min(0)] private float _groundDashForce = 5f;

    [Header("Property Checking")]
    [SerializeField] private LayerMask _mapLayers;

    [Header("Inputs")]
    public KeyCode JumpKey = KeyCode.Space;
    public KeyCode DashKey = KeyCode.LeftShift;
    public KeyCode GroundDashKey = KeyCode.LeftControl;

    private (Vector3 center, float radius) _groundChecking;
    private Vector3 _slopeNormal;

    private float _accel;
    private float _bhop;
    private float _bhopTimer;
    private Vector2 _inputs;
    private Vector3 _playerDirection;

    [HideInInspector] public bool IsMoving;
    [HideInInspector] public bool IsGrounded;

    private bool _wantToJump;

    private bool _wantToDash;
    private bool _wantToGroundDash;
    private bool _readyToDash;

    private Vector3 _playerSlopeDirection;

    private bool _isColliding;

    [Header("Particles")]
    [SerializeField, Tooltip("Не меняй")] private GameObject[] _particles;

    [Header("Sounds")]
    [SerializeField, Tooltip("Звук прыжка")] private AudioClip _jumpSound;
    [SerializeField, Tooltip("Звук получения урона")] private AudioClip _damageSound;
    [SerializeField, Tooltip("Локальный звук получения урона")] private AudioClip _localDamageSound;
    [SerializeField, Tooltip("Звук уведомления о том что игрок ударил кого то")] private AudioClip _hitLogSound;
    [SerializeField, Tooltip("Звук уведомления о том что игрок убил кого то")] private AudioClip _killLogSound;

    [Header("Objects")]
    [SerializeField, Tooltip("Не меняй")] private Transform _orientation;
    [SerializeField, Tooltip("Не меняй")] private GameObject _body;

    private EverywhereCanvas _everywhereCanvas;

    [HideInInspector] public Camera PlayerCamera;
    [HideInInspector] public CameraMovement PlayerMoveCamera;

    [HideInInspector] public bool AllowMovement;

    [SyncVar] public string Nickname;
    [SyncVar] public int Score;

    [Command]
    private void CmdInitialize(string nickname)
    {
        Nickname = nickname;
    }

    #region HealthSystem

    [SyncVar] public float Health;

    public void Heal(float amount)
    {
        if (!isLocalPlayer) return;

        SetHealth(Health + amount);
    }

    public void TakeDamage(float amount)
    {
        if (!isLocalPlayer) return;

        FindObjectOfType<SoundSystem>().PlaySyncedSound(new SoundTransporter(_damageSound), new SoundPositioner(transform.position), 0.95f, 1.05f, 1f);
        SoundSystem.PlaySound(new SoundTransporter(_localDamageSound), new SoundPositioner(transform));

        SetHealth(Health - amount);
    }

    public void SetHealth(float amount)
    {
        if (!isLocalPlayer) return;

        if (amount > _maxHealth || amount < 0)
        {
            Debug.LogWarning($"{gameObject.name} attempted to set health out of bounds (target: {amount}, maximum: {_maxHealth}, minimum: 0)");
        }

        float clampedAmount = Mathf.Clamp(amount, 0, _maxHealth);

        float difference = Health - clampedAmount;

        if (difference == 0)
        {
            Debug.LogWarning("It's useless to change health");
            return;
        }

        CmdSetHealth(clampedAmount);

        StartCoroutine(nameof(OnHealthChanged), clampedAmount);
    }

    private IEnumerator OnHealthChanged(float amount)
    {
        yield return new WaitUntil(() => Health == amount); // на случай задержки синхронизации поля Health

        _everywhereCanvas.SetDisplayHealth(amount);

        if (Health <= 0)
        {
            OnDeath();
        }
    }

    private void OnDeath()
    {
        Action respawn = OnRespawn;

        _ir.LoseItem();
        _ir.RemoveAllMutations();

        AllowMovement = false;
        PlayerMoveCamera.BlockMovement = true;

        CmdDisablePlayer(false);

        _rb.velocity = Vector3.zero;

        _everywhereCanvas.StartDeathScreen(ref respawn);
    }

    private void OnRespawn()
    {
        SetHealth(100);

        AllowMovement = true;
        PlayerMoveCamera.BlockMovement = false;

        CmdDisablePlayer(true);

        transform.position = Vector3.zero;
    }

    [Command]
    private void CmdSetHealth(float amount) { Health = amount; }

    [Command]
    private void CmdDisablePlayer(bool enable) { RpcDisablePlayer(enable); }

    [ClientRpc]
    private void RpcDisablePlayer(bool enable)
    {
        _body.GetComponent<MeshRenderer>().enabled = enable;
        _cc.enabled = enable;
    }

    [Command(requiresAuthority = false)]
    public void CmdHitPlayer(NetworkIdentity owner, float damage)
    {
        if (Health <= 0)
        {
            Debug.LogWarning("Can't hit a dead player");
            return;
        }

        TRpcHitPlayer(connectionToClient, damage);
        TRpcLogHit(owner.connectionToClient, Health - damage <= 0);
    }

    [TargetRpc]
    private void TRpcHitPlayer(NetworkConnectionToClient target, float damage)
    {
        TakeDamage(damage);
    }

    [TargetRpc]
    private void TRpcLogHit(NetworkConnectionToClient target, bool gonnaDie)
    {
        NetworkPlayer networkPlayer = target.identity.GetComponent<NetworkPlayer>();

        if (networkPlayer == this)
        {
            Debug.LogWarning("Can't log hit for self-damage");
            return;
        }

        networkPlayer.LogHit();

        if (gonnaDie)
        {
            networkPlayer.LogKill();
        }
    }

    #endregion

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
        AllowMovement = true;

        PlayerCurrentStats.Singleton.ResetStats();
        PlayerMutationStats.Singleton.ResetStats();
    }

    public override void OnStartLocalPlayer() // то же самое что и старт, только для локального игрока
    {
        Initialize();
    }

    private void Initialize() // уничтожаем другие камеры на сцене и создаем себе новую
    {
        CmdInitialize(PlayerPrefs.GetString("PlayerNicknameValue", "Player"));

        _everywhereCanvas = EverywhereCanvas.Singleton();

        _everywhereCanvas.Initialize(this);
        _everywhereCanvas.SetMaxHealth(_maxHealth);

        SetHealth(_maxHealth);

        _body.SetActive(false);
        gameObject.layer = 12;

        Camera[] otherCameras = FindObjectsOfType<Camera>(true);

        foreach (Camera camera in otherCameras)
        {
            Destroy(camera.gameObject);
        }

        GameObject newCamera = new GameObject("Player Camera", (typeof(CameraMovement)));

        PlayerCamera = newCamera.AddComponent<Camera>();

        InitializeCamera();
    }

    private void InitializeCamera() // инициализируем камеру (задаем параметры по дефолту и добовляем нужные компоненты)
    {
        PlayerCamera.fieldOfView = 80;
        PlayerCamera.nearClipPlane = 0.01f;

        GameObject camera = PlayerCamera.gameObject;
        GameObject cameraHolder = new GameObject("Camera Holder");

        PlayerMoveCamera = camera.GetComponent<CameraMovement>();
        camera.AddComponent<AudioListener>();
        camera.GetComponent<CameraMovement>().Player = this;

        Transform cameraHolderTransform = cameraHolder.transform;
        camera.GetComponent<CameraMovement>().CamHolder = cameraHolderTransform;

        cameraHolderTransform.SetParent(gameObject.transform);
        cameraHolderTransform.localPosition = Vector3.zero;

        camera.transform.SetParent(cameraHolderTransform);
        camera.transform.localPosition = Vector3.up * 0.5f;

        PlayerMoveCamera.Orientation = _orientation;
    }

    private void SetVariables() //реализация тупая да и хуй с ней хд (короче забей тебе не надо знать зачем это)
    {
        _groundChecking = (transform.position + Vector3.down * (_cc.height / 2), _cc.radius / 3);
    }

    private void TryGetRequiredComponents() // тут мы получаем нужные компоненты и записываем их
    {
        TryGetComponent<Rigidbody>(out _rb);
        TryGetComponent<CapsuleCollider>(out _cc);
        TryGetComponent<ItemsReader>(out _ir);
    }

    private void Update()
    {
        if (!isLocalPlayer) return; // эта строка заставляет выйти из метода, если мы не являемся локальным игроком

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        DebugKeys();
#endif

        RecieveInputs();
        SetVariables();

        JumpHandle();

        if (_wantToDash && _readyToDash)
        {
            Dash();
        }

        if (_wantToGroundDash)
        {
            GroundDash();
        }

        RaycastHit hit;

        Transform cameraTransform = PlayerCamera.transform;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, 1500f, LayerMask.GetMask("Player")))
        {
            NetworkPlayer player;

            if (hit.transform.TryGetComponent<NetworkPlayer>(out player))
            {
                _everywhereCanvas.SwitchNicknameVisibility(true, player.Nickname);
            }
        }
        else
        {
            _everywhereCanvas.SwitchNicknameVisibility(false);
        }
    }

    private void JumpHandle()
    {
        if (IsGrounded)
        {
            _coyoteTimeCounter = _coyoteTime;

            _airJumpTimeout = 0.5f;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;

            if (_airJumpTimeout > 0)
            {
                _airJumpTimeout -= Time.deltaTime;
            }
        }

        if (_wantToJump)
        {
            _jumpBufferCounter = _jumpBuffer;
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }

        bool jumpRequested = _coyoteTimeCounter > 0f;
        bool jumpIsPossible = _jumpBufferCounter > 0f;

        if (jumpRequested && jumpIsPossible)
        {
            Jump();

            _jumpBufferCounter = 0f;
        }

        if (_wantToJump && !IsGrounded && !jumpRequested && _jumpedTimesInAir < _availableAirJumps && _airJumpTimeout > 0)
        {
            Jump();

            _jumpedTimesInAir++;

            if (_jumpedTimesInAir == _availableAirJumps)
            {
                StartCoroutine(nameof(WaitForLand));
            }
        }

        if (Input.GetKeyUp(JumpKey))
        {
            _coyoteTimeCounter = 0;
        }
    }

    private IEnumerator WaitForLand()
    {
        yield return new WaitUntil(() => IsGrounded);

        _jumpedTimesInAir = 0;
    }

    private void DebugKeys()
    {

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            TakeDamage(5);
        }

        if (Input.GetKeyDown(KeyCode.Equals))
        {
            Heal(5);
        }
#endif

    }

    private void Jump()
    {
        if (!AllowMovement || _everywhereCanvas.PauseMenuOpened) return;

        PlayerCurrentStats.Singleton.Bounce = _jumpForce;

        _rb.velocity = new Vector3(_rb.velocity.x, PlayerCurrentStats.Singleton.Bounce + PlayerMutationStats.Singleton.Bounce, _rb.velocity.z);

        if (_bhopTimer > 0) // если наш банихоп не в таймауте
        {
            _bhop += _bunnyHopFactor; // то при прыжке добовляем скорости
        }
        else
        {
            _bhop = 0; // иначе сбрасываем скорсоть
        }

        _bhopTimer = _bunnyHopTimeout;

        // эффекты
        FindObjectOfType<SoundSystem>().PlaySyncedSound(new SoundTransporter(_jumpSound), new SoundPositioner(transform.position), 0.85f, 1f, 0.6f);
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

        IsGrounded = _isColliding && CheckForGrounded();

        _wantToJump = Input.GetKeyDown(JumpKey);
        _wantToDash = Input.GetKeyDown(DashKey);
        _wantToGroundDash = Input.GetKeyDown(GroundDashKey);

        _playerDirection = _orientation.forward * _inputs.y + _orientation.right * _inputs.x;
    }

    public Vector2 GetAxisInputs() // можно было и без этого метода но тут чисто ради выебонов
    {
        if (!AllowMovement || _everywhereCanvas.PauseMenuOpened) return Vector2.zero;

        return new Vector2(Input.GetAxisRaw(_horizontal), Input.GetAxisRaw(_vertical));
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return; // эта строка заставляет выйти из метода, если мы не являемся локальным игроком

        IdleHandle();
        RigidbodyMovement();

        if (_bhopTimer > 0)
        {
            _bhopTimer -= Time.fixedDeltaTime;
        }
    }

    private bool CheckForGrounded() // у меня нет поля для проверки на земле ли игрок, пусть лучше метод будет бля)
    {
        bool grounded = Physics.CheckSphere(_groundChecking.center, _groundChecking.radius, _mapLayers.value, QueryTriggerInteraction.Ignore);

        return grounded;
    }

    public bool IsSloped() // не тупле метод не ахуевший сука
    {
        if (!IsGrounded) return false;

        RaycastHit hit;
        Physics.Raycast(_groundChecking.center, Vector3.down, out hit, 1f, _mapLayers.value, QueryTriggerInteraction.Ignore);
        _slopeNormal = hit.normal;

        return _slopeNormal != Vector3.up;
    }

    private void IdleHandle()
    {
        _rb.useGravity = !IsSloped();
    }

    private void RigidbodyMovement() // тут мы двигаем перса по оси X и Z (а так же делаем слоуп хандлинг по оси Y)
    {
        if (!AllowMovement)
        {
            _rb.velocity = Vector3.zero;

            return;
        }

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

        bool sloped = IsSloped();

        _playerSlopeDirection = Vector3.ProjectOnPlane(_playerDirection, _slopeNormal);

        float angleBoost = Mathf.Abs(Vector3.Angle(Vector3.up, _slopeNormal) * 1.75f);

        PlayerCurrentStats.Singleton.Speed = (_startSpeed + _accel + _bhop + angleBoost);

        if (IsGrounded)
        {
            _rb.drag = _dragOnGround;
        }
        else
        {
            _rb.drag = 0;
            PlayerCurrentStats.Singleton.Speed /= _airSpeedDivider;
        }

        if (sloped)
        {
            _rb.AddForce(_playerSlopeDirection * (PlayerCurrentStats.Singleton.Speed + PlayerMutationStats.Singleton.Speed));
        }
        else
        {
            _rb.AddForce(_playerDirection * (PlayerCurrentStats.Singleton.Speed + PlayerMutationStats.Singleton.Speed));
        }
    }

    private void Dash() // АХАХАХАХАХАХАХАХАХАХАХАХА ДЕД С ЛЕСТНИЦЫ ЕБНУЛСЯ СМЕШНО АХАХАХАХАХАХХА
    {
        if (!AllowMovement || _everywhereCanvas.PauseMenuOpened) return;

        float targetForce = IsGrounded ? _dashGroundedForce : _dashAirForce;

        _rb.AddForce(_playerDirection * targetForce, ForceMode.Impulse);

        _readyToDash = false;
        Invoke(nameof(ResetDash), _dashTimeout);
    }

    private void GroundDash() // АХАХАХАХАХАХАХАХАХАХАХАХА ДЕД С ЛЕСТНИЦЫ ЕБНУЛСЯ СМЕШНО АХАХАХАХАХАХХА
    {
        if (!AllowMovement || _everywhereCanvas.PauseMenuOpened || IsGrounded) return;

        _rb.velocity = new Vector3(_rb.velocity.x, -_groundDashForce, _rb.velocity.z);

        StartCoroutine(nameof(ShakeWhenLanded));
    }

    private IEnumerator ShakeWhenLanded()
    {
        yield return new WaitUntil(() => IsGrounded);

        PlayerMoveCamera.Shake(0.15f, 0.15f);
    }

    public void LogHit()
    {
        SoundSystem.PlaySound(new SoundTransporter(_hitLogSound), new SoundPositioner(transform.position));
        PlayerMoveCamera.Shake(strength: 0.1f);

        AddScore(1);
    }

    public void LogKill() // TODO доделать это говно
    {
        SoundSystem.PlaySound(new SoundTransporter(_killLogSound), new SoundPositioner(transform.position), volume: 3);
        PlayerMoveCamera.Shake(strength: 0.25f);
        _everywhereCanvas.LogKill();

        AddScore(3);
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

    private void AddScore(int amount)
    {
        CmdChangeScore(amount);
    }

    [Command]
    private void CmdChangeScore(int amount)
    {
        Score += amount;
    }

    private void OnCollisionEnter(Collision other)
    {
        _isColliding = true;
    }

    private void OnCollisionExit(Collision other)
    {
        _isColliding = false;
    }
}

public class PlayerMutationStats : PlayerStats // статы, на которые влияют мутации на самом деле (но игроки тупые обезьяны они об этом не узнают)
{
    public static PlayerCurrentStats Singleton = new();
}

public class PlayerCurrentStats : PlayerStats // эти статы сделаны для защиты (чтоб никакие хакеры хуякеры не могли получить всего игрока)
{
    public static PlayerCurrentStats Singleton = new();
}

public class PlayerStats
{
    public float Speed;
    public float Bounce;
    public byte Luck;
    public float Damage;

    public void ResetStats()
    {
        Speed = 0;
        Bounce = 0;
        Luck = 0;
        Damage = 0;
    }
}
