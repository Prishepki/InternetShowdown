using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class ResultsWindow : MonoBehaviour, IGameCanvas
{
    [SerializeField] private CanvasGroup _window;
    [SerializeField] private Transform _statsContainer;
    [SerializeField] private GameObject _statPrefab;

    [Header("Sounds")]
    [SerializeField] private AudioClip _statShow;
    [SerializeField] private AudioClip _windowShow;
    [SerializeField] private AudioClip _windowHide;

    private List<ResultsStat> _stats = new List<ResultsStat>();

    public bool IsActive { get; private set; }

    private GroupsManager _groupsManager;
    private EverywhereCanvas _everywhereCanvas;
    private PauseMenu _pauseMenu;

    private TweenerCore<float, float, FloatOptions> _fadeTween;
    private TweenerCore<Vector3, Vector3, VectorOptions> _scaleTween;

    private void Start()
    {
        _groupsManager = GroupsManager.Singleton();
        _everywhereCanvas = EverywhereCanvas.Singleton();
        _pauseMenu = EverywhereCanvas.PauseMenu();
    }

    public void SetWindowDynamic(bool active)
    {
        SetWindow(active, !_everywhereCanvas.IsVotingActive && !_pauseMenu.PauseMenuOpened);
    }

    public void SetWindow(bool active, bool modifyCursor = true)
    {
        StopCoroutine(nameof(SetWindowCorourine));

        WindowParams windowParams = new WindowParams()
        {
            Active = active,
            ModifyCursor = modifyCursor
        };

        StartCoroutine(nameof(SetWindowCorourine), windowParams);
    }

    private class WindowParams
    {
        public bool Active;
        public bool ModifyCursor;
    }

    private IEnumerator SetWindowCorourine(WindowParams windowParams)
    {
        IsActive = windowParams.Active;

        _groupsManager.SetGroup(_window, windowParams.Active, false);

        _fadeTween.Complete();
        _scaleTween.Complete();

        float endValue = windowParams.Active ? 1 : 0;
        Ease ease = windowParams.Active ? Ease.OutBack : Ease.InBack;

        _fadeTween = _window.DOFade(endValue, 0.45f);
        _scaleTween = _window.transform.DOScale(Vector2.one * endValue, 0.5f).SetEase(ease);

        if (windowParams.Active)
        {
            foreach (Transform stat in _statsContainer) { Destroy(stat.gameObject); }

            _stats.Clear();

            int index = 1;
            foreach (var stat in ResultsStatsJobs.StatsToDisplay)
            {
                ResultsStat newStat = Instantiate(_statPrefab, _statsContainer).GetComponent<ResultsStat>();

                newStat.Group.alpha = 0;

                newStat.Key.text = stat.Key + ":";

                newStat.Value.text = stat.Value.value.ToString();
                newStat.Value.color = stat.Value.color;

                if (index % 2 == 0)
                {
                    newStat.Panel.color = new Color(0, 0, 0, 0.1f);
                }

                _stats.Add(newStat);

                index++;
            }

            SoundSystem.PlayInterfaceSound(new SoundTransporter(_windowShow), volume: 0.6f);

            if (windowParams.ModifyCursor) { Cursor.lockState = CursorLockMode.None; }

            yield return new WaitForSeconds(0.5f);

            float pitch = 0.85f;
            foreach (var stat in _stats)
            {
                stat.Group.DOFade(1, 0.15f);

                SoundSystem.PlayInterfaceSound(new SoundTransporter(_statShow), pitch, pitch, 0.6f);
                pitch += 0.05f;

                yield return new WaitForSeconds(0.125f);
            }
        }
        else
        {
            SoundSystem.PlayInterfaceSound(new SoundTransporter(_windowHide), volume: 0.6f);

            if (windowParams.ModifyCursor) { Cursor.lockState = CursorLockMode.Locked; }

            foreach (var stat in _stats) { stat.Group.alpha = 0; }
        }
    }

    public void OnDisconnect()
    {
        SetWindow(false, false);
    }
}
