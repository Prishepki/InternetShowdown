using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EverywhereCanvas : MonoBehaviour // юи которое будет видно ВЕЗДЕ
{
    public TMP_Text Timer;

    public Slider UseTimer;
    public Image UseTimerFill;

    public GameObject _playerDebugPanel;
    public TMP_Text[] _playerDebugStats;

    public CanvasGroup _killLog;

    [Header("Health Slider")]
    public Slider Health;
    public Image HealthFill;
    
    [Space(9)]

    [SerializeField] private float _healthBarAniamtionSpeed = 5;

    [SerializeField] private Color _healthMaxColor;
    [SerializeField] private Color _healthMinColor;

    private float _targetHealth;

    [Header("Death Screen")]
    [SerializeField] private GameObject _deathScreen;
    [SerializeField] private TMP_Text _respawnCountdown;
    [SerializeField] private TMP_Text _deathPhrase;
    [SerializeField] private List<string> _deathPhrases = new List<string>();

    private void Update()
    {
        _playerDebugPanel.SetActive(false);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        _playerDebugPanel.SetActive(true);
        DebugStats();
#endif

        UpdateHealthDisplay();
    }

    private void UpdateHealthDisplay()
    {
        Health.value = Mathf.Lerp(Health.value, _targetHealth, Time.deltaTime * _healthBarAniamtionSpeed);
        HealthFill.color = Color.Lerp(_healthMinColor, _healthMaxColor, Health.value / Health.maxValue);
    }

    private void Start()
    {
        EnableUseTimer(false);

        _killLog.alpha = 0;

        _deathScreen.SetActive(false);
    }

    public void CancelUseTimer()
    {
        EnableUseTimer(false);

        StopCoroutine(nameof(LerpUseTimer));
    }

    public void StartUseTimer(float time)
    {
        EnableUseTimer(true);

        StartCoroutine(nameof(LerpUseTimer), time);
    }

    private void EnableUseTimer(bool enable)
    {
        UseTimer.gameObject.SetActive(enable);
    }

    public void SetMaxHealth(float value)
    {
        Health.maxValue = value;
    }

    public void SetDisplayHealth(float value)
    {
        _targetHealth = value;
    }

    public void LogKill()
    {
        StartCoroutine(LogCoroutine(2, 0.65f, _killLog));
    }

    public void StartDeathScreen(ref Action onRespawn)
    {
        StartCoroutine(RespawnScreenCoroutine(onRespawn));
    }

    private IEnumerator RespawnScreenCoroutine(Action onRespawn)
    {
        _deathScreen.SetActive(true);
        _deathPhrase.text = _deathPhrases[UnityEngine.Random.Range(0, _deathPhrases.Count)];

        for (int i = 5; i > 0; i--)
        {
            _respawnCountdown.text = $"Respawning in {i}";
            yield return new WaitForSeconds(1f);
        }

        if (onRespawn != null)
        {
            onRespawn.Invoke();
        }

        _deathScreen.SetActive(false);
    }

    private IEnumerator LogCoroutine(float wait, float fadeDuration, CanvasGroup target)
    {
        float elapsed = 0.0f;

        target.alpha = 1;

        yield return new WaitForSeconds(wait);

        while (elapsed < fadeDuration)
        {
            target.alpha = Mathf.Lerp(1, 0, elapsed / fadeDuration);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.alpha = 0;
    }

    private IEnumerator LerpUseTimer(float duration)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            UseTimer.value = Mathf.Lerp(0, 1, elapsed / duration);
            UseTimerFill.color = new Color(1, 1 - UseTimer.value, 1 - UseTimer.value / 3, Mathf.Sin(1 - UseTimer.value));
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        UseTimer.value = 1;

        EnableUseTimer(false);
    }

    private void Awake()
    {
        DontDestroyOnLoad(transform);
    }
    
    public static EverywhereCanvas Singleton()
    {
        return FindObjectOfType<EverywhereCanvas>();
    }

    private void DebugStats()
    {
        for (int i = 0; i < _playerDebugStats.Length; i++)
        {
            switch (i)
            {
                case 0:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Speed}";
                    break;

                case 1:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Bounce}";
                    break;

                case 2:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Luck}";
                    break;

                case 3:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} notImplement";
                    break;

                case 4:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Speed}";
                    break;

                case 5:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Bounce}";
                    break;

                case 6:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Luck}";
                    break;

                case 7:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} notImplement";
                    break;
            }
        }
    }
}
