using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Gerencia toda a interface do usuário: HUD, menus, alertas e feedbacks.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── HUD Principal ───────────────────────────────────────────────────────
    [Header("HUD - Água")]
    [SerializeField] private Slider barraAgua;
    [SerializeField] private Image  preenchimentoBarraAgua;
    [SerializeField] private TextMeshProUGUI textoPercentualAgua;
    [SerializeField] private Color corAguaNormal   = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color corAguaAlerta   = Color.yellow;
    [SerializeField] private Color corAguaCritica  = Color.red;

    [Header("HUD - Pontuação e Tempo")]
    [SerializeField] private TextMeshProUGUI textoPontuacao;
    [SerializeField] private TextMeshProUGUI textoTempo;
    [SerializeField] private TextMeshProUGUI textoNivel;
    [SerializeField] private TextMeshProUGUI textoVazamentos;

    [Header("HUD - Progresso de ação")]
    [SerializeField] private GameObject painelProgresso;
    [SerializeField] private Slider     barraProgresso;
    [SerializeField] private TextMeshProUGUI textoAcao;

    [Header("HUD - Dica de interação")]
    [SerializeField] private GameObject painelDica;
    [SerializeField] private TextMeshProUGUI textoDica;

    // ── Painéis de menu ─────────────────────────────────────────────────────
    [Header("Painéis")]
    [SerializeField] private GameObject painelGameOver;
    [SerializeField] private GameObject painelVitoria;
    [SerializeField] private GameObject painelPausa;
    [SerializeField] private GameObject painelMenuPrincipal;
    [SerializeField] private GameObject painelCreditos;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI textoMotivoGameOver;
    [SerializeField] private TextMeshProUGUI textoPontuacaoFinal;

    [Header("Vitória")]
    [SerializeField] private TextMeshProUGUI textoPontuacaoVitoria;
    [SerializeField] private GameObject[] estrelasVitoria;

    // ── Alertas e Feedback ──────────────────────────────────────────────────
    [Header("Alertas")]
    [SerializeField] private GameObject painelAlerta;
    [SerializeField] private TextMeshProUGUI textoAlerta;
    [SerializeField] private float duracaoAlerta = 3f;

    [Header("Feedback flutuante")]
    [SerializeField] private GameObject prefabFeedback;
    [SerializeField] private Transform  canvasFeedback;

    // ── Logo Cagepa ─────────────────────────────────────────────────────────
    [Header("Marca")]
    [SerializeField] private Image logoCagepa;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        CityWaterSystem.OnNivelAlterado += AtualizarBarraAgua;
        CityWaterSystem.OnNivelCritico  += () => PiscarBarraAgua(corAguaCritica);
        GameManager.OnScoreChanged      += AtualizarPontuacao;
    }

    void OnDisable()
    {
        CityWaterSystem.OnNivelAlterado -= AtualizarBarraAgua;
        GameManager.OnScoreChanged      -= AtualizarPontuacao;
    }

    // ── Barra de Água ────────────────────────────────────────────────────────

    public void AtualizarBarraAgua(float nivel)
    {
        if (barraAgua == null) return;

        float percentual = nivel / 100f;
        barraAgua.value = percentual;

        if (textoPercentualAgua != null)
            textoPercentualAgua.text = $"{Mathf.RoundToInt(nivel)}%";

        // Muda cor conforme nível
        if (preenchimentoBarraAgua != null)
        {
            if (percentual <= 0.2f)
                preenchimentoBarraAgua.color = corAguaCritica;
            else if (percentual <= 0.4f)
                preenchimentoBarraAgua.color = corAguaAlerta;
            else
                preenchimentoBarraAgua.color = corAguaNormal;
        }
    }

    private Coroutine coroutinePiscar;

    private void PiscarBarraAgua(Color cor)
    {
        if (coroutinePiscar != null) StopCoroutine(coroutinePiscar);
        coroutinePiscar = StartCoroutine(EfeitoPiscar(preenchimentoBarraAgua, cor));
    }

    private IEnumerator EfeitoPiscar(Graphic grafico, Color cor)
    {
        if (grafico == null) yield break;
        Color original = grafico.color;
        for (int i = 0; i < 6; i++)
        {
            grafico.color = cor;
            yield return new WaitForSecondsRealtime(0.2f);
            grafico.color = original;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    // ── Pontuação e HUD ──────────────────────────────────────────────────────

    public void AtualizarPontuacao(int pontos)
    {
        if (textoPontuacao != null)
            textoPontuacao.text = $"Pontos: {pontos:N0}";
    }

    public void AtualizarTempo(float segundosRestantes)
    {
        if (textoTempo == null) return;
        int min = Mathf.FloorToInt(segundosRestantes / 60f);
        int seg = Mathf.FloorToInt(segundosRestantes % 60f);
        textoTempo.text = $"{min:00}:{seg:00}";
        textoTempo.color = segundosRestantes < 30f ? Color.red : Color.white;
    }

    public void AtualizarContadorVazamentos(int total)
    {
        if (textoVazamentos != null)
            textoVazamentos.text = $"Vazamentos: {total}";
    }

    public void AtualizarNivel(int nivel)
    {
        if (textoNivel != null)
            textoNivel.text = $"Fase {nivel}";
    }

    // ── Barra de progresso de ação ───────────────────────────────────────────

    public void AtualizarBarraProgresso(float progresso, string descricao = "Trabalhando...")
    {
        if (painelProgresso == null) return;
        painelProgresso.SetActive(true);
        barraProgresso.value = progresso;
        if (textoAcao != null) textoAcao.text = descricao;
    }

    public void EsconderBarraProgresso()
    {
        painelProgresso?.SetActive(false);
    }

    // ── Dica de interação ────────────────────────────────────────────────────

    public void MostrarDica(string texto)
    {
        if (painelDica == null) return;
        painelDica.SetActive(true);
        if (textoDica != null) textoDica.text = texto;
    }

    public void EsconderDica() => painelDica?.SetActive(false);

    // ── Alertas ───────────────────────────────────────────────────────────────

    public void MostrarAlerta(string mensagem, Color cor)
    {
        if (painelAlerta == null) return;
        StopCoroutine(nameof(EsconderAlertaComDelay));
        painelAlerta.SetActive(true);
        if (textoAlerta != null)
        {
            textoAlerta.text  = mensagem;
            textoAlerta.color = cor;
        }
        StartCoroutine(EsconderAlertaComDelay());
    }

    private IEnumerator EsconderAlertaComDelay()
    {
        yield return new WaitForSeconds(duracaoAlerta);
        painelAlerta?.SetActive(false);
    }

    // ── Feedback flutuante ───────────────────────────────────────────────────

    public void MostrarFeedback(string texto, Vector3? posicao = null)
    {
        if (prefabFeedback == null) return;

        Transform pai = canvasFeedback != null ? canvasFeedback : transform;
        var obj = Instantiate(prefabFeedback, pai);

        var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = texto;

        Destroy(obj, 2f);
    }

    // ── Game Over / Vitória / Pausa ──────────────────────────────────────────

    public void MostrarGameOver(string motivo, int pontuacao)
    {
        painelGameOver?.SetActive(true);
        if (textoMotivoGameOver != null)  textoMotivoGameOver.text  = motivo;
        if (textoPontuacaoFinal != null)  textoPontuacaoFinal.text  = $"Pontuação: {pontuacao:N0}";
    }

    public void MostrarVitoria(int pontuacao)
    {
        painelVitoria?.SetActive(true);
        if (textoPontuacaoVitoria != null)
            textoPontuacaoVitoria.text = $"Pontuação: {pontuacao:N0}";

        AtivarEstrelas(pontuacao);
    }

    private void AtivarEstrelas(int pontuacao)
    {
        if (estrelasVitoria == null) return;
        int estrelas = pontuacao >= 2000 ? 3 : pontuacao >= 1000 ? 2 : 1;
        for (int i = 0; i < estrelasVitoria.Length; i++)
            estrelasVitoria[i]?.SetActive(i < estrelas);
    }

    public void TogglePausa()
    {
        if (painelPausa == null) return;
        bool pausado = !painelPausa.activeSelf;
        painelPausa.SetActive(pausado);
        if (pausado) GameManager.Instance?.PausarJogo();
        else         GameManager.Instance?.RetomarJogo();
    }

    // ── Botões de UI (chamados pelos botões no editor) ───────────────────────

    public void BotaoIniciar()        => GameManager.Instance?.IniciarJogo();
    public void BotaoReiniciar()      => GameManager.Instance?.ReiniciarFase();
    public void BotaoProximaFase()    => GameManager.Instance?.ProximaFase();
    public void BotaoMenuPrincipal()  => GameManager.Instance?.VoltarMenuPrincipal();
    public void BotaoPausar()         => TogglePausa();
}
