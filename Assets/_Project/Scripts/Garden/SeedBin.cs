using UnityEngine;

/// <summary>
/// Bac à graines pour UN type de légume.
/// Responsabilité unique : fournir des graines de ce légume au drone, à un certain débit.
/// Le bac a besoin d'un collider en Is Trigger (haut) pour que le drone le détecte en survol.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SeedBin : MonoBehaviour
{
    [Tooltip("Le légume dont ce bac fournit les graines (Prefab de la plante).")]
    [SerializeField] private Plant seedPlant;

    [Tooltip("Graines données par seconde quand on maintient R au-dessus du bac.")]
    [SerializeField, Min(0.1f)] private float seedsPerSecond = 4f;

    public Plant SeedPlant => seedPlant;
    public float SeedsPerSecond => seedsPerSecond;

    /// <summary>Nom du légume (affiché dans l'interface).</summary>
    public string SeedName => seedPlant != null ? seedPlant.DisplayName : "?";
}
