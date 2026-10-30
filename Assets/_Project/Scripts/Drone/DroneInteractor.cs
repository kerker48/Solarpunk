using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Gère les interactions du drone avec les bacs de dépôt, le réservoir d'eau et les bacs à graines.
/// Responsabilité unique : savoir au-dessus de quoi le drone se trouve (OnTriggerEnter / Exit)
/// et, quand on appuie sur « relâcher » (R / bouton A), déposer ou recharger.
/// </summary>
public class DroneInteractor : MonoBehaviour
{
    [Header("Commandes")]
    [Tooltip("Action 'Release' : R / bouton A de la manette.")]
    [SerializeField] private InputActionReference releaseAction;

    [Header("Références (sur le drone)")]
    [SerializeField] private DroneCargo cargo;
    [SerializeField] private WaterSprayer waterSprayer;
    [SerializeField] private DroneSeeds seeds;

    // Ce que le drone survole en ce moment (null = rien).
    // OnTriggerEnter les remplit, OnTriggerExit les vide : les deux vont toujours ensemble.
    private DepositBin binBelow;
    private WaterRefillStation stationBelow;
    private SeedBin seedBinBelow;

    /// <summary>Le bac survolé (pour l'affichage), ou null.</summary>
    public DepositBin BinBelow => binBelow;

    /// <summary>Vrai si le drone survole le réservoir d'eau.</summary>
    public bool IsOverWaterStation => stationBelow != null;

    /// <summary>Le bac à graines survolé (pour l'affichage), ou null.</summary>
    public SeedBin SeedBinBelow => seedBinBelow;

    private void OnEnable()
    {
        if (releaseAction == null) return;
        releaseAction.action.performed += OnReleasePerformed;
        releaseAction.action.Enable();
    }

    private void OnDisable()
    {
        if (releaseAction == null) return;
        releaseAction.action.performed -= OnReleasePerformed;
        releaseAction.action.Disable();
    }

    private void OnTriggerEnter(Collider other)
    {
        DepositBin bin = other.GetComponentInParent<DepositBin>();
        if (bin != null) binBelow = bin;

        WaterRefillStation station = other.GetComponentInParent<WaterRefillStation>();
        if (station != null) stationBelow = station;

        SeedBin seedBin = other.GetComponentInParent<SeedBin>();
        if (seedBin != null) seedBinBelow = seedBin;
    }

    private void OnTriggerExit(Collider other)
    {
        // On ne vide la référence QUE si c'est bien l'objet qu'on quitte.
        DepositBin bin = other.GetComponentInParent<DepositBin>();
        if (bin != null && bin == binBelow) binBelow = null;

        WaterRefillStation station = other.GetComponentInParent<WaterRefillStation>();
        if (station != null && station == stationBelow) stationBelow = null;

        SeedBin seedBin = other.GetComponentInParent<SeedBin>();
        if (seedBin != null && seedBin == seedBinBelow) seedBinBelow = null;
    }

    [Header("Son")]
    [Tooltip("AudioSource en boucle (Loop coché) joué pendant la recharge d'EAU. Optionnel.")]
    [FormerlySerializedAs("refillLoopSource")] // garde le réglage déjà fait dans l'Inspector
    [SerializeField] private AudioSource waterRefillLoopSource;

    [Tooltip("AudioSource en boucle (Loop coché) joué pendant la recharge de GRAINES. Optionnel.")]
    [SerializeField] private AudioSource seedRefillLoopSource;

    [Header("Détection de secours")]
    [Tooltip("Rayon de la colonne de recherche sous le drone (utilisée si les triggers n'ont rien détecté).")]
    [SerializeField, Min(0.1f)] private float searchRadius = 1f;

    [Tooltip("Profondeur de la recherche sous le drone.")]
    [SerializeField, Min(0.5f)] private float searchDepth = 12f;

    [Tooltip("Afficher dans la Console ce qui se passe quand on appuie sur R (pratique pour déboguer).")]
    [SerializeField] private bool debugLogs = true;

    // Appui sur R au-dessus d'un bac : on dépose les légumes du bon type.
    private void OnReleasePerformed(InputAction.CallbackContext context)
    {
        DepositBin bin = binBelow != null ? binBelow : FindBelow<DepositBin>();
        if (bin == null)
        {
            if (debugLogs && FindStation() == null && FindSeedBin() == null)
                Debug.Log("DroneInteractor : R appuyé, mais aucun bac ni réservoir sous le drone.");
            return;
        }
        if (cargo == null)
        {
            Debug.LogWarning("DroneInteractor : le champ Cargo est vide dans l'Inspector.");
            return;
        }

        int deposited = bin.Deposit(cargo);
        if (debugLogs)
        {
            Debug.Log(deposited > 0
                ? $"DroneInteractor : {deposited} × {bin.AcceptedName} déposé(s)."
                : $"DroneInteractor : aucun(e) {bin.AcceptedName} dans la soute.");
        }
    }

    // R MAINTENU au-dessus du réservoir ou d'un bac à graines : remplissage petit à petit.
    private void Update()
    {
        bool refillingWater = false;
        bool refillingSeeds = false;

        if (releaseAction != null && releaseAction.action.IsPressed())
        {
            WaterRefillStation station = FindStation();
            if (station != null && waterSprayer != null)
            {
                waterSprayer.Refill(station.RefillPerSecond * Time.deltaTime);
                refillingWater = true;
            }

            SeedBin seedBin = FindSeedBin();
            if (seedBin != null && seeds != null)
            {
                seeds.Refill(seedBin.SeedPlant, seedBin.SeedsPerSecond * Time.deltaTime);
                refillingSeeds = true;
            }
        }

        // Sons de recharge : chacun n'est joué que pendant sa recharge.
        SetLoop(waterRefillLoopSource, refillingWater);
        SetLoop(seedRefillLoopSource, refillingSeeds);
    }

    /// <summary>Démarre ou arrête un son en boucle selon 'playing'.</summary>
    private static void SetLoop(AudioSource source, bool playing)
    {
        if (source == null) return;
        if (playing && !source.isPlaying) source.Play();
        else if (!playing && source.isPlaying) source.Stop();
    }

    private SeedBin FindSeedBin()
    {
        return seedBinBelow != null ? seedBinBelow : FindBelow<SeedBin>();
    }

    private WaterRefillStation FindStation()
    {
        return stationBelow != null ? stationBelow : FindBelow<WaterRefillStation>();
    }

    /// <summary>
    /// Cherche un composant T dans une colonne verticale sous le drone (secours si les triggers
    /// n'ont rien signalé, par exemple si le drone a démarré déjà au-dessus d'un bac).
    /// </summary>
    private T FindBelow<T>() where T : Component
    {
        Vector3 top = transform.position;
        Vector3 bottom = top + Vector3.down * searchDepth;
        Collider[] hits = Physics.OverlapCapsule(top, bottom, searchRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            T found = hit.GetComponentInParent<T>();
            if (found != null) return found;
        }
        return null;
    }
}
