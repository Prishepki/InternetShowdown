using UnityEngine;
using System;
using Mirror;
using System.Collections.Generic;
using NaughtyAttributes;

public class SoundSystem : NetworkBehaviour
{
    public List<AudioClip> NetworkRegisteredSounds = new List<AudioClip>();

    public static void PlaySound(SoundTransporter sound, SoundPositioner positionMode, float pitchMin = 1, float pitchMax = 1,  float volume = 1, bool enableFade = true)
    {
        AudioClip targetSound = sound.Clips[UnityEngine.Random.Range(0, sound.Clips.Count)];

        GameObject sourceObject = new GameObject(targetSound.name, new Type[] { typeof(AudioSource), typeof(ActiveSoundEffect) });

        ActiveSoundEffect sourceSound = sourceObject.GetComponent<ActiveSoundEffect>();

        if (positionMode.Locked)
        {
            sourceSound.LockSound(positionMode.Target);
        }
        else
        {
            sourceSound.transform.position = positionMode.Position;
        }

        AudioSource sourcePlayer = sourceObject.GetComponent<AudioSource>();

        sourcePlayer.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        sourcePlayer.volume = volume;

        sourcePlayer.clip = targetSound;
        sourceSound.RemoveTime = targetSound.length + 0.15f;

        if (enableFade)
        {
            sourcePlayer.rolloffMode = AudioRolloffMode.Custom;
            sourcePlayer.maxDistance = 85;

            sourcePlayer.spatialBlend = 1;
            sourcePlayer.dopplerLevel = 0;
        }

        sourcePlayer.Play();
    }

    public void PlaySyncedSound(SoundTransporter sound, SoundPositioner positionMode, float pitchMin = 1, float pitchMax = 1, float volume = 1, bool enableFade = true)
    {
        List<int> idxes = new List<int>();

        foreach (var clip in sound.Clips)
        {
            if ( !NetworkRegisteredSounds.Contains(clip))
            { Debug.LogWarning($"Network Registered Sounds does not contain {clip.name}! Are you forgot to add the sound in the list?"); return; }

            idxes.Add(NetworkRegisteredSounds.IndexOf(clip));
        }

        CmdPlaySyncedSound(idxes, positionMode.Target, positionMode.Position, pitchMin, pitchMax, volume, enableFade);
    }

    [Command(requiresAuthority = false)]
    private void CmdPlaySyncedSound(List<int> soundsIndexes, Transform target, Vector3 position, float pitchMin, float pitchMax, float volume, bool enableFade)
    {
        RpcPlaySyncedSound(soundsIndexes, target, position, pitchMin, pitchMax, volume, enableFade);
    }

    [ClientRpc]
    private void RpcPlaySyncedSound(List<int> soundsIndexes, Transform target, Vector3 position, float pitchMin, float pitchMax, float volume, bool enableFade)
    {
        List<AudioClip> targetSounds = new List<AudioClip>();

        for (int i = 0; i < soundsIndexes.Count; i++)
        {
            targetSounds.Add(NetworkRegisteredSounds[soundsIndexes[i]]);
        }
        

        SoundPositioner pos = target == null ? new SoundPositioner(position) : new SoundPositioner(target);

        PlaySound(new SoundTransporter(targetSounds), pos, pitchMin, pitchMax, volume, enableFade);
    }
}

public class SoundTransporter
{
    public readonly List<AudioClip> Clips;

    public SoundTransporter(AudioClip clip)
    {
        Clips = new List<AudioClip>() { clip };
    }

    public SoundTransporter(List<AudioClip> clips)
    {
        Clips = clips;
    }
}

public class SoundPositioner
{
    public readonly Transform Target;
    public readonly Vector3 Position;
    public readonly bool Locked;

    public SoundPositioner(bool locked, Transform target)
    {
        Locked = locked;

        if (locked)
        {
            Target = target;
        }
        else
        {
            Position = target.position;
        }
    }

    public SoundPositioner(Vector3 position)
    {
        Locked = false;
        Position = position;
    }

    public SoundPositioner(Transform target)
    {
        Locked = true;
        Target = target;
    }
}

[Serializable]
public class SoundEffect
{
    public AudioClip Sound;
    public float Volume = 1;

    [MinMaxSlider(-3, 3)]
    public Vector2 Pitch = Vector2.one;

    public bool Lock;
}
