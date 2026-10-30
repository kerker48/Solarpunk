/// <summary>
/// Résultat de la dernière partie, transmis de la scène de jeu à l'écran de fin.
/// Responsabilité unique : garder ces quelques valeurs pendant le changement de scène.
/// (Les variables « static » ne sont pas détruites quand on charge une autre scène.)
/// </summary>
public static class GameResults
{
    /// <summary>Score final de la dernière partie.</summary>
    public static int LastScore;

    /// <summary>Vrai si les objectifs ont été atteints, faux si le temps est écoulé.</summary>
    public static bool Won;

    /// <summary>Temps joué, en secondes.</summary>
    public static float TimePlayed;

    /// <summary>Vrai si la dernière partie a battu le record.</summary>
    public static bool IsNewRecord;
}
