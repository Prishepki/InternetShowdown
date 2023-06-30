using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

public class SoundSystem : NetworkBehaviour
{
    public List<AudioClip> NetworkRegisteredSounds = new List<AudioClip>();

    public static SoundSystem Singleton()
    {
        return FindObjectOfType<SoundSystem>(true);
    }

    public static AudioSource PlayInterfaceSound(SoundTransporter sound, float pitchMin = 1, float pitchMax = 1, float volume = 1)
    {
        return PlaySound(sound, new SoundPositioner(Vector3.zero), SoundType.UI, pitchMin, pitchMax, volume, false);
    }

    public static AudioSource PlaySound(SoundTransporter sound, SoundPositioner positionMode, SoundType type, float pitchMin = 1, float pitchMax = 1, float volume = 1, bool enableFade = true)
    {
        AudioClip targetSound = sound.Clips[UnityEngine.Random.Range(0, sound.Clips.Count)];

        GameObject sourceObject = new GameObject(targetSound.name, new Type[] { typeof(AudioSource), typeof(ActiveSoundEffect) });

        ActiveSoundEffect sourceSound = sourceObject.GetComponent<ActiveSoundEffect>();

        if (positionMode.Locked)
            sourceSound.LockSound(positionMode.Target);
        else
            sourceSound.transform.position = positionMode.Position;

        AudioSource sourcePlayer = sourceObject.GetComponent<AudioSource>();

        AudioMixer mixer = Resources.Load<AudioMixer>("InternetShowdownMaster");
        string groupName = SoundType.GetName(typeof(SoundType), type);
        AudioMixerGroup group = mixer.FindMatchingGroups(groupName).First();

        sourcePlayer.outputAudioMixerGroup = group;

        sourcePlayer.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        sourcePlayer.volume = volume;

        sourcePlayer.clip = targetSound;
        sourceSound.RemoveTime = targetSound.length + 0.15f;

        if (enableFade)
        {
            sourcePlayer.rolloffMode = AudioRolloffMode.Custom;
            sourcePlayer.maxDistance = 85;

            sourcePlayer.spatialBlend = 1;
            sourcePlayer.dopplerLevel = 0.05f;
        }

        sourcePlayer.Play();

        return sourcePlayer;
    }

    public void PlaySFX(SoundTransporter sound, SoundPositioner positionMode, float pitchMin = 1, float pitchMax = 1, float volume = 1, bool enableFade = true)
    {
        List<int> idxes = new List<int>();

        foreach (var clip in sound.Clips)
        {
            if (!NetworkRegisteredSounds.Contains(clip))
            {
                Debug.LogWarning($"Network Registered Sounds does not contain {clip.name}! Are you forgot to add the sound in the list?");
                return;
            }

            idxes.Add(NetworkRegisteredSounds.IndexOf(clip));
        }

        CmdPlaySFXSound(idxes, positionMode.Target, positionMode.Position, pitchMin, pitchMax, volume, enableFade);
    }

    [Command(requiresAuthority = false)]
    private void CmdPlaySFXSound(List<int> soundsIndexes, Transform target, Vector3 position, float pitchMin, float pitchMax, float volume, bool enableFade)
    {
        RpcPlaySFXSound(soundsIndexes, target, position, pitchMin, pitchMax, volume, enableFade);
    }

    [ClientRpc]
    private void RpcPlaySFXSound(List<int> soundsIndexes, Transform target, Vector3 position, float pitchMin, float pitchMax, float volume, bool enableFade)
    {
        List<AudioClip> targetSounds = new List<AudioClip>();

        for (int i = 0; i < soundsIndexes.Count; i++)
        {
            targetSounds.Add(NetworkRegisteredSounds[soundsIndexes[i]]);
        }

        SoundPositioner pos = target ? new SoundPositioner(target) : new SoundPositioner(position);

        PlaySound(new SoundTransporter(targetSounds), pos, SoundType.SFX, pitchMin, pitchMax, volume, enableFade);
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
            Target = target;
        else
            Position = target.position;
    }

    public SoundPositioner(Vector3 position)
    {
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
    [Tooltip("Звук который будет проигран")] public AudioClip Sound;
    [Tooltip("Должна ли позиция звука быть залочена под позицию источника, или задавать позицию только на старте?")] public bool Lock;

    [Tooltip("Громкость звука")] public float Volume = 1;
    [Tooltip("Рандомайзер питча в диапозоне"), MinMaxSlider(-3, 3)] public Vector2 Pitch = Vector2.one;
}

public enum SoundType
{
    SFX,
    UI,
    Music
}
