using System;
using UnityEngine;

/// <summary>
/// Tient le score de la partie.
/// Responsabilité unique : compter les points et prévenir quiconque s'y intéresse
/// (l'interface, l'écran de fin…) quand le score change.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>Score actuel (lecture seule de l'extérieur).</summary>
    public int Score { get; private set; }

    /// <summary>
    /// Événement déclenché à chaque changement de score, avec la nouvelle valeur.
    /// L'interface s'y abonne : le ScoreManager n'a pas besoin de connaître l'UI.
    /// </summary>
    public event Action<int> ScoreChanged;

    private void Start()
    {
        // Prévenir les abonnés de la valeur de départ (0), pour que l'affichage soit juste dès le début.
        ScoreChanged?.Invoke(Score);
    }

    /// <summary>Ajoute des points (appelé par une plante récoltée).</summary>
    public void AddPoints(int points)
    {
        if (points <= 0) return;
        Score += points;
        ScoreChanged?.Invoke(Score);
    }
}
