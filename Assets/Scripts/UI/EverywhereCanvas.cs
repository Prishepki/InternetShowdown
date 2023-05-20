using System;
using System.Collections;
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

    private float _targetHealth;

    [Header("Health Slider")]
    public Slider Health;
    public Image HealthFill;
    
    [Space(9)]

    [SerializeField] private float _healthBarAniamtionSpeed = 5;

    [SerializeField] private Color _healthMaxColor;
    [SerializeField] private Color _healthMinColor;


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
