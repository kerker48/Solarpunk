using System.Collections;
using UnityEngine;

/// <summary>Les étapes de croissance d'une plante.</summary>
public enum GrowthStage
{
    Seed,      // graine : pas encore récoltable
    Ripe,      // mûre : prête à être récoltée
    Overripe   // trop mûre : va bientôt éclater
}

/// <summary>
/// Fait pousser une plante : graine → légume mûr → trop mûr → éclatement.
/// Responsabilité unique : gérer le temps de croissance et l'étape actuelle.
/// Raisonner en ÉTATS (une seule variable 'Stage') évite une pile de booléens contradictoires.
/// </summary>
public class Plant : MonoBehaviour
{
    [Header("Visuels de chaque étape (enfants du Prefab)")]
    [Tooltip("Objet affiché pendant l'étape graine.")]
    [SerializeField] private GameObject seedVisual;

    [Tooltip("Objet affiché quand le légume est mûr.")]
    [SerializeField] private GameObject ripeVisual;

    [Tooltip("Objet affiché quand le légume est trop mûr.")]
    [SerializeField] private GameObject overripeVisual;

    [Tooltip("Objet qui grossit avec le temps (le parent des 3 visuels).")]
    [SerializeField] private Transform growthRoot;

    [Header("Durées (en secondes)")]
    [Tooltip("Temps pour passer de graine à légume mûr.")]
    [SerializeField] private float seedDuration = 10f;

    [Tooltip("Temps pendant lequel le légume reste mûr (fenêtre de récolte).")]
    [SerializeField] private float ripeDuration = 7f;

    [Tooltip("Temps avant que le légume trop mûr éclate.")]
    [SerializeField] private float overripeDuration = 4f;

    [Tooltip("Variation aléatoire des durées (0.15 = ±15 %) : chaque plante pousse à son propre rythme.")]
    [SerializeField, Range(0f, 0.5f)] private float durationVariation = 0.15f;

    [Header("Taille")]
    [Tooltip("Taille à la plantation.")]
    [SerializeField] private float startScale = 0.3f;

    [Tooltip("Taille quand le légume devient mûr.")]
    [SerializeField] private float ripeScale = 1f;

    [Tooltip("Taille juste avant l'éclatement.")]
    [SerializeField] private float overripeScale = 1.3f;

    [Header("Arrosage")]
    [Tooltip("Bonus de croissance quand la plante est arrosée (2 = pousse 2× plus vite).")]
    [SerializeField, Min(1f)] private float wateredGrowthMultiplier = 2.5f;

    [Tooltip("Combien de secondes la terre reste humide après le dernier arrosage.")]
    [SerializeField, Min(0f)] private float wetDuration = 2f;

    /// <summary>Étape actuelle (lecture seule de l'extérieur).</summary>
    public GrowthStage Stage { get; private set; }

    /// <summary>Vrai seulement quand le légume peut être récolté.</summary>
    public bool IsRipe => Stage == GrowthStage.Ripe;

    /// <summary>Vrai tant que la plante profite encore d'un arrosage récent.</summary>
    public bool IsWet => Time.time < wetUntil;

    // Moment (en secondes de jeu) jusqu'auquel la plante reste arrosée.
    private float wetUntil = -1f;

    // Vitesse de croissance actuelle : normale, ou accélérée si la plante est arrosée.
    private float GrowthSpeed => IsWet ? wateredGrowthMultiplier : 1f;

    /// <summary>
    /// Appelée par le drone (WaterSprayer) à chaque image où la plante est sous le jet d'eau.
    /// Prolonge la période « arrosée » de wetDuration secondes.
    /// </summary>
    public void Water()
    {
        wetUntil = Time.time + wetDuration;
    }

    // Start peut être une coroutine : elle tourne en parallèle du jeu sans le bloquer.
    private IEnumerator Start()
    {
        float variation = Random.Range(1f - durationVariation, 1f + durationVariation);

        SetStage(GrowthStage.Seed);
        yield return Grow(seedDuration * variation, startScale, ripeScale);

        SetStage(GrowthStage.Ripe);
        yield return Grow(ripeDuration * variation, ripeScale, Mathf.Lerp(ripeScale, overripeScale, 0.5f));

        SetStage(GrowthStage.Overripe);
        yield return Grow(overripeDuration * variation, Mathf.Lerp(ripeScale, overripeScale, 0.5f), overripeScale);

        Burst();
    }

    /// <summary>
    /// Attend 'duration' secondes de croissance en faisant grossir la plante de 'fromScale' à 'toScale'.
    /// Le temps avance de Time.deltaTime × GrowthSpeed à chaque image : arroser = pousser plus vite.
    /// </summary>
    private IEnumerator Grow(float duration, float fromScale, float toScale)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * GrowthSpeed;
            float progress = Mathf.Clamp01(elapsed / duration);
            if (growthRoot != null)
            {
                growthRoot.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, progress);
            }
            yield return null; // attendre l'image suivante, sans bloquer le reste du jeu
        }
    }

    private void SetStage(GrowthStage newStage)
    {
        Stage = newStage;
        // N'afficher que le visuel de l'étape actuelle.
        if (seedVisual != null) seedVisual.SetActive(newStage == GrowthStage.Seed);
        if (ripeVisual != null) ripeVisual.SetActive(newStage == GrowthStage.Ripe);
        if (overripeVisual != null) overripeVisual.SetActive(newStage == GrowthStage.Overripe);
    }

    private void Burst()
    {
        // Plus tard (jalon 4) : son + particules d'éclatement ici.
        Destroy(gameObject); // disparaît de la scène ET de la Hierarchy
    }
}
