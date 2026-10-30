using UnityEngine;

/// <summary>
/// Effet de vol du drone : flottement de haut en bas et inclinaison dans le sens du déplacement.
/// Responsabilité unique : animer le VISUEL du drone (DroneVisuel) par le code.
/// Il ne touche jamais au parent « Drone » (déplacé par DroneController, porteur du collider) :
/// l'effet ne gêne donc ni les déplacements ni la détection.
/// </summary>
public class DroneHover : MonoBehaviour
{
    [Header("Flottement")]
    [Tooltip("Hauteur du flottement (en unités) : de combien le visuel monte et descend.")]
    [SerializeField, Min(0f)] private float bobHeight = 0.08f;

    [Tooltip("Durée d'un aller-retour complet (secondes).")]
    [SerializeField, Min(0.1f)] private float bobPeriod = 3f;

    [Header("Inclinaison en mouvement")]
    [Tooltip("Inclinaison en degrés par unité de vitesse (0 = pas d'inclinaison).")]
    [SerializeField, Min(0f)] private float tiltPerSpeed = 2.5f;

    [Tooltip("Inclinaison maximale (degrés).")]
    [SerializeField, Range(0f, 45f)] private float maxTilt = 15f;

    [Tooltip("Rapidité avec laquelle l'inclinaison suit le mouvement (plus grand = plus réactif).")]
    [SerializeField, Min(0.1f)] private float tiltSmoothing = 6f;

    private Vector3 basePosition;      // position locale de départ du visuel
    private Vector3 lastParentPosition;
    private Quaternion currentTilt = Quaternion.identity;

    private void Start()
    {
        basePosition = transform.localPosition;
        lastParentPosition = transform.parent != null ? transform.parent.position : transform.position;
    }

    private void LateUpdate()
    {
        // 1. Flottement : une sinusoïde est parfaitement fluide (elle ralentit toute seule en haut et en bas).
        float phase = Time.time * Mathf.PI * 2f / bobPeriod;
        transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(phase) * bobHeight);

        // 2. Vitesse horizontale du drone, calculée à partir du déplacement du parent depuis l'image précédente.
        if (transform.parent == null || Time.deltaTime <= 0f) return;
        Vector3 parentPosition = transform.parent.position;
        Vector3 velocity = (parentPosition - lastParentPosition) / Time.deltaTime;
        lastParentPosition = parentPosition;
        velocity.y = 0f;

        // 3. Inclinaison vers l'avant du mouvement (comme un vrai drone qui « pique » pour avancer).
        Vector3 tiltAngles = new Vector3(velocity.z, 0f, -velocity.x) * tiltPerSpeed;
        tiltAngles = Vector3.ClampMagnitude(tiltAngles, maxTilt);
        Quaternion targetTilt = Quaternion.Euler(tiltAngles);

        // Lissage : l'inclinaison rejoint sa cible progressivement, sans à-coup.
        currentTilt = Quaternion.Slerp(currentTilt, targetTilt, 1f - Mathf.Exp(-tiltSmoothing * Time.deltaTime));
        transform.localRotation = currentTilt;
    }
}
