using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerencia as fases do jogo: objetivos, tempo, dificuldade e spawn de eventos.
/// Cada fase tem metas específicas para o Guardião das Águas.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    // ── Configurações da fase ────────────────────────────────────────────────
    [Header("Fase")]
    [SerializeField] private int numeroFase = 1;
    [SerializeField] private float duracaoFaseSegundos = 180f;
    [SerializeField] private string nomeFase = "Centro da Cidade";
    [SerializeField] [TextArea] private string descricaoFase = "Conserte os vazamentos e conscientize os cidadãos!";

    [Header("Objetivos")]
    [SerializeField] private int vazamentosParaConsertar = 5;
    [SerializeField] private int cidadaosParaEducar      = 3;
    [SerializeField] private float nivelAguaMinimoFinal  = 30f;

    [Header("Dificuldade")]
    [Tooltip("Intervalo entre novos vazamentos (segundos)")]
    [SerializeField] private float intervaloSpawnVazamento = 20f;
    [SerializeField] private float intervaloSpawnMinimo    = 5f;
    [SerializeField] private float reducaoIntervaloPorFase = 2f;
    [SerializeField] private int   maxVazamentosSimultaneos = 3;

    [Header("Pontos de Spawn")]
    [SerializeField] private Transform[] pontosSpawnVazamento;
    [SerializeField] private GameObject  prefabVazamentoEncanamento;
    [SerializeField] private GameObject  prefabVazamentoHidrante;
    [SerializeField] private GameObject  prefabVazamentoPrincipal;

    [Header("Bônus")]
    [SerializeField] private GameObject prefabColetavelAgua;
    [SerializeField] private Transform[] pontosColetavel;
    [SerializeField] private float intervaloColetavel = 30f;

    // ── Estado ──────────────────────────────────────────────────────────────
    public float TempoRestante { get; private set; }
    public int   VazamentosConsertados { get; private set; }
    public int   CidadaosEducados      { get; private set; }

    private bool faseConcluida = false;
    private bool faseIniciada  = false;

    // ── Progresso dos objetivos ─────────────────────────────────────────────
    public float ProgressoVazamentos => (float)VazamentosConsertados / vazamentosParaConsertar;
    public float ProgressoCidadaos   => (float)CidadaosEducados / cidadaosParaEducar;
    public bool  ObjetivoVazamentos  => VazamentosConsertados >= vazamentosParaConsertar;
    public bool  ObjetivoCidadaos    => CidadaosEducados >= cidadaosParaEducar;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        TempoRestante = duracaoFaseSegundos;
    }

    void OnEnable()
    {
        WaterLeak.OnVazamentoConsertado  += AoConsertar;
        CitizenController.OnCidadaoEducado += AoEducar;
    }

    void OnDisable()
    {
        WaterLeak.OnVazamentoConsertado  -= AoConsertar;
        CitizenController.OnCidadaoEducado -= AoEducar;
    }

    void Start()
    {
        IniciarFase();
    }

    void Update()
    {
        if (!GameManager.Instance.EstaJogando() || faseConcluida) return;

        // Contagem regressiva
        TempoRestante -= Time.deltaTime;
        UIManager.Instance?.AtualizarTempo(TempoRestante);

        if (TempoRestante <= 0f)
        {
            TempoRestante = 0f;
            AvaliarResultado();
        }
    }

    // ── Inicialização ────────────────────────────────────────────────────────

    private void IniciarFase()
    {
        faseIniciada = true;
        UIManager.Instance?.AtualizarNivel(numeroFase);

        // Ajusta dificuldade conforme nível
        float intervaloAjustado = Mathf.Max(
            intervaloSpawnVazamento - (numeroFase - 1) * reducaoIntervaloPorFase,
            intervaloSpawnMinimo
        );

        StartCoroutine(SpawnVazamentosAutomatico(intervaloAjustado));
        StartCoroutine(SpawnColetaveis());
        StartCoroutine(IntroducaoFase());
    }

    private IEnumerator IntroducaoFase()
    {
        yield return new WaitForSeconds(0.5f);
        UIManager.Instance?.MostrarAlerta($"Fase {numeroFase}: {nomeFase}", Color.cyan);
        yield return new WaitForSeconds(3f);
        UIManager.Instance?.MostrarAlerta(descricaoFase, Color.white);
    }

    // ── Spawn de Vazamentos ──────────────────────────────────────────────────

    private IEnumerator SpawnVazamentosAutomatico(float intervalo)
    {
        // Spawn inicial após breve delay
        yield return new WaitForSeconds(5f);

        while (!faseConcluida && faseIniciada)
        {
            if (GameManager.Instance.EstaJogando())
            {
                int ativos = CityWaterSystem.Instance?.TotalVazamentosAtivos() ?? 0;
                if (ativos < maxVazamentosSimultaneos)
                    SpawnVazamentoAleatorio();
            }
            yield return new WaitForSeconds(intervalo);
        }
    }

    private void SpawnVazamentoAleatorio()
    {
        if (pontosSpawnVazamento == null || pontosSpawnVazamento.Length == 0) return;

        Transform ponto = pontosSpawnVazamento[Random.Range(0, pontosSpawnVazamento.Length)];
        GameObject prefab = EscolherPrefabVazamento();

        if (prefab != null)
        {
            var obj = Instantiate(prefab, ponto.position, Quaternion.identity);
            obj.GetComponent<WaterLeak>()?.Ativar();
        }
    }

    private GameObject EscolherPrefabVazamento()
    {
        // Fases mais avançadas têm chance de vazamentos mais graves
        float roll = Random.value;
        if (numeroFase >= 4 && roll < 0.2f && prefabVazamentoPrincipal != null)
            return prefabVazamentoPrincipal;
        if (numeroFase >= 2 && roll < 0.4f && prefabVazamentoHidrante != null)
            return prefabVazamentoHidrante;
        return prefabVazamentoEncanamento;
    }

    // ── Spawn de Coletáveis ──────────────────────────────────────────────────

    private IEnumerator SpawnColetaveis()
    {
        if (prefabColetavelAgua == null || pontosColetavel == null) yield break;

        while (!faseConcluida)
        {
            yield return new WaitForSeconds(intervaloColetavel);
            if (!GameManager.Instance.EstaJogando()) continue;

            Transform ponto = pontosColetavel[Random.Range(0, pontosColetavel.Length)];
            var obj = Instantiate(prefabColetavelAgua, ponto.position, Quaternion.identity);
            Destroy(obj, 15f); // Desaparece se não coletado
        }
    }

    // ── Registro de progresso ────────────────────────────────────────────────

    private void AoConsertar(WaterLeak _)
    {
        VazamentosConsertados++;
        VerificarCondicaoVitoria();
    }

    private void AoEducar(CitizenController _)
    {
        CidadaosEducados++;
        VerificarCondicaoVitoria();
    }

    // ── Avaliação de resultado ───────────────────────────────────────────────

    private void VerificarCondicaoVitoria()
    {
        if (ObjetivoVazamentos && ObjetivoCidadaos)
            AvaliarResultado();
    }

    private void AvaliarResultado()
    {
        if (faseConcluida) return;
        faseConcluida = true;

        float nivelAgua = CityWaterSystem.Instance?.NivelAtual ?? 0f;

        bool passou = ObjetivoVazamentos
                   && ObjetivoCidadaos
                   && nivelAgua >= nivelAguaMinimoFinal;

        if (passou)
        {
            GameManager.Instance?.Vitoria();
        }
        else
        {
            string motivo = !ObjetivoVazamentos
                ? $"Faltaram {vazamentosParaConsertar - VazamentosConsertados} consertos!"
                : !ObjetivoCidadaos
                    ? $"Faltaram educar {cidadaosParaEducar - CidadaosEducados} cidadãos!"
                    : "O nível de água caiu demais!";

            GameManager.Instance?.GameOver(motivo);
        }
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (pontosSpawnVazamento != null)
        {
            Gizmos.color = Color.red;
            foreach (var p in pontosSpawnVazamento)
                if (p != null) Gizmos.DrawWireCube(p.position, Vector3.one * 0.5f);
        }
        if (pontosColetavel != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var p in pontosColetavel)
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.3f);
        }
    }
}
