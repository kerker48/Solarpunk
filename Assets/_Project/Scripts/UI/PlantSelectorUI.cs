using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Affiche la plante sélectionnée à l'écran.
/// Responsabilité unique : écouter le PlantSelector et mettre à jour le texte (et l'icône).
/// </summary>
public class PlantSelectorUI : MonoBehaviour
{
    [Tooltip("Le PlantSelector de la scène.")]
    [SerializeField] private PlantSelector plantSelector;

    [Tooltip("Texte qui affiche le nom de la plante sélectionnée.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("Image qui affiche l'icône de la plante (optionnelle).")]
    [SerializeField] private Image iconImage;

    [Tooltip("Mise en forme : {0} = nom, {1} = points rapportés, {2} = position, {3} = nombre total.")]
    [SerializeField] private string format = "«  {0}  »\n<size=60%>{1} pt · {2}/{3}</size>";

    private void OnEnable()
    {
        if (plantSelector == null) return; // Null check : référence oubliée dans l'Inspector
        plantSelector.SelectionChanged += Refresh;
        Refresh(plantSelector.CurrentPlant);
    }

    private void OnDisable()
    {
        if (plantSelector == null) return;
        plantSelector.SelectionChanged -= Refresh;
    }

    private void Refresh(Plant plant)
    {
        if (nameText != null)
        {
            nameText.text = plant != null
                ? string.Format(format, plant.DisplayName, plant.HarvestPoints,
                                plantSelector.CurrentIndex + 1, plantSelector.Count)
                : "Aucune plante";
        }

        if (iconImage != null)
        {
            bool hasIcon = plant != null && plant.Icon != null;
            iconImage.enabled = hasIcon; // cacher l'image s'il n'y a pas d'icône
            if (hasIcon) iconImage.sprite = plant.Icon;
        }
    }
}
