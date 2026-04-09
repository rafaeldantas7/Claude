using UnityEngine;

/// <summary>
/// Atributos persistentes do personagem Guardião das Águas.
/// Mantido entre fases via DontDestroyOnLoad.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Atributos Base")]
    [SerializeField] private int nivelJogador      = 1;
    [SerializeField] private int experiencia       = 0;
    [SerializeField] private int experienciaProxNivel = 200;

    [Header("Multiplicadores (desbloqueados ao subir de nível)")]
    public float MultiplicadorVelocidade   { get; private set; } = 1f;
    public float MultiplicadorReparo       { get; private set; } = 1f;  // Reduz tempo de reparo
    public float MultiplicadorEducacao     { get; private set; } = 1f;  // Reduz tempo de educação
    public int   BonusPontosVazamento      { get; private set; } = 0;

    // ── Eventos ─────────────────────────────────────────────────────────────
    public static event System.Action<int> OnSubiuDeNivel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── XP e Nível ───────────────────────────────────────────────────────────

    public void GanharXP(int quantidade)
    {
        experiencia += quantidade;
        while (experiencia >= experienciaProxNivel)
        {
            experiencia -= experienciaProxNivel;
            SubirDeNivel();
        }
    }

    private void SubirDeNivel()
    {
        nivelJogador++;
        experienciaProxNivel = Mathf.RoundToInt(experienciaProxNivel * 1.5f);

        AplicarBeneficioNivel();
        OnSubiuDeNivel?.Invoke(nivelJogador);
        UIManager.Instance?.MostrarFeedback($"Nível {nivelJogador}! Novas habilidades desbloqueadas!");
        AudioManager.Instance?.TocarEfeito("level_up");
    }

    private void AplicarBeneficioNivel()
    {
        switch (nivelJogador)
        {
            case 2:
                MultiplicadorVelocidade = 1.2f;
                break;
            case 3:
                MultiplicadorReparo = 0.8f; // 20% mais rápido
                break;
            case 4:
                MultiplicadorEducacao = 0.7f;
                break;
            case 5:
                BonusPontosVazamento = 50;
                break;
            default:
                if (nivelJogador > 5)
                {
                    MultiplicadorVelocidade += 0.05f;
                    MultiplicadorReparo     = Mathf.Max(0.3f, MultiplicadorReparo - 0.05f);
                }
                break;
        }
    }

    // ── Getters ───────────────────────────────────────────────────────────────

    public int   Nivel            => nivelJogador;
    public int   Experiencia      => experiencia;
    public int   XPParaProxNivel  => experienciaProxNivel;
    public float ProgressoXP      => (float)experiencia / experienciaProxNivel;
}
