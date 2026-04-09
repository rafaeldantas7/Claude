using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Animações de UI: transições de painel, pulsar barra de água crítica,
/// entrada de textos e animação de estrelas na tela de vitória.
/// </summary>
public class UIAnimationController : MonoBehaviour
{
    public static UIAnimationController Instance { get; private set; }

    // ── Transições ────────────────────────────────────────────────────────────
    [Header("Transições de Painel")]
    [SerializeField] private float duracaoTransicao = 0.3f;
    [SerializeField] private AnimationCurve curvaEntrada  = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve curvaSaida    = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // ── Barra de água ─────────────────────────────────────────────────────────
    [Header("Barra de Água")]
    [SerializeField] private Image  preenchimentoAgua;
    [SerializeField] private float  velocidadePiscarCritico = 3f;

    // ── Estrelas de vitória ───────────────────────────────────────────────────
    [Header("Estrelas")]
    [SerializeField] private GameObject[] estrelas;
    [SerializeField] private float        delayEntreEstrelas = 0.4f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnEnable()
    {
        CityWaterSystem.OnNivelCritico += IniciarPiscarCritico;
        CityWaterSystem.OnNivelNormal  += PararPiscarCritico;
        GameManager.OnGameStateChanged += AoMudarEstadoJogo;
    }

    void OnDisable()
    {
        CityWaterSystem.OnNivelCritico -= IniciarPiscarCritico;
        CityWaterSystem.OnNivelNormal  -= PararPiscarCritico;
        GameManager.OnGameStateChanged -= AoMudarEstadoJogo;
    }

    // ── Transição de painéis ──────────────────────────────────────────────────

    /// <summary>Abre um painel com animação de fade + escala.</summary>
    public void AbrirPainel(GameObject painel)
    {
        if (painel == null) return;
        painel.SetActive(true);
        StartCoroutine(AnimarEntradaPainel(painel));
    }

    /// <summary>Fecha um painel com animação de fade.</summary>
    public void FecharPainel(GameObject painel, System.Action aoTerminar = null)
    {
        if (painel == null) return;
        StartCoroutine(AnimarSaidaPainel(painel, aoTerminar));
    }

    private IEnumerator AnimarEntradaPainel(GameObject painel)
    {
        var grupo = painel.GetComponent<CanvasGroup>() ?? painel.AddComponent<CanvasGroup>();
        var rt    = painel.GetComponent<RectTransform>();

        grupo.alpha = 0f;
        Vector3 escalaFinal  = rt.localScale;
        rt.localScale = escalaFinal * 0.85f;

        float t = 0f;
        while (t < duracaoTransicao)
        {
            t += Time.unscaledDeltaTime;
            float prog = curvaEntrada.Evaluate(t / duracaoTransicao);
            grupo.alpha     = prog;
            rt.localScale   = Vector3.LerpUnclamped(escalaFinal * 0.85f, escalaFinal, prog);
            yield return null;
        }
        grupo.alpha   = 1f;
        rt.localScale = escalaFinal;
    }

    private IEnumerator AnimarSaidaPainel(GameObject painel, System.Action aoTerminar)
    {
        var grupo = painel.GetComponent<CanvasGroup>() ?? painel.AddComponent<CanvasGroup>();
        float t = 0f;
        while (t < duracaoTransicao)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = curvaSaida.Evaluate(t / duracaoTransicao);
            yield return null;
        }
        painel.SetActive(false);
        aoTerminar?.Invoke();
    }

    // ── Barra de água crítica ─────────────────────────────────────────────────

    private Coroutine corPiscar;

    private void IniciarPiscarCritico()
    {
        if (corPiscar != null) StopCoroutine(corPiscar);
        corPiscar = StartCoroutine(PiscarBarra());
    }

    private void PararPiscarCritico()
    {
        if (corPiscar != null) { StopCoroutine(corPiscar); corPiscar = null; }
        if (preenchimentoAgua != null) preenchimentoAgua.color = new Color(0.2f, 0.6f, 1f);
    }

    private IEnumerator PiscarBarra()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.time * velocidadePiscarCritico, 1f);
            if (preenchimentoAgua != null)
                preenchimentoAgua.color = Color.Lerp(Color.red, new Color(1f, 0.5f, 0.2f), t);
            yield return null;
        }
    }

    // ── Estrelas de vitória ───────────────────────────────────────────────────

    public void AnimarEstrelas(int quantidade)
    {
        if (estrelas == null) return;
        StartCoroutine(SequenciaEstrelas(quantidade));
    }

    private IEnumerator SequenciaEstrelas(int quantidade)
    {
        for (int i = 0; i < estrelas.Length; i++)
        {
            if (estrelas[i] == null) continue;
            bool ativa = i < quantidade;
            estrelas[i].SetActive(false);

            if (ativa)
            {
                yield return new WaitForSecondsRealtime(delayEntreEstrelas);
                estrelas[i].SetActive(true);
                yield return StartCoroutine(AnimarEstrela(estrelas[i]));
            }
        }
    }

    private IEnumerator AnimarEstrela(GameObject estrela)
    {
        var rt = estrela.GetComponent<RectTransform>();
        if (rt == null) yield break;

        rt.localScale = Vector3.zero;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float prog = curvaEntrada.Evaluate(t / 0.35f);
            rt.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one * 1.2f, prog);
            yield return null;
        }
        // Pequeno "bounce"
        t = 0f;
        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            float prog = t / 0.15f;
            rt.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, prog);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // ── Texto de pontuação contando ───────────────────────────────────────────

    /// <summary>Anima o texto contando de 0 até o valor final.</summary>
    public void AnimarContadorPontuacao(TMPro.TextMeshProUGUI texto, int valorFinal, float duracao = 1.5f)
    {
        StartCoroutine(ContarPontuacao(texto, valorFinal, duracao));
    }

    private IEnumerator ContarPontuacao(TMPro.TextMeshProUGUI texto, int alvo, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            int valor = Mathf.RoundToInt(curvaEntrada.Evaluate(t / dur) * alvo);
            texto.text = $"Pontuação: {valor:N0}";
            yield return null;
        }
        texto.text = $"Pontuação: {alvo:N0}";
    }

    // ── Evento de mudança de estado ───────────────────────────────────────────

    private void AoMudarEstadoJogo(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.Victory)
        {
            int pontos = GameManager.Instance?.Score ?? 0;
            int estrelasCont = pontos >= 2000 ? 3 : pontos >= 1000 ? 2 : 1;
            AnimarEstrelas(estrelasCont);
        }
    }
}
