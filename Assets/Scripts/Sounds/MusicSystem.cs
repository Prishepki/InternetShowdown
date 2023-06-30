using System.Collections.Generic;
using UnityEngine;

public class MusicSystem : MonoBehaviour
{
    [SerializeField] private List<AudioClip> _lobbySoundtracks;
    [SerializeField] private List<AudioClip> _matchSoundtracks;

    public static ActiveMusic MainSoundtrack { get; private set; }

    public static MusicSystem Singleton()
    {
        return FindObjectOfType<MusicSystem>(true);
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

        source = SoundSystem.PlaySound(new SoundTransporter(target), new SoundPositioner(Vector3.zero), SoundType.Music, volume: 0.525f, enableFade: false);
        source.time = offset;

        ActiveMusic newInstance = new ActiveMusic()
        {
            Current = target,
            Source = source
        };

        MainSoundtrack = newInstance;

        return newInstance;
    }
}

public class ActiveMusic
{
    public AudioClip Current;
    public AudioSource Source;
}

public enum MusicGameStates
{
    Lobby,
    Match
}
