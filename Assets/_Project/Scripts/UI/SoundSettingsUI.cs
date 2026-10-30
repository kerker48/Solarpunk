using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Panneau « Réglages du son » (menu titre et menu pause) : un curseur et une case « Muet » par canal.
/// Applique aussi les réglages sauvegardés dès le chargement de la scène.
/// </summary>
public class SoundSettingsUI : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;

    [Header("Panneau")]
    [Tooltip("Le panneau des réglages (caché au départ).")]
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Le contenu du menu (boutons) caché pendant les réglages.")]
    [SerializeField] private GameObject menuContent;
    [Tooltip("Le bouton « Son » : re-sélectionné en fermant (navigation manette).")]
    [SerializeField] private GameObject openButton;

    [Header("Curseurs")]
    [SerializeField] private Slider generalSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Cases Muet")]
    [SerializeField] private Toggle generalMute;
    [SerializeField] private Toggle musicMute;
    [SerializeField] private Toggle sfxMute;

    private void Start()
    {
        // Dans Start (et pas Awake) : le mixer n'accepte pas toujours les réglages dans Awake.
        SoundSettings.ApplyAll(mixer);

        Bind(generalSlider, generalMute, SoundChannel.General);
        Bind(musicSlider, musicMute, SoundChannel.Musique);
        Bind(sfxSlider, sfxMute, SoundChannel.SFX);

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Update()
    {
        // Si le menu pause se ferme alors que les réglages sont ouverts, on referme proprement.
        if (settingsPanel != null && settingsPanel.activeSelf && !settingsPanel.activeInHierarchy) Close();
    }

    private void Bind(Slider slider, Toggle mute, SoundChannel channel)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(SoundSettings.GetVolume(channel));
            slider.onValueChanged.AddListener(value => SoundSettings.SetVolume(mixer, channel, value));
        }
        if (mute != null)
        {
            mute.SetIsOnWithoutNotify(SoundSettings.IsMuted(channel));
            mute.onValueChanged.AddListener(muted => SoundSettings.SetMuted(mixer, channel, muted));
        }
    }

    // ----- Boutons -----

    public void Open()
    {
        if (menuContent != null) menuContent.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        Select(generalSlider != null ? generalSlider.gameObject : null);
    }

    public void Close()
    {
        SoundSettings.Save();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuContent != null) menuContent.SetActive(true);
        Select(openButton);
    }

    private static void Select(GameObject target)
    {
        if (target != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }
}
