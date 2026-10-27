using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permet au drone de planter une graine juste sous lui.
/// Responsabilité unique : quand on appuie sur « planter », vérifier qu'il y a de la terre
/// sous le drone et assez de place, puis faire apparaître une plante (Prefab).
/// </summary>
public class SeedPlanter : MonoBehaviour
{
    [Header("Commandes")]
    [Tooltip("Action 'Plant' : clic gauche / gâchette droite (RT).")]
    [SerializeField] private InputActionReference plantAction;

    [Header("Plantation")]
    [Tooltip("Prefab de la plante à faire apparaître.")]
    [SerializeField] private Plant plantPrefab;

    [Tooltip("Distance minimale entre deux plantes (en unités).")]
    [SerializeField] private float minSpacing = 1.2f;

    [Tooltip("Longueur du rayon lancé vers le bas pour trouver le sol.")]
    [SerializeField] private float rayLength = 20f;

    [Tooltip("Le ScoreManager de la scène : il sera transmis à chaque plante à sa naissance.")]
    [SerializeField] private ScoreManager scoreManager;

    [Tooltip("Objet de la Hierarchy qui servira de parent aux plantes (pour garder la Hierarchy rangée). Optionnel.")]
    [SerializeField] private Transform plantsParent;

    // Toutes les plantes actuellement dans le potager.
    // Une variable simple ne garderait que la dernière : il faut une List.
    private readonly List<Plant> plantedPlants = new List<Plant>();

    private void OnEnable()
    {
        if (plantAction == null) return;
        plantAction.action.performed += OnPlantPerformed; // s'abonner : « préviens-moi quand on appuie »
        plantAction.action.Enable();
    }

    private void OnDisable()
    {
        if (plantAction == null) return;
        plantAction.action.performed -= OnPlantPerformed; // se désabonner, sinon appel sur un objet détruit
        plantAction.action.Disable();
    }

    private void OnPlantPerformed(InputAction.CallbackContext context)
    {
        TryPlant();
    }

    private void TryPlant()
    {
        if (plantPrefab == null)
        {
            Debug.LogWarning("SeedPlanter : aucun Prefab de plante assigné dans l'Inspector.");
            return;
        }

        // 1. Lancer un rayon tout droit vers le BAS depuis le drone : la graine naîtra
        //    là où il touche le sol, donc exactement sous le drone (pas devant, pas à côté).
        //    QueryTriggerInteraction.Ignore : on ignore les zones invisibles (triggers) des plantes.
        Ray ray = new Ray(transform.position, Vector3.down);
        if (!Physics.Raycast(ray, out RaycastHit hit, rayLength, ~0, QueryTriggerInteraction.Ignore))
        {
            return; // rien sous le drone
        }

        // 2. Le sol touché est-il une parcelle de terre, et reste-t-il de la place dessus ?
        SoilPlot plot = hit.collider.GetComponentInParent<SoilPlot>();
        if (plot == null)
        {
            return; // herbe, chemin… on ne plante pas ici
        }
        if (!plot.HasRoom)
        {
            return; // parcelle pleine
        }

        // 3. Y a-t-il assez de place ? On retire d'abord de la liste les plantes
        //    déjà détruites (récoltées ou éclatées) : elles valent null.
        plantedPlants.RemoveAll(plant => plant == null);
        foreach (Plant plant in plantedPlants)
        {
            Vector3 offset = plant.transform.position - hit.point;
            offset.y = 0f; // on compare seulement à l'horizontale
            if (offset.magnitude < minSpacing)
            {
                return; // trop près d'une autre plante
            }
        }

        // 4. Faire naître la plante et la mémoriser.
        Plant newPlant = Instantiate(plantPrefab, hit.point, Quaternion.identity, plantsParent);
        newPlant.Initialize(scoreManager); // injection de dépendance : la plante sait à qui donner ses points
        plantedPlants.Add(newPlant);
        plot.Register(newPlant);
    }

    // Affiche le rayon de plantation dans la vue Scene quand le drone est sélectionné.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.55f, 0.35f, 0.15f); // marron terre
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * rayLength);
    }
}
