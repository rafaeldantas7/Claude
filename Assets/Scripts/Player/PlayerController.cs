using UnityEngine;
using System.Collections;

/// <summary>
/// Controla o personagem principal "Guardião das Águas".
/// Movimentação top-down 2D, interação com vazamentos, estações e cidadãos.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    // ── Configurações de movimento ──────────────────────────────────────────
    [Header("Movimento")]
    [SerializeField] private float velocidade = 5f;
    [SerializeField] private float velocidadeComFerramentas = 3.5f;

    // ── Ferramentas disponíveis ─────────────────────────────────────────────
    [Header("Ferramentas")]
    [SerializeField] private float tempoReparoVazamento = 3f;
    [SerializeField] private float tempoEducacaoCidadao = 2f;
    [SerializeField] private LayerMask camadaInteragivel;

    // ── Referências de componentes ──────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // ── Estado interno ──────────────────────────────────────────────────────
    private Vector2 direcaoMovimento;
    private bool estaReparando = false;
    private bool estaEducando  = false;
    private IInteragivel alvoDaAcao;

    // ── Hash dos parâmetros do Animator ────────────────────────────────────
    private static readonly int HashVelocidade    = Animator.StringToHash("velocidade");
    private static readonly int HashDirecaoX      = Animator.StringToHash("direcaoX");
    private static readonly int HashDirecaoY      = Animator.StringToHash("direcaoY");
    private static readonly int HashReparando     = Animator.StringToHash("reparando");
    private static readonly int HashEducando      = Animator.StringToHash("educando");

    // ── Efeitos visuais ─────────────────────────────────────────────────────
    [Header("Efeitos")]
    [SerializeField] private GameObject efeitoReparo;
    [SerializeField] private GameObject efeitoEducacao;
    [SerializeField] private GameObject efeitoPassos;

    void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        animator       = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!GameManager.Instance.EstaJogando()) return;

        LerInput();
        AtualizarAnimacoes();
        VerificarInteracao();
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.EstaJogando()) return;
        Mover();
    }

    // ── Input ───────────────────────────────────────────────────────────────

    private void LerInput()
    {
        if (estaReparando || estaEducando)
        {
            direcaoMovimento = Vector2.zero;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        direcaoMovimento = new Vector2(h, v).normalized;
    }

    private void VerificarInteracao()
    {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            TentarInteragir();
        }

        // Cancelar ação com Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelarAcao();
        }
    }

    // ── Movimento ───────────────────────────────────────────────────────────

    private void Mover()
    {
        float vel = (estaReparando || estaEducando) ? 0f : velocidade;
        rb.linearVelocity = direcaoMovimento * vel;
    }

    // ── Animações ───────────────────────────────────────────────────────────

    private void AtualizarAnimacoes()
    {
        animator.SetFloat(HashVelocidade, direcaoMovimento.magnitude);
        animator.SetBool(HashReparando, estaReparando);
        animator.SetBool(HashEducando, estaEducando);

        if (direcaoMovimento != Vector2.zero)
        {
            animator.SetFloat(HashDirecaoX, direcaoMovimento.x);
            animator.SetFloat(HashDirecaoY, direcaoMovimento.y);
        }

        // Virar sprite horizontalmente conforme direção
        if (direcaoMovimento.x != 0)
            spriteRenderer.flipX = direcaoMovimento.x < 0;

        // Ativar partículas de passos se movendo
        if (efeitoPassos != null)
            efeitoPassos.SetActive(direcaoMovimento.magnitude > 0.1f);
    }

    // ── Interação ───────────────────────────────────────────────────────────

    private void TentarInteragir()
    {
        // Detecta objetos interagiveis próximos (raio de 1 unidade)
        Collider2D[] proximidades = Physics2D.OverlapCircleAll(transform.position, 1f, camadaInteragivel);

        IInteragivel maisProximo = null;
        float menorDistancia = float.MaxValue;

        foreach (var col in proximidades)
        {
            IInteragivel interagivel = col.GetComponent<IInteragivel>();
            if (interagivel == null || !interagivel.PodeInteragir()) continue;

            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < menorDistancia)
            {
                menorDistancia = dist;
                maisProximo = interagivel;
                alvoDaAcao = interagivel;
            }
        }

        if (maisProximo is WaterLeak vazamento)
            StartCoroutine(RepararVazamento(vazamento));
        else if (maisProximo is CitizenController cidadao)
            StartCoroutine(EducarCidadao(cidadao));
        else if (maisProximo is WaterTreatmentStation estacao)
            estacao.Interagir(this);
    }

    // ── Ações ───────────────────────────────────────────────────────────────

    private IEnumerator RepararVazamento(WaterLeak vazamento)
    {
        estaReparando = true;
        vazamento.IniciarReparo();

        if (efeitoReparo != null)
        {
            efeitoReparo.SetActive(true);
            efeitoReparo.transform.position = vazamento.transform.position;
        }

        AudioManager.Instance?.TocarEfeito("reparo");

        float progresso = 0f;
        while (progresso < tempoReparoVazamento)
        {
            progresso += Time.deltaTime;
            UIManager.Instance?.AtualizarBarraProgresso(progresso / tempoReparoVazamento);

            // Verifica se o jogador se afastou
            float dist = Vector2.Distance(transform.position, vazamento.transform.position);
            if (dist > 1.5f)
            {
                CancelarAcao();
                yield break;
            }

            yield return null;
        }

        // Reparo concluído
        vazamento.ConcluirReparo();
        GameManager.Instance?.AdicionarPontos(GameManager.PONTOS_VAZAMENTO_CONSERTADO);
        AudioManager.Instance?.TocarEfeito("sucesso");
        UIManager.Instance?.MostrarFeedback("+100 pts - Vazamento consertado!");

        FinalizarAcao();
    }

    private IEnumerator EducarCidadao(CitizenController cidadao)
    {
        estaEducando = true;
        cidadao.IniciarEducacao();

        if (efeitoEducacao != null)
            efeitoEducacao.SetActive(true);

        AudioManager.Instance?.TocarEfeito("conversa");

        yield return new WaitForSeconds(tempoEducacaoCidadao);

        cidadao.ConcluirEducacao();
        GameManager.Instance?.AdicionarPontos(GameManager.PONTOS_CIDADAO_EDUCADO);
        AudioManager.Instance?.TocarEfeito("sucesso");
        UIManager.Instance?.MostrarFeedback("+50 pts - Cidadão conscientizado!");

        FinalizarAcao();
    }

    private void CancelarAcao()
    {
        StopAllCoroutines();
        FinalizarAcao();
        UIManager.Instance?.EsconderBarraProgresso();
    }

    private void FinalizarAcao()
    {
        estaReparando = false;
        estaEducando  = false;
        alvoDaAcao    = null;

        if (efeitoReparo != null)   efeitoReparo.SetActive(false);
        if (efeitoEducacao != null) efeitoEducacao.SetActive(false);

        UIManager.Instance?.EsconderBarraProgresso();
    }

    // ── Gizmos (debug no editor) ─────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
