using System.Collections.Generic;
using UnityEngine;

public class MusicSystem : MonoBehaviour
{
    [SerializeField] private List<AudioClip> _lobbySoundtracks;
    [SerializeField] private List<AudioClip> _matchSoundtracks;

    public static List<ActiveMusic> ActiveSoundtracks { get; private set; } = new List<ActiveMusic>();

    public static MusicSystem Singleton()
    {
        return FindObjectOfType<MusicSystem>(true);
    }

    private void Awake()
    {
        ActiveSoundtracks.Clear();
    }

    public static ActiveMusic StartMusic(MusicGameStates state, float offset = 0f)
    {
        MusicSystem current = Singleton();

        AudioClip target = null;
        AudioSource source = null;

        if (state == MusicGameStates.Lobby)
        {
            if (current._lobbySoundtracks.Count > 0)
            {
                target = current._lobbySoundtracks[Random.Range(0, current._lobbySoundtracks.Count)];
            }
        }
        else if (state == MusicGameStates.Match)
        {
            if (current._matchSoundtracks.Count > 0)
            {
                target = current._matchSoundtracks[Random.Range(0, current._matchSoundtracks.Count)];
            }
        }

        if (target == null)
        {
            Debug.LogWarning("There's no music to play");
            return null;
        }

        source = SoundSystem.PlayInterfaceSound(new SoundTransporter(target), volume: 0.525f);
        source.time = offset;

        ActiveMusic newInstance = new ActiveMusic()
        {
            Current = target,
            Source = source
        };

        ActiveSoundtracks.Add(newInstance);

        return newInstance;
    }

    public static void RemoveMusicElement(ActiveMusic target)
    {
        if (!ActiveSoundtracks.Contains(target)) return;

        ActiveSoundtracks.Remove(target);
    }
}

public class ActiveMusic
{
    public AudioClip Current;
    public AudioSource Source;

    ~ActiveMusic()
    {
        MusicSystem.RemoveMusicElement(this);
    }
}

public enum MusicGameStates
{
    Lobby,
    Match
}
