using UnityEngine;

/// <summary>
/// Controla as animações do CitizenController.
/// Troca entre idle, caminhando, desperdiçando e estado educado.
///
/// SETUP: Adicione no mesmo GameObject que CitizenController.
/// </summary>
[RequireComponent(typeof(SpriteAnimator))]
[RequireComponent(typeof(CitizenController))]
public class CitizenAnimationController : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private SpriteAnimationClip clipIdleDown;
    [SerializeField] private SpriteAnimationClip clipWalkSide;
    [SerializeField] private SpriteAnimationClip clipDesperdicio;
    [SerializeField] private SpriteAnimationClip clipEducado;
    [SerializeField] private SpriteAnimationClip clipFalando;     // durante educação

    [Header("Efeito visual ao ser educado")]
    [SerializeField] private ParticleSystem particulasEducacao;

    private SpriteAnimator     anim;
    private CitizenController  cidadao;
    private SpriteRenderer     sr;
    private Rigidbody2D        rb;

    // Estado interno de animação
    private bool emEducacao = false;
    private bool educado    = false;

    void Awake()
    {
        anim    = GetComponent<SpriteAnimator>();
        cidadao = GetComponent<CitizenController>();
        sr      = GetComponent<SpriteRenderer>();
        rb      = GetComponent<Rigidbody2D>();

        anim.RegistrarClips(new[] {
            clipIdleDown, clipWalkSide, clipDesperdicio, clipEducado, clipFalando
        });
    }

    void Start()
    {
        anim.Reproduzir(clipIdleDown);
    }

    void LateUpdate()
    {
        if (emEducacao || educado) return;

        Vector2 vel = rb != null ? rb.linearVelocity : Vector2.zero;
        bool movendo = vel.magnitude > 0.1f;

        if (movendo)
        {
            anim.Reproduzir(clipWalkSide);
            if (sr != null && vel.x != 0) sr.flipX = vel.x < 0;
        }
        else if (!cidadao.EstaEducado)
        {
            // Se desperdiça, mostra animação de torneira aberta
            anim.Reproduzir(clipDesperdicio ?? clipIdleDown);
        }
    }

    // ── Chamados pelo CitizenController ──────────────────────────────────────

    public void AoIniciarEducacao()
    {
        emEducacao = true;
        if (clipFalando != null) anim.Reproduzir(clipFalando, true);
    }

    public void AoConcluirEducacao()
    {
        emEducacao = false;
        educado    = true;

        if (clipEducado != null) anim.Reproduzir(clipEducado, true);

        // Dispara partículas de celebração
        particulasEducacao?.Play();

        // Após a animação de educação concluir, volta para idle feliz
        anim.OnAnimacaoConcluida += NomeClip =>
        {
            if (NomeClip == (clipEducado?.nome ?? ""))
                anim.Reproduzir(clipIdleDown);
        };
    }
}
