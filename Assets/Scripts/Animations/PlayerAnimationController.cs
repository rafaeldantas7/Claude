using UnityEngine;

/// <summary>
/// Controla as animações do Player.
/// Usa o SpriteAnimator para trocar clips conforme o estado do PlayerController.
///
/// SETUP: Adicione este componente no mesmo GameObject que PlayerController.
/// Arraste os SpriteAnimationClips nos slots do Inspector.
/// </summary>
[RequireComponent(typeof(SpriteAnimator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimationController : MonoBehaviour
{
    // ── Clips (arrastar no Inspector) ────────────────────────────────────────
    [Header("Idle")]
    [SerializeField] private SpriteAnimationClip idleDown;
    [SerializeField] private SpriteAnimationClip idleUp;
    [SerializeField] private SpriteAnimationClip idleSide;

    [Header("Caminhando")]
    [SerializeField] private SpriteAnimationClip walkDown;
    [SerializeField] private SpriteAnimationClip walkUp;
    [SerializeField] private SpriteAnimationClip walkSide;

    [Header("Ações")]
    [SerializeField] private SpriteAnimationClip reparando;
    [SerializeField] private SpriteAnimationClip educando;
    [SerializeField] private SpriteAnimationClip vitoria;

    // ── Referências ──────────────────────────────────────────────────────────
    private SpriteAnimator  anim;
    private SpriteRenderer  sr;
    private Rigidbody2D     rb;

    // ── Estado de animação ───────────────────────────────────────────────────
    private enum Direcao { Down, Up, Side }
    private Direcao direcaoAtual = Direcao.Down;
    private bool    movendo      = false;
    private bool    emAcao       = false;

    void Awake()
    {
        anim = GetComponent<SpriteAnimator>();
        sr   = GetComponent<SpriteRenderer>();
        rb   = GetComponent<Rigidbody2D>();

        // Registra todos os clips
        anim.RegistrarClips(new[] {
            idleDown, idleUp, idleSide,
            walkDown, walkUp, walkSide,
            reparando, educando, vitoria
        });
    }

    void Start()
    {
        // Inscreve nos eventos de jogo
        GameManager.OnGameStateChanged += AoMudarEstadoJogo;
        anim.Reproduzir(idleDown);
    }

    void OnDestroy() => GameManager.OnGameStateChanged -= AoMudarEstadoJogo;

    void LateUpdate()
    {
        if (emAcao) return; // não sobrescreve animação de ação

        Vector2 vel = rb != null ? rb.linearVelocity : Vector2.zero;
        movendo = vel.magnitude > 0.1f;

        AtualizarDirecao(vel);
        AtualizarClip();
    }

    // ── Lógica de direção ────────────────────────────────────────────────────

    private void AtualizarDirecao(Vector2 vel)
    {
        if (!movendo) return;

        if (Mathf.Abs(vel.y) > Mathf.Abs(vel.x))
            direcaoAtual = vel.y > 0 ? Direcao.Up : Direcao.Down;
        else
        {
            direcaoAtual = Direcao.Side;
            sr.flipX     = vel.x < 0;
        }
    }

    private void AtualizarClip()
    {
        SpriteAnimationClip alvo = direcaoAtual switch
        {
            Direcao.Down => movendo ? walkDown  : idleDown,
            Direcao.Up   => movendo ? walkUp    : idleUp,
            Direcao.Side => movendo ? walkSide  : idleSide,
            _            => idleDown
        };

        if (alvo != null) anim.Reproduzir(alvo);
    }

    // ── API chamada pelo PlayerController ────────────────────────────────────

    /// <summary>Chame quando começar a reparar um vazamento.</summary>
    public void IniciarAnimacaoReparo()
    {
        emAcao = true;
        if (reparando != null) anim.Reproduzir(reparando, true);
    }

    /// <summary>Chame quando começar a educar cidadão.</summary>
    public void IniciarAnimacaoEducacao()
    {
        emAcao = true;
        if (educando != null) anim.Reproduzir(educando, true);
    }

    /// <summary>Chame ao concluir qualquer ação.</summary>
    public void EncerrarAnimacaoAcao()
    {
        emAcao = false;
        anim.Reproduzir(idleDown);
    }

    // ── Eventos de jogo ──────────────────────────────────────────────────────

    private void AoMudarEstadoJogo(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.Victory && vitoria != null)
        {
            emAcao = true;
            anim.Reproduzir(vitoria, true);
        }
    }
}
