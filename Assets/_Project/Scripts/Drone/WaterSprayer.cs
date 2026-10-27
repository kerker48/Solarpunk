using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permet au drone d'arroser ce qui se trouve sous lui.
/// Responsabilité unique : tant qu'on maintient « arroser », jouer le jet de particules
/// et prévenir chaque plante située dans la zone d'eau sous le drone.
/// </summary>
public class WaterSprayer : MonoBehaviour
{
    [Header("Commandes")]
    [Tooltip("Action 'Water' : clic droit / gâchette gauche (LT), à maintenir.")]
    [SerializeField] private InputActionReference waterAction;

    [Header("Effet visuel")]
    [Tooltip("Système de particules du jet d'eau (enfant du drone, orienté vers le bas).")]
    [SerializeField] private ParticleSystem waterParticles;

    [Header("Zone arrosée")]
    [Tooltip("Rayon de la zone arrosée sous le drone (en unités).")]
    [SerializeField, Min(0.1f)] private float waterRadius = 1f;

    [Tooltip("Profondeur de la zone arrosée sous le drone (doit atteindre le sol depuis la hauteur max).")]
    [SerializeField, Min(0.5f)] private float waterDepth = 12f;

    public bool IsWatering { get; private set; }

    private void OnEnable()
    {
        if (waterAction != null) waterAction.action.Enable();
    }

    private void OnDisable()
    {
        if (waterAction != null) waterAction.action.Disable();
        SetSprayVisible(false);
    }

    private void Update()
    {
        if (waterAction == null) return;

        // IsPressed() : vrai à chaque image tant que le bouton est maintenu
        // (contrairement à 'performed', qui ne se déclenche qu'une fois à l'appui).
        IsWatering = waterAction.action.IsPressed();
        SetSprayVisible(IsWatering);

        if (IsWatering)
        {
            WaterPlantsBelow();
        }
    }

    private void WaterPlantsBelow()
    {
        // Une capsule verticale, du drone jusque sous le sol : tout ce qui la touche est « sous le jet ».
        // QueryTriggerInteraction.Collide : on veut détecter les zones trigger des plantes.
        Vector3 top = transform.position;
        Vector3 bottom = top + Vector3.down * waterDepth;
        Collider[] hits = Physics.OverlapCapsule(top, bottom, waterRadius, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            Plant plant = hit.GetComponentInParent<Plant>();
            if (plant != null) // Null check : le collider peut appartenir au sol, au drone…
            {
                plant.Water();
            }
        }
    }

    private void SetSprayVisible(bool visible)
    {
        if (waterParticles == null) return;
        if (visible && !waterParticles.isPlaying) waterParticles.Play();
        else if (!visible && waterParticles.isPlaying) waterParticles.Stop();
    }

    // Affiche la zone arrosée dans la vue Scene quand le drone est sélectionné.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.8f);
        Vector3 top = transform.position;
        Vector3 bottom = top + Vector3.down * waterDepth;
        Gizmos.DrawWireSphere(top, waterRadius);
        Gizmos.DrawWireSphere(bottom, waterRadius);
        Gizmos.DrawLine(top + Vector3.right * waterRadius, bottom + Vector3.right * waterRadius);
        Gizmos.DrawLine(top - Vector3.right * waterRadius, bottom - Vector3.right * waterRadius);
    }
}
