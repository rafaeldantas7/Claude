using UnityEngine;

/// <summary>
/// Animação do coletável de água: flutuação, brilho rotativo e efeito de coleta.
/// Não depende do SpriteAnimator — usa lógica procedural por ser simples.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WaterCollectibleAnimationController : MonoBehaviour
{
    [Header("Flutuação")]
    [SerializeField] private float amplitudeFloat = 0.15f;
    [SerializeField] private float frequenciaFloat = 1.5f;

    [Header("Brilho")]
    [SerializeField] private Transform brilho;
    [SerializeField] private float velocidadeRotBrilho = 60f;
    [SerializeField] private float amplitudeBrilho     = 0.15f;
    [SerializeField] private float frequenciaBrilho    = 2f;

    [Header("Coleta")]
    [SerializeField] private SpriteAnimationClip clipColetado;
    [SerializeField] private ParticleSystem particulasColeta;

    private Vector3   posOrigem;
    private SpriteRenderer sr;
    private SpriteAnimator anim;
    private bool coletado = false;

    void Awake()
    {
        sr   = GetComponent<SpriteRenderer>();
        anim = GetComponent<SpriteAnimator>();
        posOrigem = transform.position;

        if (anim != null && clipColetado != null)
            anim.RegistrarClip(clipColetado);
    }

    void Update()
    {
        if (coletado) return;

        // Flutuação senoidal
        float y = posOrigem.y + Mathf.Sin(Time.time * frequenciaFloat * Mathf.PI * 2f) * amplitudeFloat;
        transform.position = new Vector3(posOrigem.x, y, posOrigem.z);

        // Brilho rotativo e pulsante
        if (brilho != null)
        {
            brilho.Rotate(0, 0, velocidadeRotBrilho * Time.deltaTime);
            float escala = 1f + Mathf.Sin(Time.time * frequenciaBrilho * Mathf.PI * 2f) * amplitudeBrilho;
            brilho.localScale = Vector3.one * escala;
        }

        // Piscar levemente o sprite
        float alpha = 0.85f + Mathf.Sin(Time.time * 3f) * 0.15f;
        var c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    /// <summary>Chame ao coletar o item para disparar a animação de saída.</summary>
    public void AnimarColeta()
    {
        coletado = true;

        particulasColeta?.Play();

        if (anim != null && clipColetado != null)
        {
            anim.Reproduzir(clipColetado, true);
            anim.OnAnimacaoConcluida += _ => Destroy(gameObject);
        }
        else
        {
            // Animação procedural de escala e fade
            StartCoroutine(AnimacaoDesaparecimento());
        }
    }

    private System.Collections.IEnumerator AnimacaoDesaparecimento()
    {
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float prog = t / 0.4f;
            transform.localScale = Vector3.one * (1f + prog * 0.5f);
            var c = sr.color;
            c.a = 1f - prog;
            sr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}
