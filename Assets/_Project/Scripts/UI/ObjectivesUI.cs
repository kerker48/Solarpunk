using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Affiche le chronomètre et la progression des objectifs.
/// Responsabilité unique : lire GameSession / ScoreManager et écrire le texte.
/// </summary>
public class ObjectivesUI : MonoBehaviour
{
    [SerializeField] private GameSession gameSession;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private PlantSelector plantSelector;

    [Tooltip("Texte du chronomètre (ex. 04:32).")]
    [SerializeField] private TMP_Text timerText;

    [Tooltip("Texte des objectifs (score et récoltes par légume).")]
    [SerializeField] private TMP_Text objectivesText;

    [Tooltip("Sous ce nombre de secondes, le chrono passe en orange.")]
    [SerializeField] private float warningTime = 30f;

    private void Update()
    {
        // Le chrono change à chaque image : on rafraîchit simplement ici (texte court, coût négligeable).
        if (gameSession == null) return;
        UpdateTimer();
        UpdateObjectives();
    }

    private void UpdateTimer()
    {
        if (timerText == null) return;
        int total = Mathf.CeilToInt(gameSession.TimeRemaining);
        string time = $"{total / 60:00}:{total % 60:00}";
        timerText.text = gameSession.TimeRemaining <= warningTime ? $"<color=#FFB347>{time}</color>" : time;
    }

    private void UpdateObjectives()
    {
        if (objectivesText == null || scoreManager == null) return;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(Line("Score", scoreManager.Score, gameSession.TargetScore));
        if (plantSelector != null)
        {
            for (int i = 0; i < plantSelector.Count; i++)
            {
                Plant plant = plantSelector.GetPlant(i);
                if (plant == null) continue;
                builder.AppendLine(Line(plant.DisplayName, gameSession.GetHarvested(plant), gameSession.TargetPerPlant));
            }
        }
        objectivesText.text = builder.ToString();
    }

    // Une ligne d'objectif : verte et cochée quand elle est atteinte.
    private static string Line(string label, int current, int target)
    {
        return current >= target
            ? $"<color=#8BE28B><b>{label} : {current}/{target}  OK</b></color>"
            : $"{label} : {current}/{target}";
    }
}
