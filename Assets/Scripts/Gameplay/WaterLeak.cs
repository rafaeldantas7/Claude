using UnityEngine;
using System.Collections;

/// <summary>
/// Representa um vazamento de água na cidade.
/// Pode surgir em encanamentos, hidrantes ou calçadas.
/// </summary>
public class WaterLeak : MonoBehaviour, IInteragivel
{
    // ── Tipos de vazamento ──────────────────────────────────────────────────
    public enum TipoVazamento
    {
        Encanamento,   // Perda média, fácil de reparar
        Hidrante,      // Perda alta, demora mais para reparar
        Calcada,       // Perda baixa, rápido de reparar
        Principal      // Perda crítica, precisa de ferramentas especiais
    }

    // ── Configurações ───────────────────────────────────────────────────────
    [Header("Configuração")]
    [SerializeField] private TipoVazamento tipo = TipoVazamento.Encanamento;
    [SerializeField] private float perdaPorSegundoBase = 1f;
    [SerializeField] private bool aparecerAutomaticamente = false;
    [SerializeField] private float delayAparecimento = 0f;

    [Header("Visual")]
    [SerializeField] private Animator animadorAgua;
    [SerializeField] private ParticleSystem particulasAgua;
    [SerializeField] private GameObject iconeAlerta;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] spritesProgressoReparo; // 0..4 estágios

    [Header("Feedback")]
    [SerializeField] private GameObject textoFlutuante;

    // ── Estado ──────────────────────────────────────────────────────────────
    public bool EstaAtivo { get; private set; } = false;
    public bool EstaReparado { get; private set; } = false;
    private bool reparoEmAndamento = false;

    // ── Perda calculada conforme tipo ────────────────────────────────────────
    public float PerdaPorSegundo
    {
        get
        {
            return tipo switch
            {
                TipoVazamento.Encanamento => perdaPorSegundoBase * 1.0f,
                TipoVazamento.Hidrante    => perdaPorSegundoBase * 3.0f,
                TipoVazamento.Calcada     => perdaPorSegundoBase * 0.5f,
                TipoVazamento.Principal   => perdaPorSegundoBase * 5.0f,
                _                        => perdaPorSegundoBase,
            };
        }
    }

    // ── Eventos ─────────────────────────────────────────────────────────────
    public static event System.Action<WaterLeak> OnVazamentoAtivado;
    public static event System.Action<WaterLeak> OnVazamentoConsertado;

    void Start()
    {
        if (aparecerAutomaticamente)
            StartCoroutine(AparecerComDelay());
        else
            DesativarVisual();
    }

    // ── Ciclo de vida ───────────────────────────────────────────────────────

    private IEnumerator AparecerComDelay()
    {
        yield return new WaitForSeconds(delayAparecimento);
        Ativar();
    }

    public void Ativar()
    {
        if (EstaAtivo || EstaReparado) return;

        EstaAtivo = true;
        AtivarVisual();
        CityWaterSystem.Instance?.RegistrarVazamento(this);
        OnVazamentoAtivado?.Invoke(this);
        AudioManager.Instance?.TocarEfeito("vazamento", transform.position);
    }

    public void Desativar()
    {
        EstaAtivo = false;
        DesativarVisual();
        CityWaterSystem.Instance?.RemoverVazamento(this);
    }

    // ── Interface IInteragivel ──────────────────────────────────────────────

    public bool PodeInteragir() => EstaAtivo && !reparoEmAndamento && !EstaReparado;

    public string ObterDescricao() => tipo switch
    {
        TipoVazamento.Encanamento => "Encanamento partido — pressione E para consertar",
        TipoVazamento.Hidrante    => "Hidrante danificado — pressione E para consertar",
        TipoVazamento.Calcada     => "Vazamento na calçada — pressione E para consertar",
        TipoVazamento.Principal   => "CANO PRINCIPAL rompido — pressione E para consertar",
        _                        => "Vazamento detectado"
    };

    // ── Reparo ──────────────────────────────────────────────────────────────

    public void IniciarReparo()
    {
        reparoEmAndamento = true;
        iconeAlerta?.SetActive(false);

        // Reduz partículas enquanto repara
        if (particulasAgua != null)
        {
            var emission = particulasAgua.emission;
            emission.rateOverTime = particulasAgua.emission.rateOverTime.constant * 0.3f;
        }
    }

    public void AtualizarProgressoReparo(float progresso)
    {
        if (spritesProgressoReparo == null || spritesProgressoReparo.Length == 0) return;
        int idx = Mathf.FloorToInt(progresso * (spritesProgressoReparo.Length - 1));
        idx = Mathf.Clamp(idx, 0, spritesProgressoReparo.Length - 1);
        if (spriteRenderer != null)
            spriteRenderer.sprite = spritesProgressoReparo[idx];
    }

    public void ConcluirReparo()
    {
        EstaAtivo   = false;
        EstaReparado = true;
        reparoEmAndamento = false;

        CityWaterSystem.Instance?.RemoverVazamento(this);
        OnVazamentoConsertado?.Invoke(this);

        DesativarVisual();
        MostrarEfeitoConclusao();

        // Remove o objeto após a animação
        Destroy(gameObject, 1.5f);
    }

    public void CancelarReparo()
    {
        reparoEmAndamento = false;
        iconeAlerta?.SetActive(true);

        // Restaura partículas
        if (particulasAgua != null)
            particulasAgua.Play();
    }

    // ── Visual ──────────────────────────────────────────────────────────────

    private void AtivarVisual()
    {
        gameObject.SetActive(true);
        particulasAgua?.Play();
        if (iconeAlerta != null) iconeAlerta.SetActive(true);
        animadorAgua?.SetBool("ativo", true);

        // Pulsar ícone de alerta
        StartCoroutine(PulsarAlerta());
    }

    private void DesativarVisual()
    {
        particulasAgua?.Stop();
        if (iconeAlerta != null) iconeAlerta.SetActive(false);
        animadorAgua?.SetBool("ativo", false);
    }

    private void MostrarEfeitoConclusao()
    {
        if (textoFlutuante != null)
        {
            var t = Instantiate(textoFlutuante, transform.position + Vector3.up, Quaternion.identity);
            Destroy(t, 1.5f);
        }
    }

    private IEnumerator PulsarAlerta()
    {
        if (iconeAlerta == null) yield break;
        while (EstaAtivo)
        {
            iconeAlerta.transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSeconds(0.5f);
            iconeAlerta.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ── Debug ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Color cor = tipo switch
        {
            TipoVazamento.Principal => Color.red,
            TipoVazamento.Hidrante  => Color.yellow,
            _                      => Color.cyan
        };
        Gizmos.color = cor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
