using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Déplace le drone au-dessus du potager.
/// Responsabilité unique : lire les commandes (clavier OU manette, via l'Input System),
/// déplacer le drone à l'horizontale selon la caméra, gérer son altitude,
/// et l'empêcher de sortir du potager ou de descendre trop bas.
/// </summary>
public class DroneController : MonoBehaviour
{
    [Header("Commandes")]
    [Tooltip("Action 'Move' (Vector2) : ZQSD / flèches / stick gauche.")]
    [SerializeField] private InputActionReference moveAction;

    [Tooltip("Action 'Altitude' (Axis) : Espace monte, Ctrl descend / RB monte, LB descend.")]
    [SerializeField] private InputActionReference altitudeAction;

    [Tooltip("Caméra orbitale : sert à orienter le déplacement selon l'écran. Si vide, on se déplace selon les axes du monde.")]
    [SerializeField] private CameraOrbit cameraOrbit;

    [Header("Déplacement horizontal")]
    [Tooltip("Vitesse du drone en unités par seconde.")]
    [SerializeField] private float speed = 5f;

    [Header("Altitude")]
    [Tooltip("Vitesse de montée / descente en unités par seconde.")]
    [SerializeField] private float verticalSpeed = 3f;

    [Tooltip("Hauteur au lancement du jeu.")]
    [SerializeField] private float startHeight = 3f;

    [Tooltip("Hauteur minimale au-dessus du sol (le drone ne descend jamais plus bas).")]
    [SerializeField] private float minHeight = 1f;

    [Tooltip("Hauteur maximale au-dessus du sol.")]
    [SerializeField] private float maxHeight = 8f;

    [Tooltip("Hauteur (Y) de la surface du sol dans le monde.")]
    [SerializeField] private float groundLevel = 0f;

    [Header("Limites du potager (axes X et Z)")]
    [Tooltip("Coin minimum du terrain (X, Z).")]
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -10f);

    [Tooltip("Coin maximum du terrain (X, Z).")]
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 10f);

    private float currentHeight; // hauteur actuelle au-dessus du sol

    private void Awake()
    {
        currentHeight = startHeight;
    }

    // Les actions de l'Input System doivent être activées / désactivées
    // en suivant le cycle de vie du composant.
    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (altitudeAction != null) altitudeAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (altitudeAction != null) altitudeAction.action.Disable();
    }

    private void Update()
    {
        MoveHorizontally();
        ChangeAltitude();
        ApplyLimits();
    }

    private void MoveHorizontally()
    {
        if (moveAction == null) return; // Null check : référence oubliée dans l'Inspector

        // Lire la direction : (x = gauche/droite, y = haut/bas) entre -1 et 1.
        // Le même code marche pour ZQSD, les flèches et le stick : c'est l'Input System qui unifie.
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y);

        // Tourner cette direction du même angle que la caméra :
        // « haut » reste le haut de l'écran, même quand la caméra a tourné.
        if (cameraOrbit != null)
        {
            direction = Quaternion.Euler(0f, cameraOrbit.Yaw, 0f) * direction;
        }

        // Time.deltaTime : vitesse identique quel que soit le nombre d'images par seconde.
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void ChangeAltitude()
    {
        if (altitudeAction == null) return;

        // +1 = monter, -1 = descendre, 0 = rien.
        float verticalInput = altitudeAction.action.ReadValue<float>();
        currentHeight += verticalInput * verticalSpeed * Time.deltaTime;

        // Contrainte : jamais à moins de minHeight du sol, ni au-dessus de maxHeight.
        currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
    }

    private void ApplyLimits()
    {
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.z = Mathf.Clamp(position.z, minBounds.y, maxBounds.y);
        position.y = groundLevel + currentHeight;
        transform.position = position;
    }

    // Dessine les limites du potager dans la vue Scene (aide visuelle, invisible en jeu).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, groundLevel + minHeight, (minBounds.y + maxBounds.y) / 2f);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, 0.05f, maxBounds.y - minBounds.y);
        Gizmos.DrawWireCube(center, size);   // plancher : hauteur minimale
        center.y = groundLevel + maxHeight;
        Gizmos.DrawWireCube(center, size);   // plafond : hauteur maximale
    }
}
