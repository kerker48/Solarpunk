using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Affiche l'état du drone : jauge d'eau et contenu de la soute.
/// Responsabilité unique : écouter WaterSprayer et DroneCargo, et mettre l'interface à jour.
/// </summary>
public class DroneStatusUI : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private WaterSprayer waterSprayer;
    [SerializeField] private DroneCargo cargo;
    [SerializeField] private DroneInteractor interactor;

    [Tooltip("Le PlantSelector : sert à lister chaque type de légume dans la soute.")]
    [SerializeField] private PlantSelector plantSelector;

    [Header("Eau")]
    [Tooltip("Image de la jauge (Image Type = Filled).")]
    [SerializeField] private Image waterFill;

    [Tooltip("Texte à côté de la jauge (optionnel).")]
    [SerializeField] private TMP_Text waterText;

    [Header("Soute")]
    [Tooltip("Texte du contenu de la soute.")]
    [SerializeField] private TMP_Text cargoText;

    [Header("Aide contextuelle")]
    [Tooltip("Texte qui indique quoi faire au-dessus d'un bac ou du réservoir (optionnel).")]
    [SerializeField] private TMP_Text hintText;

    private void OnEnable()
    {
        if (waterSprayer != null) waterSprayer.WaterChanged += UpdateWater;
        if (cargo != null) cargo.CargoChanged += UpdateCargo;
        if (waterSprayer != null) UpdateWater(waterSprayer.WaterRatio);
        UpdateCargo();
    }

    private void OnDisable()
    {
        if (waterSprayer != null) waterSprayer.WaterChanged -= UpdateWater;
        if (cargo != null) cargo.CargoChanged -= UpdateCargo;
    }

    private void Update()
    {
        // L'aide change selon ce que le drone survole : on la vérifie à chaque image.
        if (hintText == null || interactor == null) return;
        if (interactor.BinBelow != null)
            hintText.text = $"R : déposer {interactor.BinBelow.AcceptedName}";
        else if (interactor.IsOverWaterStation)
            hintText.text = "R (maintenu) : remplir l'eau";
        else if (interactor.SeedBinBelow != null)
            hintText.text = $"R (maintenu) : prendre des graines de {interactor.SeedBinBelow.SeedName}";
        else
            hintText.text = "";
    }

    private void UpdateWater(float ratio)
    {
        if (waterFill != null) waterFill.fillAmount = ratio;
        if (waterText != null) waterText.text = $"Eau : {Mathf.RoundToInt(ratio * 100f)} %";
    }

    private void UpdateCargo()
    {
        if (cargoText == null || cargo == null) return;
        StringBuilder builder = new StringBuilder();
        builder.Append($"Soute : {cargo.Count}/{cargo.Capacity}");
        if (!cargo.HasRoom) builder.Append("  <color=#FFB347>PLEINE</color>");

        // Détail du contenu : une ligne par type de légume présent dans la soute.
        if (plantSelector != null)
        {
            for (int i = 0; i < plantSelector.Count; i++)
            {
                Plant plant = plantSelector.GetPlant(i);
                if (plant == null) continue;
                int amount = cargo.GetCount(plant.DisplayName);
                if (amount > 0)
                {
                    builder.Append($"\n<size=80%>{plant.DisplayName} : {amount}</size>");
                }
            }
        }
        cargoText.text = builder.ToString();
    }
}
