using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema leve de animação por sprite sheet.
/// Substitui o Animator do Unity para animações simples, reduzindo overhead.
/// Cada clipe é definido por um SpriteAnimationClip (ScriptableObject).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    // ── Configuração ─────────────────────────────────────────────────────────
    [SerializeField] private SpriteAnimationClip clipInicial;
    [SerializeField] private bool  reproduzirAoIniciar = true;

    // ── Estado ───────────────────────────────────────────────────────────────
    private SpriteRenderer     sr;
    private SpriteAnimationClip clipAtual;
    private int   frameAtual     = 0;
    private float tempoCronometro = 0f;
    private bool  reproduzindo   = false;
    private bool  pausado        = false;

    // Dicionário para busca rápida por nome
    private Dictionary<string, SpriteAnimationClip> clips = new();

    // ── Eventos ──────────────────────────────────────────────────────────────
    public event System.Action<string> OnAnimacaoConcluida;
    public event System.Action<int>    OnFrame;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (clipInicial != null && reproduzirAoIniciar)
            Reproduzir(clipInicial);
    }

    void Update()
    {
        if (!reproduzindo || pausado || clipAtual == null) return;
        if (clipAtual.sprites == null || clipAtual.sprites.Length == 0) return;

        tempoCronometro += Time.deltaTime;
        float duracaoFrame = 1f / clipAtual.fps;

        while (tempoCronometro >= duracaoFrame)
        {
            tempoCronometro -= duracaoFrame;
            AvancarFrame();
        }
    }

    private void AvancarFrame()
    {
        frameAtual++;
        int total = clipAtual.sprites.Length;

        if (frameAtual >= total)
        {
            if (clipAtual.loop)
            {
                frameAtual = 0;
            }
            else
            {
                frameAtual = total - 1;
                reproduzindo = false;
                OnAnimacaoConcluida?.Invoke(clipAtual.nome);
                return;
            }
        }

        AplicarFrame();
    }

    private void AplicarFrame()
    {
        if (clipAtual.sprites == null || frameAtual >= clipAtual.sprites.Length) return;
        sr.sprite = clipAtual.sprites[frameAtual];
        OnFrame?.Invoke(frameAtual);
    }

    // ── API pública ──────────────────────────────────────────────────────────

    /// <summary>Reproduz um clip diretamente.</summary>
    public void Reproduzir(SpriteAnimationClip clip, bool forcarReiniciar = false)
    {
        if (clip == null) return;
        if (clipAtual == clip && reproduzindo && !forcarReiniciar) return;

        clipAtual        = clip;
        frameAtual       = 0;
        tempoCronometro  = 0f;
        reproduzindo     = true;
        pausado          = false;
        AplicarFrame();
    }

    /// <summary>Reproduz por nome (precisa estar registrado via RegistrarClip).</summary>
    public void Reproduzir(string nomeClip, bool forcarReiniciar = false)
    {
        if (clips.TryGetValue(nomeClip, out var clip))
            Reproduzir(clip, forcarReiniciar);
        else
            Debug.LogWarning($"[SpriteAnimator] Clip '{nomeClip}' não encontrado em {name}.");
    }

    public void RegistrarClip(SpriteAnimationClip clip)
    {
        if (clip != null && !string.IsNullOrEmpty(clip.nome))
            clips[clip.nome] = clip;
    }

    public void RegistrarClips(SpriteAnimationClip[] lista)
    {
        foreach (var c in lista) RegistrarClip(c);
    }

    public void Pausar()    => pausado = true;
    public void Retomar()   => pausado = false;
    public void Parar()     { reproduzindo = false; frameAtual = 0; }

    public bool EstaReproduzindo => reproduzindo && !pausado;
    public string NomeClipAtual  => clipAtual?.nome ?? "";
    public int    FrameAtual     => frameAtual;
}
