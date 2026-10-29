using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Affiche le score total et le détail des récoltes (ex. « Score : 5 », « Blé : 3 », « Carotte : 1 »).
/// Responsabilité unique : écouter le ScoreManager et réécrire le texte à chaque changement.
/// </summary>
public class HarvestUI : MonoBehaviour
{
    [Tooltip("Le ScoreManager de la scène (GameManager).")]
    [SerializeField] private ScoreManager scoreManager;

    [Tooltip("Le PlantSelector : sert à lister TOUTES les plantes, même celles pas encore récoltées.")]
    [SerializeField] private PlantSelector plantSelector;

    [Tooltip("Le texte TextMeshPro qui affiche la liste.")]
    [SerializeField] private TMP_Text harvestText;

    [Tooltip("Afficher le score total en première ligne ?")]
    [SerializeField] private bool showTotalScore = true;

    [Tooltip("Mise en forme du score total : {0} = score.")]
    [SerializeField] private string totalFormat = "<b>Score : {0}</b>";

    [Tooltip("Afficher aussi le nombre de récoltes par plante sous le score ?")]
    [SerializeField] private bool showHarvestLines = false;

    [Tooltip("Mise en forme d'une ligne : {0} = nom de la plante, {1} = quantité récoltée.")]
    [SerializeField] private string lineFormat = "<size=75%>{0} : {1}</size>";

    private void OnEnable()
    {
        if (scoreManager == null) return; // Null check : référence oubliée dans l'Inspector
        scoreManager.HarvestsChanged += Refresh;
        scoreManager.ScoreChanged += OnScoreChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (scoreManager == null) return;
        scoreManager.HarvestsChanged -= Refresh;
        scoreManager.ScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int score) => Refresh();

    private void Refresh()
    {
        if (harvestText == null || scoreManager == null) return;
        if (showHarvestLines && plantSelector == null) return;

        // StringBuilder : construire un texte morceau par morceau sans créer plein de chaînes.
        StringBuilder builder = new StringBuilder();
        if (showTotalScore)
        {
            builder.AppendLine(string.Format(totalFormat, scoreManager.Score));
        }
        for (int i = 0; showHarvestLines && i < plantSelector.Count; i++)
        {
            Plant plant = plantSelector.GetPlant(i);
            if (plant == null) continue;
            builder.AppendLine(string.Format(lineFormat, plant.DisplayName,
                                             scoreManager.GetHarvestCount(plant.DisplayName)));
        }
        harvestText.text = builder.ToString();
    }
}
