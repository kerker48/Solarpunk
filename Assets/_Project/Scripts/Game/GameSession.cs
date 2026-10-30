using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Déroulement d'une partie : chronomètre, objectifs, et passage à l'écran de fin.
/// Responsabilité unique : savoir quand la partie est gagnée ou terminée, et la conclure.
/// </summary>
public class GameSession : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private ScoreManager scoreManager;

    [Tooltip("Sert à connaître toutes les plantes dont il faut atteindre l'objectif de récolte.")]
    [SerializeField] private PlantSelector plantSelector;

    [Header("Objectifs (les DEUX doivent être atteints pour gagner)")]
    [Tooltip("Score à atteindre.")]
    [SerializeField, Min(1)] private int targetScore = 200;

    [Tooltip("Nombre de récoltes à atteindre pour CHAQUE type de légume.")]
    [SerializeField, Min(1)] private int targetPerPlant = 35;

    [Header("Temps")]
    [Tooltip("Durée maximale de la partie (secondes). 300 = 5 minutes.")]
    [SerializeField, Min(10f)] private float timeLimit = 300f;

    [Header("Fin de partie")]
    [Tooltip("Nom exact de la scène de fin (telle qu'elle apparaît dans la liste des scènes de la build).")]
    [SerializeField] private string endSceneName = "Fin";

    [Tooltip("Petite pause avant d'afficher l'écran de fin (secondes).")]
    [SerializeField, Min(0f)] private float delayBeforeEndScreen = 1.5f;

    private float timeRemaining;
    private bool isOver;

    public int TargetScore => targetScore;
    public int TargetPerPlant => targetPerPlant;
    public float TimeRemaining => Mathf.Max(0f, timeRemaining);
    public bool IsOver => isOver;

    /// <summary>Déclenché quand la partie se termine (vrai = gagnée).</summary>
    public event Action<bool> GameEnded;

    private void Awake()
    {
        Time.timeScale = 1f; // au cas où on revient d'un menu pause
        timeRemaining = timeLimit;
    }

    private void OnEnable()
    {
        if (scoreManager == null) return;
        scoreManager.ScoreChanged += OnScoreChanged;
        scoreManager.HarvestsChanged += CheckVictory;
    }

    private void OnDisable()
    {
        if (scoreManager == null) return;
        scoreManager.ScoreChanged -= OnScoreChanged;
        scoreManager.HarvestsChanged -= CheckVictory;
    }

    private void Update()
    {
        if (isOver) return;

        // Time.deltaTime vaut 0 quand le jeu est en pause (Time.timeScale = 0) : le chrono s'arrête tout seul.
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            EndGame(false); // temps écoulé
        }
    }

    private void OnScoreChanged(int score) => CheckVictory();

    /// <summary>Progression de la récolte d'une plante (pour l'interface).</summary>
    public int GetHarvested(Plant plant)
    {
        return (plant != null && scoreManager != null) ? scoreManager.GetHarvestCount(plant.DisplayName) : 0;
    }

    private void CheckVictory()
    {
        if (isOver || scoreManager == null) return;
        if (scoreManager.Score < targetScore) return;

        // Chaque type de légume doit avoir atteint son objectif.
        if (plantSelector != null)
        {
            for (int i = 0; i < plantSelector.Count; i++)
            {
                Plant plant = plantSelector.GetPlant(i);
                if (plant != null && GetHarvested(plant) < targetPerPlant) return;
            }
        }

        EndGame(true);
    }

    private void EndGame(bool won)
    {
        if (isOver) return;
        isOver = true;

        int finalScore = scoreManager != null ? scoreManager.Score : 0;
        GameResults.LastScore = finalScore;
        GameResults.Won = won;
        GameResults.TimePlayed = timeLimit - TimeRemaining;
        GameResults.IsNewRecord = BestScore.TrySubmit(finalScore); // sauvegarde (le record ne baisse jamais)

        GameEnded?.Invoke(won);
        StartCoroutine(LoadEndScreen());
    }

    private IEnumerator LoadEndScreen()
    {
        // WaitForSecondsRealtime : fonctionne même si le temps du jeu est arrêté.
        yield return new WaitForSecondsRealtime(delayBeforeEndScreen);
        SceneManager.LoadScene(endSceneName);
    }
}
