using TMPro;
using UnityEngine;

/// <summary>
/// Affiche le score à l'écran.
/// Responsabilité unique : écouter le ScoreManager et mettre le texte à jour.
/// Le ScoreManager ne connaît pas l'interface : c'est l'interface qui s'abonne à son événement.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Tooltip("Le ScoreManager de la scène (GameManager).")]
    [SerializeField] private ScoreManager scoreManager;

    [Tooltip("Le texte TextMeshPro qui affiche le score.")]
    [SerializeField] private TMP_Text scoreText;

    [Tooltip("Mise en forme du texte : {0} est remplacé par le score.")]
    [SerializeField] private string format = "Blé récolté : {0}";

    private void OnEnable()
    {
        if (scoreManager == null) return; // Null check : référence oubliée dans l'Inspector
        scoreManager.ScoreChanged += UpdateText;   // s'abonner
        UpdateText(scoreManager.Score);            // afficher la valeur actuelle tout de suite
    }

    private void OnDisable()
    {
        if (scoreManager == null) return;
        scoreManager.ScoreChanged -= UpdateText;   // se désabonner (évite un appel sur un objet détruit)
    }

    private void UpdateText(int score)
    {
        if (scoreText == null) return;
        scoreText.text = string.Format(format, score);
    }
}
