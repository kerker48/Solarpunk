using System.Collections;
using UnityEngine;

/// <summary>
/// Bac de dépôt pour UN type de légume.
/// Responsabilité unique : quand on lui demande, prendre dans la soute du drone
/// les légumes du bon type, donner le bonus de points, et jouer l'animation de dépôt.
/// Le bac a besoin d'un collider en Is Trigger (haut) pour que le drone le détecte en survol.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DepositBin : MonoBehaviour
{
    [Header("Règles")]
    [Tooltip("Le type de légume accepté par ce bac (Prefab de la plante).")]
    [SerializeField] private Plant acceptedPlant;

    [Tooltip("Points bonus pour chaque légume déposé.")]
    [SerializeField, Min(0)] private int bonusPerItem = 1;

    [Tooltip("Le ScoreManager de la scène (pour donner le bonus).")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Animation de dépôt")]
    [Tooltip("Modèle qui « tombe » du drone dans le bac pour chaque légume (ex. Wheat_Crop). Optionnel.")]
    [SerializeField] private GameObject flyingItemModel;

    [Tooltip("Visuel du bac qui rebondit à chaque légume reçu (ex. la charrette). Optionnel.")]
    [SerializeField] private Transform binVisual;

    [Tooltip("Durée du vol d'un légume, du drone jusqu'au bac (secondes).")]
    [SerializeField, Min(0.05f)] private float flightDuration = 0.5f;

    [Tooltip("Délai entre deux légumes qui tombent (secondes).")]
    [SerializeField, Min(0f)] private float delayBetweenItems = 0.12f;

    [Tooltip("Hauteur de la courbe du vol (au-dessus de la ligne droite).")]
    [SerializeField] private float arcHeight = 1.5f;

    [Tooltip("Taille du rebond du bac (0.15 = +15 %).")]
    [SerializeField, Range(0f, 0.5f)] private float bounceStrength = 0.15f;

    private Vector3 binVisualBaseScale;

    /// <summary>Nom du légume accepté (affiché dans l'interface).</summary>
    public string AcceptedName => acceptedPlant != null ? acceptedPlant.DisplayName : "?";

    private void Awake()
    {
        if (binVisual != null) binVisualBaseScale = binVisual.localScale;
    }

    /// <summary>
    /// Vide dans ce bac les légumes du bon type présents dans la soute.
    /// Renvoie le nombre de légumes déposés (0 si aucun du bon type).
    /// </summary>
    public int Deposit(DroneCargo cargo)
    {
        if (cargo == null || acceptedPlant == null) return 0; // Null checks

        int amount = cargo.Unload(acceptedPlant.DisplayName);
        if (amount <= 0) return 0;

        // Les points sont donnés tout de suite ; l'animation n'est que du spectacle.
        if (scoreManager != null)
        {
            scoreManager.AddPoints(amount * bonusPerItem);
        }

        StartCoroutine(PlayDepositAnimation(cargo.transform.position, amount));
        return amount;
    }

    /// <summary>Fait tomber les légumes un par un du drone dans le bac.</summary>
    private IEnumerator PlayDepositAnimation(Vector3 dronePosition, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (flyingItemModel != null)
            {
                StartCoroutine(FlyItem(dronePosition));
            }
            else
            {
                StartCoroutine(Bounce()); // pas de modèle : le bac rebondit quand même
            }
            yield return new WaitForSeconds(delayBetweenItems);
        }
    }

    /// <summary>Un légume suit une courbe du drone jusqu'au bac, puis le bac rebondit.</summary>
    private IEnumerator FlyItem(Vector3 start)
    {
        GameObject item = Instantiate(flyingItemModel, start, Random.rotation);
        Vector3 end = transform.position + Vector3.up * 0.8f;
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);
            // Ligne droite + une bosse (parabole 4t(1-t), qui vaut 0 au départ et à l'arrivée).
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += arcHeight * 4f * t * (1f - t);
            item.transform.position = position;
            item.transform.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }

        Destroy(item);
        StartCoroutine(Bounce());
        // Plus tard (jalon 4) : son de dépôt ici.
    }

    /// <summary>Petit rebond du visuel du bac : il gonfle puis revient à sa taille.</summary>
    private IEnumerator Bounce()
    {
        if (binVisual == null) yield break;
        const float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = 1f + bounceStrength * Mathf.Sin(t * Mathf.PI); // 0 → max → 0
            binVisual.localScale = binVisualBaseScale * scale;
            yield return null;
        }
        binVisual.localScale = binVisualBaseScale;
    }
}
