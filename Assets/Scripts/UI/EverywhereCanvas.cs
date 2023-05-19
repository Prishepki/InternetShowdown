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

    public Slider Health;
    public Image HealthFill;

    public TMP_Text[] _asasa;

    private void Update()
    {
        for (int i = 0; i < _asasa.Length; i++)
        {
            switch (i)
            {
                case 0:
                    _asasa[i].text = $"{_asasa[i].name} {PlayerMutationStats.Singleton.Speed}";
                    break;

                case 1:
                    _asasa[i].text = $"{_asasa[i].name} {PlayerMutationStats.Singleton.Bounce}";
                    break;
                
                case 2:
                    _asasa[i].text = $"{_asasa[i].name} {PlayerMutationStats.Singleton.Luck}";
                    break;
                
                case 3:
                    _asasa[i].text = $"{_asasa[i].name} notImplement";
                    break;

                case 4:
                    _asasa[i].text = $"{_asasa[i].name} {PlayerCurrentStats.Singleton.Speed}";
                    break;
                
                case 5:
                    _asasa[i].text = $"{_asasa[i].name} {PlayerCurrentStats.Singleton.Bounce}";
                    break;
                
                case 6:
                    _asasa[i].text = $"{_asasa[i].name} {PlayerCurrentStats.Singleton.Luck}";
                    break;
                
                case 7:
                    _asasa[i].text = $"{_asasa[i].name} notImplement";
                    break;
            }
        }
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
}
