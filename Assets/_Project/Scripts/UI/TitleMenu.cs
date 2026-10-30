using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Écran titre : lancer la partie, voir le meilleur score, quitter.
/// Responsabilité unique : gérer les boutons de l'écran titre.
/// </summary>
public class TitleMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";

    [Tooltip("Texte qui affiche le meilleur score.")]
    [SerializeField] private TMP_Text bestScoreText;

    [SerializeField] private string bestScoreFormat = "Meilleur score : {0}";

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None; // le curseur doit être utilisable dans le menu
        Cursor.visible = true;
        if (bestScoreText != null) bestScoreText.text = string.Format(bestScoreFormat, BestScore.Get());
    }

    // ----- Boutons -----
    public void Play() => SceneManager.LoadScene(gameSceneName);

    public void Quit()
    {
        Application.Quit(); // ferme la build (sans effet dans l'éditeur ni sur une page Web)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // dans l'éditeur : arrête le Play Mode
#endif
    }
}
