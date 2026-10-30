using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>Les trois canaux réglables. Le nom sert aussi à trouver le paramètre exposé du mixer ("Volume" + nom).</summary>
public enum SoundChannel { General, Musique, SFX }

/// <summary>
/// Réglages du son (volume 0..1 et « muet » par canal), sauvegardés avec PlayerPrefs
/// et appliqués à l'AudioMixer via ses paramètres exposés : VolumeGeneral, VolumeMusique, VolumeSFX.
/// Classe statique : les réglages sont les mêmes dans toutes les scènes.
/// </summary>
public static class SoundSettings
{
    private const string Prefix = "PotagerSolarpunk_Son_";
    private const float MutedDb = -80f;

    // Volume « normal » de chaque groupe tel que réglé dans le mixer (ex. Musique à +12 dB).
    // Le curseur à 100 % redonne exactement ce niveau.
    private static readonly Dictionary<SoundChannel, float> baseDb = new Dictionary<SoundChannel, float>();

    public static float GetVolume(SoundChannel channel) => PlayerPrefs.GetFloat(Prefix + channel + "_Volume", 1f);
    public static bool IsMuted(SoundChannel channel) => PlayerPrefs.GetInt(Prefix + channel + "_Muet", 0) == 1;

    public static void SetVolume(AudioMixer mixer, SoundChannel channel, float volume)
    {
        PlayerPrefs.SetFloat(Prefix + channel + "_Volume", Mathf.Clamp01(volume));
        Apply(mixer, channel);
    }

    public static void SetMuted(AudioMixer mixer, SoundChannel channel, bool muted)
    {
        PlayerPrefs.SetInt(Prefix + channel + "_Muet", muted ? 1 : 0);
        Apply(mixer, channel);
    }

    public static void ApplyAll(AudioMixer mixer)
    {
        Apply(mixer, SoundChannel.General);
        Apply(mixer, SoundChannel.Musique);
        Apply(mixer, SoundChannel.SFX);
    }

    public static void Save() => PlayerPrefs.Save();

    private static void Apply(AudioMixer mixer, SoundChannel channel)
    {
        if (mixer == null) return;
        string parameter = "Volume" + channel;

        if (!baseDb.ContainsKey(channel))
        {
            if (!mixer.GetFloat(parameter, out float original))
            {
                Debug.LogWarning($"[SoundSettings] Paramètre exposé « {parameter} » introuvable dans le mixer.");
                return;
            }
            baseDb[channel] = original;
        }

        // L'oreille perçoit le volume de façon logarithmique : on convertit 0..1 en décibels.
        float volume = GetVolume(channel);
        float db = IsMuted(channel) || volume <= 0.0001f
            ? MutedDb
            : baseDb[channel] + 20f * Mathf.Log10(volume);
        mixer.SetFloat(parameter, db);
    }
}
