using UnityEngine;

/// <summary>
/// Joue les effets sonores ponctuels du jeu (plantation, récolte, éclatement, dépôt…).
/// Responsabilité unique : jouer un son court, avec une légère variation de hauteur
/// pour qu'il ne soit pas répétitif. Son AudioSource passe par le groupe « SFX » de l'Audio Mixer.
/// Les plantes, créées pendant la partie, le reçoivent par injection (comme le ScoreManager).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour
{
    [Tooltip("Variation aléatoire de la hauteur du son (0.1 = ±10 %).")]
    [SerializeField, Range(0f, 0.5f)] private float pitchVariation = 0.08f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    /// <summary>Joue un son court (ne coupe pas les sons déjà en cours).</summary>
    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || source == null) return; // Null check : son pas encore assigné
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, volume);
    }
}
