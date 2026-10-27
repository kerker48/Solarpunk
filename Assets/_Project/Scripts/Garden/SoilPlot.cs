using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parcelle de terre cultivable.
/// Responsabilité unique : savoir quelles plantes poussent sur cette parcelle
/// et dire si on peut encore y planter (nombre maximum de plantes).
/// Le drone ne peut planter que sur un collider qui porte ce composant.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SoilPlot : MonoBehaviour
{
    [Tooltip("Nombre maximum de plantes en même temps sur cette parcelle.")]
    [SerializeField, Min(1)] private int maxPlants = 4;

    // Les plantes actuellement sur la parcelle.
    private readonly List<Plant> plants = new List<Plant>();

    /// <summary>Nombre de plantes encore vivantes sur la parcelle.</summary>
    public int PlantCount
    {
        get
        {
            plants.RemoveAll(plant => plant == null); // oublier les plantes récoltées ou éclatées
            return plants.Count;
        }
    }

    /// <summary>Vrai s'il reste de la place sur la parcelle.</summary>
    public bool HasRoom => PlantCount < maxPlants;

    /// <summary>Enregistre une plante qui vient d'être plantée ici.</summary>
    public void Register(Plant plant)
    {
        if (plant != null) plants.Add(plant);
    }
}
