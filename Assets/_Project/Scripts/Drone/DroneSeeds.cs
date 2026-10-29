using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Le stock de graines du drone : un stock par type de légume.
/// Responsabilité unique : savoir combien de graines de chaque type le drone transporte,
/// en consommer une à chaque plantation, et se remplir au bac à graines.
/// La capacité de chaque stock est définie sur le Prefab de la plante (Seed Capacity).
/// </summary>
public class DroneSeeds : MonoBehaviour
{
    [Tooltip("Le PlantSelector : sert à connaître tous les types de graines au démarrage.")]
    [SerializeField] private PlantSelector plantSelector;

    [Tooltip("Le drone démarre-t-il avec tous ses stocks pleins ?")]
    [SerializeField] private bool startFull = true;

    // Stock par plante. En float pour pouvoir remonter « petit à petit » au bac ;
    // on affiche et on consomme des graines entières.
    private readonly Dictionary<string, float> stock = new Dictionary<string, float>();

    /// <summary>Déclenché quand un stock change (l'interface s'y abonne).</summary>
    public event Action SeedsChanged;

    private void Start()
    {
        if (plantSelector != null && startFull)
        {
            for (int i = 0; i < plantSelector.Count; i++)
            {
                Plant plant = plantSelector.GetPlant(i);
                if (plant != null) stock[plant.DisplayName] = plant.SeedCapacity;
            }
        }
        SeedsChanged?.Invoke();
    }

    /// <summary>Nombre de graines entières disponibles pour cette plante.</summary>
    public int GetCount(Plant plant)
    {
        if (plant == null) return 0;
        return stock.TryGetValue(plant.DisplayName, out float amount) ? Mathf.FloorToInt(amount) : 0;
    }

    /// <summary>Vrai s'il reste au moins une graine de cette plante.</summary>
    public bool HasSeed(Plant plant) => GetCount(plant) >= 1;

    /// <summary>Utilise une graine. Renvoie false s'il n'y en a plus.</summary>
    public bool TryConsume(Plant plant)
    {
        if (!HasSeed(plant)) return false;
        stock[plant.DisplayName] -= 1f;
        SeedsChanged?.Invoke();
        return true;
    }

    /// <summary>Ajoute des graines (appelé au-dessus du bac à graines), sans dépasser la capacité.</summary>
    public void Refill(Plant plant, float amount)
    {
        if (plant == null || amount <= 0f) return;
        float current = stock.TryGetValue(plant.DisplayName, out float value) ? value : 0f;
        if (current >= plant.SeedCapacity) return;

        int before = Mathf.FloorToInt(current);
        current = Mathf.Min(plant.SeedCapacity, current + amount);
        stock[plant.DisplayName] = current;

        // Ne prévenir l'interface que quand le nombre ENTIER change (évite de rafraîchir à chaque image).
        if (Mathf.FloorToInt(current) != before) SeedsChanged?.Invoke();
    }
}
