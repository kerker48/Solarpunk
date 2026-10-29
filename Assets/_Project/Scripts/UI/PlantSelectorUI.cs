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

    [Tooltip("Stock de graines du drone (optionnel) : affiche les graines restantes.")]
    [SerializeField] private DroneSeeds seeds;

    [Tooltip("Ligne ajoutée sous la plante : {0} = graines restantes, {1} = capacité.")]
    [SerializeField] private string seedsFormat = "\n<size=60%>Graines : {0}/{1}</size>";

    [Tooltip("Mise en forme : {0} = nom, {1} = points rapportés, {2} = position, {3} = nombre total.")]
    [SerializeField] private string format = "«  {0}  »\n<size=60%>{1} pt · {2}/{3}</size>";

    private void OnEnable()
    {
        if (plantSelector == null) return; // Null check : référence oubliée dans l'Inspector
        plantSelector.SelectionChanged += Refresh;
        if (seeds != null) seeds.SeedsChanged += OnSeedsChanged;
        Refresh(plantSelector.CurrentPlant);
    }

    private void OnDisable()
    {
        if (plantSelector == null) return;
        plantSelector.SelectionChanged -= Refresh;
        if (seeds != null) seeds.SeedsChanged -= OnSeedsChanged;
    }

    private void OnSeedsChanged() => Refresh(plantSelector.CurrentPlant);

    private void Refresh(Plant plant)
    {
        if (nameText != null)
        {
            nameText.text = plant != null
                ? string.Format(format, plant.DisplayName, plant.HarvestPoints,
                                plantSelector.CurrentIndex + 1, plantSelector.Count)
                : "Aucune plante";
            if (plant != null && seeds != null)
            {
                int count = seeds.GetCount(plant);
                string line = string.Format(seedsFormat, count, plant.SeedCapacity);
                // Plus de graines : la ligne passe en orange pour attirer l'œil.
                nameText.text += count == 0 ? $"<color=#FFB347>{line}</color>" : line;
            }
        }

        if (iconImage != null)
        {
            bool hasIcon = plant != null && plant.Icon != null;
            iconImage.enabled = hasIcon; // cacher l'image s'il n'y a pas d'icône
            if (hasIcon) iconImage.sprite = plant.Icon;
        }
    }
}
