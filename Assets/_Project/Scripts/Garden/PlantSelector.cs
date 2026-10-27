using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Choix du type de plante à semer.
/// Responsabilité unique : garder la liste des plantes disponibles, savoir laquelle est
/// sélectionnée, changer de sélection avec les touches, et prévenir l'interface.
/// </summary>
public class PlantSelector : MonoBehaviour
{
    [Header("Commandes")]
    [Tooltip("Action 'NextPlant' : E / molette vers le haut / croix directionnelle droite.")]
    [SerializeField] private InputActionReference nextAction;

    [Tooltip("Action 'PreviousPlant' : A / molette vers le bas / croix directionnelle gauche.")]
    [SerializeField] private InputActionReference previousAction;

    [Header("Plantes disponibles")]
    [Tooltip("Les Prefabs de plantes, dans l'ordre où on les fait défiler.")]
    [SerializeField] private Plant[] plantTypes;

    private int currentIndex;

    /// <summary>La plante actuellement sélectionnée (null si la liste est vide).</summary>
    public Plant CurrentPlant =>
        plantTypes != null && plantTypes.Length > 0 ? plantTypes[currentIndex] : null;

    /// <summary>Position de la sélection dans la liste (0 = première).</summary>
    public int CurrentIndex => currentIndex;

    /// <summary>Nombre de plantes disponibles.</summary>
    public int Count => plantTypes != null ? plantTypes.Length : 0;

    /// <summary>Renvoie la plante à cette position de la liste (null si l'index est invalide).</summary>
    public Plant GetPlant(int index)
    {
        if (plantTypes == null || index < 0 || index >= plantTypes.Length) return null;
        return plantTypes[index];
    }

    /// <summary>Déclenché à chaque changement de sélection (l'interface s'y abonne).</summary>
    public event Action<Plant> SelectionChanged;

    private void OnEnable()
    {
        if (nextAction != null)
        {
            nextAction.action.performed += OnNext;
            nextAction.action.Enable();
        }
        if (previousAction != null)
        {
            previousAction.action.performed += OnPrevious;
            previousAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (nextAction != null)
        {
            nextAction.action.performed -= OnNext;
            nextAction.action.Disable();
        }
        if (previousAction != null)
        {
            previousAction.action.performed -= OnPrevious;
            previousAction.action.Disable();
        }
    }

    private void Start()
    {
        // Prévenir l'interface de la sélection de départ.
        SelectionChanged?.Invoke(CurrentPlant);
    }

    private void OnNext(InputAction.CallbackContext context) => Select(currentIndex + 1);
    private void OnPrevious(InputAction.CallbackContext context) => Select(currentIndex - 1);

    /// <summary>Sélectionne la plante à cet index, en bouclant (après la dernière, on revient à la première).</summary>
    public void Select(int index)
    {
        if (Count == 0) return;
        // Modulo « positif » : -1 devient Count - 1 (on revient à la fin de la liste).
        currentIndex = (index % Count + Count) % Count;
        SelectionChanged?.Invoke(CurrentPlant);
    }
}
