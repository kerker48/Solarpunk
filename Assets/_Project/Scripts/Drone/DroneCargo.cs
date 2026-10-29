using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La soute du drone : les légumes récoltés y sont stockés jusqu'au dépôt dans un bac.
/// Responsabilité unique : compter ce que transporte le drone (par type de plante)
/// et refuser de nouvelles récoltes quand la soute est pleine.
/// </summary>
public class DroneCargo : MonoBehaviour
{
    [Tooltip("Nombre maximum de légumes transportés en même temps.")]
    [SerializeField, Min(1)] private int capacity = 6;

    // Contenu de la soute : clé = nom de la plante ("Blé"), valeur = quantité.
    private readonly Dictionary<string, int> content = new Dictionary<string, int>();

    /// <summary>Déclenché à chaque fois que le contenu change (l'interface s'y abonne).</summary>
    public event Action CargoChanged;

    public int Capacity => capacity;

    /// <summary>Nombre total de légumes dans la soute.</summary>
    public int Count
    {
        get
        {
            int total = 0;
            foreach (int amount in content.Values) total += amount;
            return total;
        }
    }

    /// <summary>Vrai s'il reste de la place pour au moins un légume.</summary>
    public bool HasRoom => Count < capacity;

    /// <summary>Quantité transportée pour un type de plante.</summary>
    public int GetCount(string plantName)
    {
        return content.TryGetValue(plantName, out int amount) ? amount : 0;
    }

    /// <summary>Ajoute un légume récolté. Renvoie false si la soute est pleine.</summary>
    public bool TryAdd(string plantName)
    {
        if (!HasRoom) return false;
        content[plantName] = GetCount(plantName) + 1;
        CargoChanged?.Invoke();
        return true;
    }

    /// <summary>Vide tous les légumes d'un type et renvoie combien ont été déposés.</summary>
    public int Unload(string plantName)
    {
        int amount = GetCount(plantName);
        if (amount == 0) return 0;
        content.Remove(plantName);
        CargoChanged?.Invoke();
        return amount;
    }

    private void Start()
    {
        CargoChanged?.Invoke(); // afficher la soute vide dès le départ
    }
}
