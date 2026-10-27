using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Caméra qui suit le drone et tourne autour de lui.
/// Responsabilité unique : placer la caméra autour d'une cible,
/// selon un angle horizontal (yaw) et une inclinaison (pitch) contrôlés
/// à la souris ou au stick droit de la manette.
/// </summary>
public class CameraOrbit : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("L'objet que la caméra suit et autour duquel elle tourne (le Drone).")]
    [SerializeField] private Transform target;

    [Header("Commandes")]
    [Tooltip("Action 'CameraOrbitMouse' : déplacement de la souris.")]
    [SerializeField] private InputActionReference mouseOrbitAction;

    [Tooltip("Action 'CameraOrbit' : stick droit de la manette.")]
    [SerializeField] private InputActionReference stickOrbitAction;

    [Tooltip("Cache et bloque le curseur au centre pendant le jeu (Échap pour le libérer dans l'éditeur).")]
    [SerializeField] private bool lockCursor = true;

    [Header("Sensibilité")]
    [Tooltip("Degrés tournés par pixel de déplacement de la souris.")]
    [SerializeField] private float mouseSensitivity = 0.15f;

    [Tooltip("Degrés par seconde quand le stick est poussé à fond.")]
    [SerializeField] private float stickSpeed = 120f;

    [Header("Inclinaison")]
    [Tooltip("Inclinaison au lancement : 90 = vue parfaitement du dessus.")]
    [SerializeField, Range(10f, 90f)] private float startPitch = 90f;

    [Tooltip("Inclinaison minimale (vue la plus rasante).")]
    [SerializeField, Range(10f, 90f)] private float minPitch = 30f;

    [Tooltip("Inclinaison maximale (90 = vue verticale).")]
    [SerializeField, Range(10f, 90f)] private float maxPitch = 90f;

    [Header("Distance")]
    [Tooltip("Distance entre la caméra et le drone.")]
    [SerializeField] private float distance = 17f;

    private float yaw;   // angle horizontal autour du drone (degrés)
    private float pitch; // inclinaison vers le bas (degrés)

    /// <summary>Angle horizontal actuel de la caméra : le drone s'en sert pour savoir où est « devant ».</summary>
    public float Yaw => yaw;

    private void Awake()
    {
        pitch = startPitch;
    }

    private void OnEnable()
    {
        if (mouseOrbitAction != null) mouseOrbitAction.action.Enable();
        if (stickOrbitAction != null) stickOrbitAction.action.Enable();
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (mouseOrbitAction != null) mouseOrbitAction.action.Disable();
        if (stickOrbitAction != null) stickOrbitAction.action.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // LateUpdate s'exécute APRÈS tous les Update : le drone a déjà bougé,
    // la caméra se place donc sur sa position finale de l'image (pas de tremblement).
    private void LateUpdate()
    {
        if (target == null) return; // Null check : cible oubliée dans l'Inspector

        // 1a. Souris : la valeur est un déplacement en pixels depuis la dernière image.
        //     Elle dépend déjà du temps écoulé, donc PAS de Time.deltaTime ici.
        if (mouseOrbitAction != null)
        {
            Vector2 mouseDelta = mouseOrbitAction.action.ReadValue<Vector2>();
            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
        }

        // 1b. Stick : la valeur est une position entre -1 et 1 (« à quel point il est poussé »).
        //     On la transforme en vitesse, donc AVEC Time.deltaTime.
        if (stickOrbitAction != null)
        {
            Vector2 stick = stickOrbitAction.action.ReadValue<Vector2>();
            yaw += stick.x * stickSpeed * Time.deltaTime;
            pitch -= stick.y * stickSpeed * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 2. Construire l'orientation de la caméra à partir des deux angles.
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // 3. Reculer la caméra de 'distance' dans la direction opposée à son regard :
        //    elle se retrouve en l'air, au-dessus du drone, et le regarde.
        transform.position = target.position + rotation * new Vector3(0f, 0f, -distance);
        transform.rotation = rotation;
    }
}
