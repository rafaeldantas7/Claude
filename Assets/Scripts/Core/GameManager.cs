using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Gerenciador central do jogo "Guardião das Águas" - Cagepa
/// Controla estado global: pausar, game over, vitória e transições de cena.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Estado do jogo ──────────────────────────────────────────────────────
    public enum GameState { MainMenu, Playing, Paused, GameOver, Victory }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    // ── Pontuação & Nível ───────────────────────────────────────────────────
    public int Score { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public int MaxLevels => 5;

    // ── Referências ─────────────────────────────────────────────────────────
    [Header("Referências")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private AudioManager audioManager;

    // ── Eventos ─────────────────────────────────────────────────────────────
    public static event System.Action<int> OnScoreChanged;
    public static event System.Action<GameState> OnGameStateChanged;

    // ── Constantes de pontuação ─────────────────────────────────────────────
    public const int PONTOS_VAZAMENTO_CONSERTADO = 100;
    public const int PONTOS_CIDADAO_EDUCADO      = 50;
    public const int PONTOS_BONUS_NIVEL          = 500;
    public const int BONUS_TEMPO_RESTANTE        = 10; // por segundo restante

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        MudarEstado(GameState.MainMenu);
    }

    // ── Controle de estado ──────────────────────────────────────────────────

    public void IniciarJogo()
    {
        Score = 0;
        CurrentLevel = 1;
        OnScoreChanged?.Invoke(Score);
        MudarEstado(GameState.Playing);
        SceneManager.LoadScene("Fase_01");
    }

    public void PausarJogo()
    {
        if (CurrentState != GameState.Playing) return;
        Time.timeScale = 0f;
        MudarEstado(GameState.Paused);
        audioManager?.PausarMusica();
    }

    public void RetomarJogo()
    {
        if (CurrentState != GameState.Paused) return;
        Time.timeScale = 1f;
        MudarEstado(GameState.Playing);
        audioManager?.RetomarMusica();
    }

    public void GameOver(string motivo = "A cidade ficou sem água!")
    {
        Time.timeScale = 0f;
        MudarEstado(GameState.GameOver);
        audioManager?.TocarEfeito("game_over");
        uiManager?.MostrarGameOver(motivo, Score);
    }

    public void Vitoria()
    {
        MudarEstado(GameState.Victory);
        audioManager?.TocarEfeito("vitoria");

        int bonusTempo = levelManager != null
            ? Mathf.RoundToInt(levelManager.TempoRestante * BONUS_TEMPO_RESTANTE)
            : 0;
        AdicionarPontos(PONTOS_BONUS_NIVEL + bonusTempo);

        uiManager?.MostrarVitoria(Score);
    }

    public void ProximaFase()
    {
        CurrentLevel++;
        if (CurrentLevel > MaxLevels)
        {
            MostrarCreditos();
            return;
        }
        Time.timeScale = 1f;
        MudarEstado(GameState.Playing);
        SceneManager.LoadScene($"Fase_0{CurrentLevel}");
    }

    public void ReiniciarFase()
    {
        Time.timeScale = 1f;
        MudarEstado(GameState.Playing);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VoltarMenuPrincipal()
    {
        Time.timeScale = 1f;
        MudarEstado(GameState.MainMenu);
        SceneManager.LoadScene("MenuPrincipal");
    }

    private void MostrarCreditos()
    {
        SceneManager.LoadScene("Creditos");
    }

    private void MudarEstado(GameState novoEstado)
    {
        CurrentState = novoEstado;
        OnGameStateChanged?.Invoke(novoEstado);
    }

    // ── Pontuação ───────────────────────────────────────────────────────────

    public void AdicionarPontos(int pontos)
    {
        Score += pontos;
        OnScoreChanged?.Invoke(Score);
        uiManager?.AtualizarPontuacao(Score);
    }

    // ── Utilitário ──────────────────────────────────────────────────────────

    public bool EstaJogando() => CurrentState == GameState.Playing;

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && CurrentState == GameState.Playing)
            PausarJogo();
    }
}
