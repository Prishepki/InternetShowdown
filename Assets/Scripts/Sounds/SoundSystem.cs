using UnityEngine;
using System;
using Mirror;
using System.Collections.Generic;

public class SoundSystem : NetworkBehaviour
{
    public List<AudioClip> NetworkRegisteredSounds = new List<AudioClip>();

    public static void PlaySound(SoundTransporter sound, Vector3 position, float pitchMin = 1, float pitchMax = 1,  float volume = 1, bool enableFade = true)
    {
        AudioClip targetSound = sound.Clips[UnityEngine.Random.Range(0, sound.Clips.Count)];

        GameObject sourceObject = new GameObject(targetSound.name, new Type[] { typeof(AudioSource), typeof(DestroyableSound) });

        sourceObject.transform.position = position;

        DestroyableSound sourceSound = sourceObject.GetComponent<DestroyableSound>();
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

    public void PlaySyncedSound(SoundTransporter sound, Vector3 position, float pitchMin = 1, float pitchMax = 1, float volume = 1, bool enableFade = true)
    {
        List<int> idxes = new List<int>();

        foreach (var clip in sound.Clips)
        {
            if ( !NetworkRegisteredSounds.Contains(clip))
            { Debug.LogWarning($"Network Registered Sounds does not contain {clip.name}! Are you forgot to add the sound in the list?"); return; }

            idxes.Add(NetworkRegisteredSounds.IndexOf(clip));
        }

        CmdPlaySyncedSound(idxes, position, pitchMin, pitchMax, volume, enableFade);
    }

    [Command(requiresAuthority = false)]
    private void CmdPlaySyncedSound(List<int> soundsIndexes, Vector3 position, float pitchMin, float pitchMax, float volume, bool enableFade)
    {
        RpcPlaySyncedSound(soundsIndexes, position, pitchMin, pitchMax, volume, enableFade);
    }

    [ClientRpc]
    private void RpcPlaySyncedSound(List<int> soundsIndexes, Vector3 position, float pitchMin, float pitchMax, float volume, bool enableFade)
    {
        List<AudioClip> targetSounds = new List<AudioClip>();

        for (int i = 0; i < soundsIndexes.Count; i++)
        {
            targetSounds.Add(NetworkRegisteredSounds[soundsIndexes[i]]);
        }

        PlaySound(new SoundTransporter(targetSounds), position, pitchMin, pitchMax, volume, enableFade);
    }
}

public class SoundTransporter
{
    public List<AudioClip> Clips;

    public SoundTransporter(AudioClip clip)
    {
        Clips = new List<AudioClip>() { clip };
    }

    public SoundTransporter(List<AudioClip> clips)
    {
        Clips = clips;
    }
}
