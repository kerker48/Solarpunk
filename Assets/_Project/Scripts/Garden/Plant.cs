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
    [Header("Identité")]
    [Tooltip("Nom affiché à l'écran quand on sélectionne cette plante.")]
    [SerializeField] private string displayName = "Blé";

    [Tooltip("Icône affichée à l'écran (optionnelle).")]
    [SerializeField] private Sprite icon;

    [Header("Place dans le potager")]
    [Tooltip("Nombre maximum de plantes de ce type sur une même parcelle.")]
    [SerializeField, Min(1)] private int maxPerPlot = 15;

    [Tooltip("Distance minimale entre cette plante et ses voisines (en unités).")]
    [SerializeField, Min(0.1f)] private float minSpacing = 0.9f;

    /// <summary>Nombre maximum de plantes sur une parcelle (utilisé par SoilPlot).</summary>
    public int MaxPerPlot => maxPerPlot;

    /// <summary>Espace minimum autour de cette plante (utilisé par SeedPlanter).</summary>
    public float MinSpacing => minSpacing;

    /// <summary>Nom affiché dans l'interface.</summary>
    public string DisplayName => displayName;

    /// <summary>Icône affichée dans l'interface (peut être vide).</summary>
    public Sprite Icon => icon;

    /// <summary>Points rapportés à la récolte (affichés dans le sélecteur).</summary>
    public int HarvestPoints => harvestPoints;

    [Header("Visuels de chaque étape (enfants du Prefab)")]
    [Tooltip("Visuels de la pousse, dans l'ordre (ex. Wheat_1, Wheat_2, Wheat_3). " +
             "Ils s'affichent l'un après l'autre pendant l'étape graine : un seul visible à la fois.")]
    [SerializeField] private GameObject[] growingVisuals;

    [Tooltip("Objet affiché quand le légume est mûr (ex. Wheat_4).")]
    [SerializeField] private GameObject ripeVisual;

    [Tooltip("Objet affiché quand le légume est trop mûr (ex. Wheat_4 avec un matériau fané).")]
    [SerializeField] private GameObject overripeVisual;

    [Tooltip("Objet qui peut grossir avec le temps (le parent des visuels). Optionnel.")]
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
    [Tooltip("Faire aussi grossir la plante en continu ? Décoché = seuls les changements de modèle montrent la pousse (plus réaliste avec des modèles par stade).")]
    [SerializeField] private bool animateScale = false;

    [Tooltip("Taille à la plantation (si Animate Scale est coché).")]
    [SerializeField] private float startScale = 0.6f;

    [Tooltip("Taille quand le légume devient mûr (si Animate Scale est coché).")]
    [SerializeField] private float ripeScale = 1f;

    [Tooltip("Taille juste avant l'éclatement (si Animate Scale est coché).")]
    [SerializeField] private float overripeScale = 1.1f;

    [Header("Récolte")]
    [Tooltip("Points gagnés quand on récolte ce légume mûr.")]
    [SerializeField, Min(1)] private int harvestPoints = 1;

    [Header("Aspiration à la récolte")]
    [Tooltip("Durée de l'aspiration du légume vers le drone (en secondes).")]
    [SerializeField, Min(0.05f)] private float suctionDuration = 0.6f;

    [Tooltip("Vitesse de rotation du légume pendant qu'il est aspiré (degrés par seconde).")]
    [SerializeField] private float suctionSpinSpeed = 720f;

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

    // Qui tient le score ? La plante est créée PENDANT la partie : elle ne peut pas avoir
    // cette référence glissée dans l'Inspector. C'est le planteur qui la lui donne à sa naissance
    // (injection de dépendance, voir Initialize).
    private ScoreManager scoreManager;

    // Empêche une double récolte si plusieurs détections arrivent dans la même image.
    private bool isHarvested;

    /// <summary>
    /// Appelée par SeedPlanter juste après Instantiate : on « injecte » le ScoreManager.
    /// </summary>
    public void Initialize(ScoreManager scoreManager)
    {
        this.scoreManager = scoreManager;
    }

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
        float midScale = Mathf.Lerp(ripeScale, overripeScale, 0.5f);

        SetStage(GrowthStage.Seed);
        yield return Grow(seedDuration * variation, startScale, ripeScale, true);

        SetStage(GrowthStage.Ripe);
        yield return Grow(ripeDuration * variation, ripeScale, midScale, false);

        SetStage(GrowthStage.Overripe);
        yield return Grow(overripeDuration * variation, midScale, overripeScale, false);

        Burst();
    }

    /// <summary>
    /// Attend 'duration' secondes de croissance. Le temps avance de Time.deltaTime × GrowthSpeed
    /// à chaque image : arroser = pousser plus vite.
    /// Pendant l'étape graine, on passe d'un visuel de pousse au suivant selon l'avancement.
    /// </summary>
    private IEnumerator Grow(float duration, float fromScale, float toScale, bool showGrowingVisuals)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * GrowthSpeed;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (showGrowingVisuals)
            {
                ShowGrowingVisual(progress);
            }

            if (animateScale && growthRoot != null)
            {
                growthRoot.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, progress);
            }
            yield return null; // attendre l'image suivante, sans bloquer le reste du jeu
        }
    }

    /// <summary>
    /// Affiche UN seul visuel de pousse selon l'avancement (0 → premier, 1 → dernier).
    /// Avec 3 visuels : 0–33 % = pousse 1, 33–66 % = pousse 2, 66–100 % = pousse 3.
    /// </summary>
    private void ShowGrowingVisual(float progress)
    {
        if (growingVisuals == null || growingVisuals.Length == 0) return;
        int index = Mathf.Min((int)(progress * growingVisuals.Length), growingVisuals.Length - 1);
        for (int i = 0; i < growingVisuals.Length; i++)
        {
            if (growingVisuals[i] != null) growingVisuals[i].SetActive(i == index);
        }
    }

    private void SetStage(GrowthStage newStage)
    {
        Stage = newStage;

        // Pousse : on commence par le premier visuel ; sinon on les cache tous.
        if (newStage == GrowthStage.Seed) ShowGrowingVisual(0f);
        else if (growingVisuals != null)
        {
            foreach (GameObject visual in growingVisuals)
            {
                if (visual != null) visual.SetActive(false);
            }
        }

        // N'afficher que le visuel de l'étape actuelle.
        if (ripeVisual != null) ripeVisual.SetActive(newStage == GrowthStage.Ripe);
        if (overripeVisual != null) overripeVisual.SetActive(newStage == GrowthStage.Overripe);
    }

    // OnTriggerStay (et pas seulement Enter) : si le drone est DÉJÀ au-dessus quand le blé
    // devient mûr, il est récolté quand même. On ne garde aucune référence vers le drone
    // en dehors de la récolte : aucune erreur si la plante éclate pendant le survol.
    private void OnTriggerStay(Collider other)
    {
        if (isHarvested || !IsRipe) return;                     // seul un légume mûr se récolte
        DroneController drone = other.GetComponentInParent<DroneController>();
        if (drone == null) return;                              // ce n'est pas le drone
        Harvest(drone.transform);
    }

    private void Harvest(Transform drone)
    {
        isHarvested = true;

        // Les points sont donnés TOUT DE SUITE : le score monte sans délai (brique 3.2),
        // l'aspiration n'est que du spectacle.
        if (scoreManager != null) // Null check : plante posée à la main dans la scène, sans planteur
        {
            scoreManager.AddPoints(harvestPoints);
        }

        // Arrêter la croissance (sinon la plante pourrait encore changer d'étape ou éclater),
        // puis lancer l'aspiration.
        StopAllCoroutines();
        StartCoroutine(SuckIntoDrone(drone));
    }

    /// <summary>
    /// Le légume monte vers le drone en tournant et en rétrécissant, puis disparaît.
    /// </summary>
    private IEnumerator SuckIntoDrone(Transform drone)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < suctionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / suctionDuration);
            float eased = t * t; // démarre doucement puis accélère : effet « aspiré »

            // Le drone peut bouger pendant l'aspiration : on vise sa position actuelle.
            // S'il n'existe plus (Null check), on vise simplement vers le haut.
            Vector3 target = drone != null ? drone.position : startPosition + Vector3.up * 5f;

            transform.position = Vector3.Lerp(startPosition, target, eased);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);
            transform.Rotate(0f, suctionSpinSpeed * Time.deltaTime, 0f, Space.World);

            yield return null;
        }

        // Plus tard (jalon 4) : son + particules de récolte ici.
        Destroy(gameObject); // disparaît de la scène ET de la Hierarchy
    }

    private void Burst()
    {
        // Plus tard (jalon 4) : son + particules d'éclatement ici.
        Destroy(gameObject); // disparaît de la scène ET de la Hierarchy
    }
}
