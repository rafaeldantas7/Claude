using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gerencia todos os sons do jogo: música de fundo, efeitos sonoros e jingles.
/// Suporta áudio posicional 2D e pooling básico de AudioSources.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── Configurações de volume ─────────────────────────────────────────────
    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float volumeMusica  = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float volumeEfeitos = 0.8f;

    // ── Música ───────────────────────────────────────────────────────────────
    [Header("Músicas")]
    [SerializeField] private AudioClip musicaMenuPrincipal;
    [SerializeField] private AudioClip musicaJogo;
    [SerializeField] private AudioClip musicaAlerta;
    [SerializeField] private AudioClip musicaVitoria;
    [SerializeField] private AudioClip musicaGameOver;
    private AudioSource fonteMusica;

    // ── Efeitos sonoros ───────────────────────────────────────────────────────
    [Header("Efeitos Sonoros")]
    [SerializeField] private SomEntry[] efeitos;

    [System.Serializable]
    public struct SomEntry
    {
        public string chave;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        [Range(0.8f, 1.2f)] public float pitch;
        public bool variaPitch;
    }

    private Dictionary<string, SomEntry> dicEfeitos = new();
    private List<AudioSource> poolEfeitos = new();
    private const int TAMANHO_POOL = 10;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InicializarPool();
        IndexarEfeitos();

        fonteMusica = gameObject.AddComponent<AudioSource>();
        fonteMusica.loop   = true;
        fonteMusica.volume = volumeMusica;
    }

    void Start()
    {
        // Inscrevemos nos eventos do GameManager para trocar música conforme estado
        GameManager.OnGameStateChanged += AoMudarEstado;
    }

    void OnDestroy()
    {
        GameManager.OnGameStateChanged -= AoMudarEstado;
    }

    // ── Inicialização ────────────────────────────────────────────────────────

    private void InicializarPool()
    {
        for (int i = 0; i < TAMANHO_POOL; i++)
        {
            var go = new GameObject($"AudioSource_Efeito_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            poolEfeitos.Add(src);
        }
    }

    private void IndexarEfeitos()
    {
        foreach (var e in efeitos)
            if (!string.IsNullOrEmpty(e.chave))
                dicEfeitos[e.chave] = e;
    }

    // ── Música ───────────────────────────────────────────────────────────────

    public void TocarMusica(AudioClip clip)
    {
        if (clip == null || fonteMusica.clip == clip) return;
        fonteMusica.clip   = clip;
        fonteMusica.volume = volumeMusica;
        fonteMusica.Play();
    }

    public void PausarMusica()  => fonteMusica?.Pause();
    public void RetomarMusica() => fonteMusica?.UnPause();
    public void PararMusica()   => fonteMusica?.Stop();

    private void AoMudarEstado(GameManager.GameState estado)
    {
        switch (estado)
        {
            case GameManager.GameState.MainMenu:
                TocarMusica(musicaMenuPrincipal);
                break;
            case GameManager.GameState.Playing:
                // Se estava no alerta, volta para jogo
                if (fonteMusica.clip != musicaJogo)
                    TocarMusica(musicaJogo);
                break;
            case GameManager.GameState.GameOver:
                TocarMusica(musicaGameOver);
                break;
            case GameManager.GameState.Victory:
                TocarMusica(musicaVitoria);
                break;
        }
    }

    // ── Efeitos Sonoros ──────────────────────────────────────────────────────

    public void TocarEfeito(string chave, Vector3? posicao = null)
    {
        if (!dicEfeitos.TryGetValue(chave, out SomEntry entrada)) return;
        if (entrada.clip == null) return;

        AudioSource fonte = ObterFonteLivre();
        if (fonte == null) return;

        fonte.clip   = entrada.clip;
        fonte.volume = entrada.volume * volumeEfeitos;
        fonte.pitch  = entrada.variaPitch
            ? Random.Range(entrada.pitch - 0.1f, entrada.pitch + 0.1f)
            : entrada.pitch;

        if (posicao.HasValue)
        {
            fonte.transform.position = posicao.Value;
            fonte.spatialBlend = 1f; // 3D
        }
        else
        {
            fonte.spatialBlend = 0f; // 2D
        }

        fonte.Play();
    }

    private AudioSource ObterFonteLivre()
    {
        foreach (var src in poolEfeitos)
            if (!src.isPlaying) return src;

        // Pool cheio: reutiliza o mais antigo
        return poolEfeitos[0];
    }

    // ── Volume ───────────────────────────────────────────────────────────────

    public void SetVolumeMusica(float v)
    {
        volumeMusica = Mathf.Clamp01(v);
        if (fonteMusica != null) fonteMusica.volume = volumeMusica;
    }

    public void SetVolumeEfeitos(float v)
    {
        volumeEfeitos = Mathf.Clamp01(v);
    }

    public float GetVolumeMusica()  => volumeMusica;
    public float GetVolumeEfeitos() => volumeEfeitos;
}
