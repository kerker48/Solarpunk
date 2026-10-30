using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Écran de fin : résultat de la partie, meilleur score, rejouer ou revenir au menu.
/// Responsabilité unique : afficher GameResults et gérer les boutons de fin.
/// </summary>
public class EndMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string titleSceneName = "MenuPrincipal";

    [Header("Textes")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestScoreText;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (titleText != null)
            titleText.text = GameResults.Won ? "Potager réussi !" : "Temps écoulé !";

        if (scoreText != null)
        {
            int seconds = Mathf.RoundToInt(GameResults.TimePlayed);
            scoreText.text = $"Score : {GameResults.LastScore}\n<size=70%>Temps : {seconds / 60:00}:{seconds % 60:00}</size>";
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = GameResults.IsNewRecord
                ? $"<color=#F2C94C>Nouveau record : {BestScore.Get()} !</color>"
                : $"Meilleur score : {BestScore.Get()}";
        }
    }

    // ----- Boutons -----
    public void Replay() => SceneManager.LoadScene(gameSceneName);
    public void BackToTitle() => SceneManager.LoadScene(titleSceneName);
}
