using UnityEngine;

/// <summary>
/// Marque un objet comme « parcelle de terre cultivable ».
/// Responsabilité unique : servir d'étiquette. Le drone ne peut planter
/// que si, sous lui, il trouve un collider qui porte ce composant.
/// (Un composant vide est plus sûr qu'un Tag : pas de faute de frappe possible dans une chaîne de caractères.)
/// </summary>
[RequireComponent(typeof(Collider))]
public class SoilPlot : MonoBehaviour
{
}
