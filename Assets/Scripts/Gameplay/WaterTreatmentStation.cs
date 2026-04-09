using UnityEngine;
using System.Collections;

/// <summary>
/// Estação de Tratamento de Água da Cagepa.
/// O jogador pode ativá-la para aumentar a taxa de reposição do reservatório.
/// Possui estados: Desligada → Iniciando → Ativa → Sobrecarregada.
/// </summary>
public class WaterTreatmentStation : MonoBehaviour, IInteragivel
{
    // ── Estado da estação ────────────────────────────────────────────────────
    public enum EstadoEstacao
    {
        Desligada,
        Iniciando,
        Ativa,
        Sobrecarregada
    }

    // ── Configurações ───────────────────────────────────────────────────────
    [Header("Operação")]
    [SerializeField] private float tempoInicializacao  = 5f;
    [SerializeField] private float duracaoOperacao     = 60f;
    [SerializeField] private float tempoResfriamento   = 30f;
    [SerializeField] private float tempoSobrecarga     = 10f;

    [Header("Bônus de Tratamento")]
    [Tooltip("Litros extras de água adicionados ao ativar")]
    [SerializeField] private float aguaBonus = 15f;

    [Header("Visual")]
    [SerializeField] private Animator animadorEstacao;
    [SerializeField] private Light    luzIndicadora;
    [SerializeField] private Color    corDesligada     = Color.gray;
    [SerializeField] private Color    corIniciando     = Color.yellow;
    [SerializeField] private Color    corAtiva         = Color.green;
    [SerializeField] private Color    corSobrecarregada = Color.red;
    [SerializeField] private ParticleSystem particulasFumaca;
    [SerializeField] private ParticleSystem particulasVapor;

    [Header("UI")]
    [SerializeField] private GameObject painelStatus;
    [SerializeField] private TMPro.TextMeshProUGUI textoStatus;
    [SerializeField] private UnityEngine.UI.Slider barraOperacao;

    // ── Estado ──────────────────────────────────────────────────────────────
    public EstadoEstacao Estado { get; private set; } = EstadoEstacao.Desligada;
    public bool PodeInteragir() => Estado == EstadoEstacao.Desligada;
    public string ObterDescricao() => Estado switch
    {
        EstadoEstacao.Desligada      => "Estação desligada — Pressione E para ativar",
        EstadoEstacao.Iniciando      => "Estação iniciando...",
        EstadoEstacao.Ativa          => "Estação em operação!",
        EstadoEstacao.Sobrecarregada => "Estação sobrecarregada! Aguarde...",
        _                           => ""
    };

    private float tempoCronometro = 0f;

    void Start()
    {
        AtualizarVisual();
    }

    void Update()
    {
        if (!GameManager.Instance.EstaJogando()) return;
        AtualizarCronometro();
    }

    // ── Interação ─────────────────────────────────────────────────────────────

    public void Interagir(PlayerController jogador)
    {
        if (!PodeInteragir()) return;
        StartCoroutine(SequenciaOperacao());
    }

    private IEnumerator SequenciaOperacao()
    {
        // 1. Inicializando
        MudarEstado(EstadoEstacao.Iniciando);
        AudioManager.Instance?.TocarEfeito("estacao_ligando");

        float progresso = 0f;
        while (progresso < tempoInicializacao)
        {
            progresso += Time.deltaTime;
            tempoCronometro = progresso;
            barraOperacao.value = progresso / tempoInicializacao;
            yield return null;
        }

        // 2. Ativa — adiciona água bônus imediatamente
        MudarEstado(EstadoEstacao.Ativa);
        CityWaterSystem.Instance?.AdicionarAgua(aguaBonus);
        CityWaterSystem.Instance?.AtivarEstacaoTratamento();
        AudioManager.Instance?.TocarEfeito("estacao_ativa");
        UIManager.Instance?.MostrarFeedback($"+{aguaBonus}L — Estação ativada!");

        // 3. Conta duração normal
        float tempoAtiva = 0f;
        while (tempoAtiva < duracaoOperacao)
        {
            tempoAtiva += Time.deltaTime;
            tempoCronometro = tempoAtiva;
            barraOperacao.value = 1f - (tempoAtiva / duracaoOperacao);
            yield return null;
        }

        // 4. Sobrecarga: desligamento controlado
        MudarEstado(EstadoEstacao.Sobrecarregada);
        CityWaterSystem.Instance?.DesativarEstacaoTratamento();
        AudioManager.Instance?.TocarEfeito("estacao_sobrecarga");
        UIManager.Instance?.MostrarAlerta("Estação em resfriamento...", Color.yellow);

        yield return new WaitForSeconds(tempoSobrecarga);

        // 5. Resfriamento — estação indisponível por um tempo
        float tempoResfriando = 0f;
        while (tempoResfriando < tempoResfriamento)
        {
            tempoResfriando += Time.deltaTime;
            tempoCronometro = tempoResfriando;
            barraOperacao.value = tempoResfriando / tempoResfriamento;
            yield return null;
        }

        // 6. Disponível novamente
        MudarEstado(EstadoEstacao.Desligada);
        AudioManager.Instance?.TocarEfeito("estacao_pronta");
        UIManager.Instance?.MostrarFeedback("Estação pronta para nova operação!");
    }

    // ── Estado visual ─────────────────────────────────────────────────────────

    private void MudarEstado(EstadoEstacao novoEstado)
    {
        Estado = novoEstado;
        AtualizarVisual();
    }

    private void AtualizarVisual()
    {
        Color corLuz = Estado switch
        {
            EstadoEstacao.Desligada      => corDesligada,
            EstadoEstacao.Iniciando      => corIniciando,
            EstadoEstacao.Ativa          => corAtiva,
            EstadoEstacao.Sobrecarregada => corSobrecarregada,
            _                           => corDesligada
        };

        if (luzIndicadora != null) luzIndicadora.color = corLuz;

        bool emOperacao = Estado == EstadoEstacao.Ativa;
        particulasVapor?.gameObject.SetActive(emOperacao);
        if (emOperacao) particulasVapor?.Play();
        else            particulasVapor?.Stop();

        bool sobrecarga = Estado == EstadoEstacao.Sobrecarregada;
        particulasFumaca?.gameObject.SetActive(sobrecarga);
        if (sobrecarga) particulasFumaca?.Play();
        else            particulasFumaca?.Stop();

        animadorEstacao?.SetInteger("estado", (int)Estado);

        if (textoStatus != null)
            textoStatus.text = ObterDescricao();
    }

    // ── Debug ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(2f, 2f, 0f));
    }
}
