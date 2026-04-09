using UnityEngine;

/// <summary>
/// Controla as animações do WaterLeak.
/// Reage aos eventos de ativação, reparo em andamento e conclusão.
///
/// SETUP: Adicione no mesmo GameObject que WaterLeak.
/// Arraste os SpriteAnimationClips nos slots.
/// </summary>
[RequireComponent(typeof(SpriteAnimator))]
[RequireComponent(typeof(WaterLeak))]
public class WaterLeakAnimationController : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private SpriteAnimationClip clipAtivo;
    [SerializeField] private SpriteAnimationClip clipReparando;
    [SerializeField] private SpriteAnimationClip clipConcluido;

    [Header("Sprite por estágio de reparo")]
    [Tooltip("Sprites do cano sendo consertado (0 = quebrado, último = consertado)")]
    [SerializeField] private Sprite[] spritesEstagios;

    private SpriteAnimator anim;
    private WaterLeak      leak;
    private SpriteRenderer sr;
    private float          progressoReparo = 0f;

    void Awake()
    {
        anim = GetComponent<SpriteAnimator>();
        leak = GetComponent<WaterLeak>();
        sr   = GetComponent<SpriteRenderer>();

        anim.RegistrarClips(new[] { clipAtivo, clipReparando, clipConcluido });
    }

    void Start()
    {
        // Começa no clip ativo se o vazamento já estiver ativo
        if (leak.EstaAtivo && clipAtivo != null)
            anim.Reproduzir(clipAtivo);
    }

    void Update()
    {
        AtualizarEstagioReparo();
    }

    // ── Chamados pelo WaterLeak ───────────────────────────────────────────────

    public void AoAtivar()
    {
        progressoReparo = 0f;
        if (clipAtivo != null) anim.Reproduzir(clipAtivo, true);
    }

    public void AoIniciarReparo()
    {
        if (clipReparando != null) anim.Reproduzir(clipReparando, true);
    }

    public void AoConcluirReparo()
    {
        if (clipConcluido != null)
        {
            anim.Reproduzir(clipConcluido, true);
            anim.OnAnimacaoConcluida += _ => Destroy(gameObject, 0.5f);
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }

    // ── Estágios visuais do reparo ────────────────────────────────────────────

    /// <summary>Chame com valor 0..1 para atualizar o sprite de progressoo reparo.</summary>
    public void SetProgressoReparo(float progresso)
    {
        progressoReparo = progresso;
    }

    private void AtualizarEstagioReparo()
    {
        if (spritesEstagios == null || spritesEstagios.Length == 0) return;
        if (!leak.EstaAtivo) return;

        int idx = Mathf.FloorToInt(progressoReparo * (spritesEstagios.Length - 1));
        idx = Mathf.Clamp(idx, 0, spritesEstagios.Length - 1);

        if (sr != null && sr.sprite != spritesEstagios[idx])
            sr.sprite = spritesEstagios[idx];
    }
}
