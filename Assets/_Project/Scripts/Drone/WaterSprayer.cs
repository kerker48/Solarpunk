using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permet au drone d'arroser ce qui se trouve sous lui.
/// Responsabilité unique : tant qu'on maintient « arroser » ET qu'il reste de l'eau,
/// jouer le jet de particules, vider le réservoir et prévenir chaque plante sous le drone.
/// </summary>
public class WaterSprayer : MonoBehaviour
{
    [Header("Commandes")]
    [Tooltip("Action 'Water' : clic droit / gâchette gauche (LT), à maintenir.")]
    [SerializeField] private InputActionReference waterAction;

    [Header("Effet visuel")]
    [Tooltip("Système de particules du jet d'eau (enfant du drone, orienté vers le bas).")]
    [SerializeField] private ParticleSystem waterParticles;

    [Header("Son")]
    [Tooltip("AudioSource en boucle (Loop coché) avec le son d'eau : joué pendant l'arrosage.")]
    [SerializeField] private AudioSource waterLoopSource;

    [Header("Zone arrosée")]
    [Tooltip("Rayon de la zone arrosée sous le drone (en unités).")]
    [SerializeField, Min(0.1f)] private float waterRadius = 1f;

    [Tooltip("Profondeur de la zone arrosée sous le drone (doit atteindre le sol depuis la hauteur max).")]
    [SerializeField, Min(0.5f)] private float waterDepth = 12f;

    [Header("Réservoir d'eau")]
    [Tooltip("Quantité d'eau maximale du réservoir.")]
    [SerializeField, Min(1f)] private float maxWater = 100f;

    [Tooltip("Eau consommée par seconde pendant l'arrosage.")]
    [SerializeField, Min(0f)] private float waterUsePerSecond = 20f;

    public bool IsWatering { get; private set; }

    /// <summary>Eau restante.</summary>
    public float CurrentWater { get; private set; }

    /// <summary>Eau restante entre 0 (vide) et 1 (plein), pratique pour une jauge.</summary>
    public float WaterRatio => CurrentWater / maxWater;

    /// <summary>Déclenché quand le niveau d'eau change (valeur entre 0 et 1).</summary>
    public event Action<float> WaterChanged;

    private void Awake()
    {
        CurrentWater = maxWater; // on part avec le réservoir plein
    }

    private void Start()
    {
        WaterChanged?.Invoke(WaterRatio);
    }

    /// <summary>Remplit le réservoir (appelé par le DroneInteractor au-dessus du réservoir).</summary>
    public void Refill(float amount)
    {
        if (amount <= 0f || CurrentWater >= maxWater) return;
        CurrentWater = Mathf.Min(maxWater, CurrentWater + amount);
        WaterChanged?.Invoke(WaterRatio);
    }

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
        // On n'arrose que si le bouton est maintenu ET qu'il reste de l'eau.
        IsWatering = waterAction.action.IsPressed() && CurrentWater > 0f;
        SetSprayVisible(IsWatering);

        if (IsWatering)
        {
            CurrentWater = Mathf.Max(0f, CurrentWater - waterUsePerSecond * Time.deltaTime);
            WaterChanged?.Invoke(WaterRatio);
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
        if (waterParticles != null)
        {
            if (visible && !waterParticles.isPlaying) waterParticles.Play();
            else if (!visible && waterParticles.isPlaying) waterParticles.Stop();
        }
        if (waterLoopSource != null)
        {
            if (visible && !waterLoopSource.isPlaying) waterLoopSource.Play();
            else if (!visible && waterLoopSource.isPlaying) waterLoopSource.Stop();
        }
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
