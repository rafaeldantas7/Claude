using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema de água da cidade.
/// Controla o nível global de água, perdas por vazamento e distribuição por bairros.
/// </summary>
public class CityWaterSystem : MonoBehaviour
{
    public static CityWaterSystem Instance { get; private set; }

    // ── Configurações ───────────────────────────────────────────────────────
    [Header("Nível de Água")]
    [SerializeField] private float nivelMaximo = 100f;
    [SerializeField] private float nivelInicial = 80f;
    [SerializeField] [Range(0f, 100f)] private float nivelCritico = 20f;
    [SerializeField] [Range(0f, 100f)] private float nivelAlerta  = 40f;

    [Header("Consumo Base")]
    [Tooltip("Litros consumidos por segundo naturalmente")]
    [SerializeField] private float consumoBasePorSegundo = 0.5f;

    [Header("Reposição")]
    [Tooltip("Litros recuperados por segundo pela estação de tratamento")]
    [SerializeField] private float taxaReposicao = 0.2f;
    private bool estacaoAtiva = false;

    // ── Estado ──────────────────────────────────────────────────────────────
    public float NivelAtual { get; private set; }
    public float Percentual => NivelAtual / nivelMaximo;
    public bool EmAlerta  => NivelAtual <= nivelAlerta;
    public bool EmCritico => NivelAtual <= nivelCritico;

    private List<WaterLeak> vazamentosAtivos = new List<WaterLeak>();

    // ── Eventos ─────────────────────────────────────────────────────────────
    public static event System.Action<float> OnNivelAlterado;
    public static event System.Action        OnNivelCritico;
    public static event System.Action        OnNivelNormal;
    public static event System.Action        OnSemAgua;

    private bool alertaDisparado  = false;
    private bool criticoDisparado = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        NivelAtual = nivelInicial;
    }

    void Update()
    {
        if (!GameManager.Instance.EstaJogando()) return;

        AplicarConsumoBase();
        AplicarPerdasVazamentos();
        AplicarReposicao();
        VerificarLimites();
    }

    // ── Lógica de água ──────────────────────────────────────────────────────

    private void AplicarConsumoBase()
    {
        Reduzir(consumoBasePorSegundo * Time.deltaTime);
    }

    private void AplicarPerdasVazamentos()
    {
        foreach (var vaz in vazamentosAtivos)
        {
            if (vaz == null || !vaz.EstaAtivo) continue;
            Reduzir(vaz.PerdaPorSegundo * Time.deltaTime);
        }
    }

    private void AplicarReposicao()
    {
        if (!estacaoAtiva) return;
        NivelAtual = Mathf.Min(NivelAtual + taxaReposicao * Time.deltaTime, nivelMaximo);
        OnNivelAlterado?.Invoke(NivelAtual);
    }

    private void Reduzir(float quantidade)
    {
        NivelAtual = Mathf.Max(0f, NivelAtual - quantidade);
        OnNivelAlterado?.Invoke(NivelAtual);
    }

    private void VerificarLimites()
    {
        if (NivelAtual <= 0f)
        {
            OnSemAgua?.Invoke();
            GameManager.Instance?.GameOver("A cidade ficou sem água!");
            return;
        }

        if (EmCritico && !criticoDisparado)
        {
            criticoDisparado = true;
            alertaDisparado  = true;
            OnNivelCritico?.Invoke();
            AudioManager.Instance?.TocarEfeito("alerta_critico");
            UIManager.Instance?.MostrarAlerta("NÍVEL CRÍTICO DE ÁGUA!", Color.red);
        }
        else if (EmAlerta && !alertaDisparado)
        {
            alertaDisparado = true;
            AudioManager.Instance?.TocarEfeito("alerta");
            UIManager.Instance?.MostrarAlerta("Nível de água baixo!", Color.yellow);
        }
        else if (!EmAlerta && alertaDisparado)
        {
            alertaDisparado  = false;
            criticoDisparado = false;
            OnNivelNormal?.Invoke();
        }
    }

    // ── API pública ─────────────────────────────────────────────────────────

    public void RegistrarVazamento(WaterLeak vazamento)
    {
        if (!vazamentosAtivos.Contains(vazamento))
            vazamentosAtivos.Add(vazamento);
    }

    public void RemoverVazamento(WaterLeak vazamento)
    {
        vazamentosAtivos.Remove(vazamento);
    }

    public void AtivarEstacaoTratamento() => estacaoAtiva = true;
    public void DesativarEstacaoTratamento() => estacaoAtiva = false;

    /// <summary>Adiciona água ao reservatório (ex: coleta de chuva).</summary>
    public void AdicionarAgua(float quantidade)
    {
        NivelAtual = Mathf.Min(NivelAtual + quantidade, nivelMaximo);
        OnNivelAlterado?.Invoke(NivelAtual);
    }

    public int TotalVazamentosAtivos()
    {
        int count = 0;
        foreach (var v in vazamentosAtivos)
            if (v != null && v.EstaAtivo) count++;
        return count;
    }
}
