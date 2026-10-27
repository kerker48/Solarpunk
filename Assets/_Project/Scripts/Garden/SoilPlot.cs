using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parcelle de terre cultivable.
/// Responsabilité unique : savoir quelles plantes poussent sur cette parcelle
/// et dire s'il reste de la place pour une plante donnée.
/// La capacité dépend de la PLANTE (chaque Prefab de plante définit son maximum par parcelle).
/// Le drone ne peut planter que sur un collider qui porte ce composant.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SoilPlot : MonoBehaviour
{
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

    /// <summary>
    /// Vrai s'il reste de la place pour planter ce type de plante.
    /// Ex. : blé = 15 max par parcelle, tomate = 6 max.
    /// </summary>
    public bool HasRoomFor(Plant plantType)
    {
        if (plantType == null) return false;
        return PlantCount < plantType.MaxPerPlot;
    }

    /// <summary>Enregistre une plante qui vient d'être plantée ici.</summary>
    public void Register(Plant plant)
    {
        if (plant != null) plants.Add(plant);
    }
}
