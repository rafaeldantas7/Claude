using UnityEngine;

/// <summary>
/// Controla as animações da Estação de Tratamento de Água.
/// Reage a mudanças de estado: Desligada, Iniciando, Ativa, Sobrecarregada.
///
/// SETUP: Adicione no mesmo GameObject que WaterTreatmentStation.
/// </summary>
[RequireComponent(typeof(SpriteAnimator))]
[RequireComponent(typeof(WaterTreatmentStation))]
public class WaterTreatmentAnimationController : MonoBehaviour
{
    [Header("Clips por estado")]
    [SerializeField] private SpriteAnimationClip clipDesligada;
    [SerializeField] private SpriteAnimationClip clipIniciando;
    [SerializeField] private SpriteAnimationClip clipAtiva;
    [SerializeField] private SpriteAnimationClip clipSobrecarga;

    [Header("Luz indicadora")]
    [SerializeField] private Light luzIndicadora;
    [SerializeField] private float intensidadeAtiva    = 3f;
    [SerializeField] private float intensidadeDesligada = 0.5f;

    [Header("Efeitos de partícula")]
    [SerializeField] private ParticleSystem particulasVapor;
    [SerializeField] private ParticleSystem particulasFumaca;
    [SerializeField] private ParticleSystem particulasEletricidade;

    private SpriteAnimator         anim;
    private WaterTreatmentStation  estacao;

    // Para piscar a luz no estado de sobrecarga
    private float     tempoPiscar   = 0f;
    private bool      piscando      = false;

    void Awake()
    {
        anim    = GetComponent<SpriteAnimator>();
        estacao = GetComponent<WaterTreatmentStation>();

        anim.RegistrarClips(new[] { clipDesligada, clipIniciando, clipAtiva, clipSobrecarga });
    }

    void Start()
    {
        AplicarEstado(WaterTreatmentStation.EstadoEstacao.Desligada);
    }

    void Update()
    {
        if (piscando) AtualizarPiscarLuz();
        SincronizarComEstado();
    }

    // ── Sincronização de estado ───────────────────────────────────────────────

    private WaterTreatmentStation.EstadoEstacao estadoAnterior =
        (WaterTreatmentStation.EstadoEstacao)(-1);

    private void SincronizarComEstado()
    {
        if (estacao.Estado == estadoAnterior) return;
        estadoAnterior = estacao.Estado;
        AplicarEstado(estacao.Estado);
    }

    private void AplicarEstado(WaterTreatmentStation.EstadoEstacao estado)
    {
        // ── Animação ──
        var clip = estado switch
        {
            WaterTreatmentStation.EstadoEstacao.Desligada      => clipDesligada,
            WaterTreatmentStation.EstadoEstacao.Iniciando      => clipIniciando,
            WaterTreatmentStation.EstadoEstacao.Ativa          => clipAtiva,
            WaterTreatmentStation.EstadoEstacao.Sobrecarregada => clipSobrecarga,
            _ => clipDesligada
        };
        if (clip != null) anim.Reproduzir(clip, true);

        // ── Luz ──
        if (luzIndicadora != null)
        {
            luzIndicadora.color = estado switch
            {
                WaterTreatmentStation.EstadoEstacao.Desligada      => Color.gray,
                WaterTreatmentStation.EstadoEstacao.Iniciando      => Color.yellow,
                WaterTreatmentStation.EstadoEstacao.Ativa          => Color.green,
                WaterTreatmentStation.EstadoEstacao.Sobrecarregada => Color.red,
                _ => Color.gray
            };
            luzIndicadora.intensity = estado == WaterTreatmentStation.EstadoEstacao.Desligada
                ? intensidadeDesligada : intensidadeAtiva;
        }

        // ── Partículas ──
        bool ativa      = estado == WaterTreatmentStation.EstadoEstacao.Ativa;
        bool iniciando  = estado == WaterTreatmentStation.EstadoEstacao.Iniciando;
        bool sobrecarga = estado == WaterTreatmentStation.EstadoEstacao.Sobrecarregada;

        ControlarParticula(particulasVapor,        ativa || iniciando);
        ControlarParticula(particulasFumaca,        sobrecarga);
        ControlarParticula(particulasEletricidade,  iniciando);

        // ── Luz piscando na sobrecarga ──
        piscando = sobrecarga;
        if (!sobrecarga && luzIndicadora != null)
            luzIndicadora.intensity = intensidadeDesligada;
    }

    private void AtualizarPiscarLuz()
    {
        if (luzIndicadora == null) return;
        tempoPiscar += Time.deltaTime;
        float t = Mathf.PingPong(tempoPiscar * 4f, 1f);
        luzIndicadora.intensity = Mathf.Lerp(0.5f, intensidadeAtiva, t);
    }

    private static void ControlarParticula(ParticleSystem ps, bool ativar)
    {
        if (ps == null) return;
        if (ativar && !ps.isPlaying) ps.Play();
        else if (!ativar && ps.isPlaying) ps.Stop();
    }
}
