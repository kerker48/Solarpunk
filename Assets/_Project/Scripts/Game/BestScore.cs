using UnityEngine;

/// <summary>
/// Sauvegarde du meilleur score, qui survit à la fermeture du jeu.
/// Responsabilité unique : lire et écrire le record dans les PlayerPrefs
/// (stockage persistant de Unity : registre Windows, ou stockage du navigateur pour une build Web).
/// Classe « static » : pas besoin de la poser sur un objet, on l'appelle de partout.
/// </summary>
public static class BestScore
{
    private const string Key = "PotagerSolarpunk_MeilleurScore";

    /// <summary>Le meilleur score enregistré (0 au tout premier lancement, sans erreur).</summary>
    public static int Get()
    {
        return PlayerPrefs.GetInt(Key, 0); // 0 = valeur par défaut si la clé n'existe pas encore
    }

    /// <summary>
    /// Propose un nouveau score. Il n'est enregistré QUE s'il bat le record :
    /// le meilleur score ne baisse jamais. Renvoie vrai si c'est un nouveau record.
    /// </summary>
    public static bool TrySubmit(int score)
    {
        if (score <= Get()) return false;
        PlayerPrefs.SetInt(Key, score);
        PlayerPrefs.Save(); // écrire tout de suite sur le disque
        return true;
    }
}
