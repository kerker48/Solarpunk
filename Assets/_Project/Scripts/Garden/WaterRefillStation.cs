using UnityEngine;

/// <summary>
/// Réservoir où le drone recharge son eau.
/// Responsabilité unique : fournir de l'eau au drone, à un certain débit.
/// Le réservoir a besoin d'un collider en Is Trigger (haut) pour que le drone le détecte en survol.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterRefillStation : MonoBehaviour
{
    [Tooltip("Quantité d'eau rechargée par seconde quand on maintient le bouton.")]
    [SerializeField, Min(0.1f)] private float refillPerSecond = 40f;

    public float RefillPerSecond => refillPerSecond;
}
