using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tient le score de la partie et le détail des récoltes.
/// Responsabilité unique : compter (points + nombre de récoltes par type de plante)
/// et prévenir quiconque s'y intéresse (l'interface, l'écran de fin…) quand ça change.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>Score actuel (lecture seule de l'extérieur).</summary>
    public int Score { get; private set; }

    // Nombre de récoltes par type de plante : clé = nom affiché ("Blé"), valeur = quantité.
    private readonly Dictionary<string, int> harvestCounts = new Dictionary<string, int>();

    /// <summary>Événement déclenché à chaque changement de score, avec la nouvelle valeur.</summary>
    public event Action<int> ScoreChanged;

    /// <summary>Événement déclenché à chaque récolte (pour l'affichage du détail des récoltes).</summary>
    public event Action HarvestsChanged;

    private void Start()
    {
        // Prévenir les abonnés des valeurs de départ, pour que l'affichage soit juste dès le début.
        ScoreChanged?.Invoke(Score);
        HarvestsChanged?.Invoke();
    }

    /// <summary>Nombre de récoltes pour un type de plante (0 si jamais récolté).</summary>
    public int GetHarvestCount(string plantName)
    {
        return harvestCounts.TryGetValue(plantName, out int count) ? count : 0;
    }

    /// <summary>Nombre total de légumes récoltés, tous types confondus.</summary>
    public int TotalHarvested
    {
        get
        {
            int total = 0;
            foreach (int count in harvestCounts.Values) total += count;
            return total;
        }
    }

    /// <summary>Enregistre une récolte : +1 pour ce type de plante, et ajoute ses points.</summary>
    public void RecordHarvest(string plantName, int points)
    {
        harvestCounts[plantName] = GetHarvestCount(plantName) + 1;
        AddPoints(points);           // le score d'abord…
        HarvestsChanged?.Invoke();   // …puis le détail : l'affichage est à jour en une seule fois
    }

    /// <summary>Ajoute des points.</summary>
    public void AddPoints(int points)
    {
        if (points <= 0) return;
        Score += points;
        ScoreChanged?.Invoke(Score);
    }
}
