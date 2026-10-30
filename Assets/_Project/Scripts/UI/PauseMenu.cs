using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu pause : met le jeu en pause et propose de reprendre, recommencer ou quitter.
/// Responsabilité unique : gérer l'état « en pause » (temps, commandes du joueur, curseur, panneau).
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Commandes")]
    [Tooltip("Action 'UI/Pause' : Échap ou P / bouton Start de la manette.")]
    [SerializeField] private InputActionReference pauseAction;

    [Tooltip("L'asset Input Actions : on désactive la map 'Player' pendant la pause pour que le drone ne réagisse plus.")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Interface")]
    [Tooltip("Le panneau du menu pause (désactivé au départ).")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Bouton sélectionné à l'ouverture (pour naviguer à la manette).")]
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Scènes")]
    [SerializeField] private string titleSceneName = "MenuPrincipal";

    [Tooltip("La GameSession : on ne peut plus mettre en pause une partie terminée.")]
    [SerializeField] private GameSession gameSession;

    private bool isPaused;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (pauseAction == null) return;
        pauseAction.action.performed += OnPausePerformed;
        pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        if (pauseAction != null) pauseAction.action.performed -= OnPausePerformed;
        if (isPaused) SetPaused(false); // ne jamais laisser le jeu figé si l'objet disparaît
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (gameSession != null && gameSession.IsOver) return;
        SetPaused(!isPaused);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;

        // 1. Le temps : 0 = tout ce qui utilise Time.deltaTime s'arrête (croissance, chrono, déplacements).
        Time.timeScale = paused ? 0f : 1f;

        // 2. Les commandes du drone : coupées pendant la pause (sinon on pourrait planter en pause).
        InputActionMap playerMap = inputActions != null ? inputActions.FindActionMap("Player") : null;
        if (playerMap != null)
        {
            if (paused) playerMap.Disable();
            else playerMap.Enable();
        }

        // 3. Le curseur : visible pour cliquer dans le menu, caché et bloqué en jeu.
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;

        // 4. Le panneau, et le bouton sélectionné par défaut pour la manette.
        if (pausePanel != null) pausePanel.SetActive(paused);
        if (paused && firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    // ----- Boutons (à relier dans l'Inspector : Button → On Click) -----

    public void Resume() => SetPaused(false);

    public void Restart()
    {
        Time.timeScale = 1f;
        // Recharger la scène remet TOUT à zéro : score, potager, eau, graines, chrono.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}
