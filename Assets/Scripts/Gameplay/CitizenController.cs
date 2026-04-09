using UnityEngine;
using System.Collections;

/// <summary>
/// Controla NPCs cidadãos da cidade.
/// Cada cidadão pode desperdiçar água e ser educado pelo jogador.
/// </summary>
public class CitizenController : MonoBehaviour, IInteragivel
{
    // ── Comportamentos possíveis ─────────────────────────────────────────────
    public enum ComportamentoCidadao
    {
        Consciente,       // Usa água com responsabilidade
        Desperdicador,    // Deixa torneira aberta, lava calçada etc.
        Poluidor,         // Descarta lixo em rios
        EmDuvida          // Pode ser facilmente educado
    }

    // ── Configurações ───────────────────────────────────────────────────────
    [Header("Perfil")]
    [SerializeField] private string nomeCidadao = "Cidadão";
    [SerializeField] private ComportamentoCidadao comportamento = ComportamentoCidadao.Desperdicador;
    [Tooltip("Litros desperdiçados por segundo quando ativo")]
    [SerializeField] private float desperdiciosPorSegundo = 0.3f;

    [Header("Diálogos")]
    [SerializeField] [TextArea] private string dialogoAntes = "Deixa eu usar a água do jeito que eu quero!";
    [SerializeField] [TextArea] private string dialogoDepois = "Boa ideia! Vou economizar água daqui em diante.";
    [SerializeField] [TextArea] private string dialogoJaEducado = "Já estou fazendo minha parte!";

    [Header("Visual")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteDesperdicio;
    [SerializeField] private Sprite spriteEducado;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject iconeDesperdicio;
    [SerializeField] private GameObject iconeConsciente;
    [SerializeField] private GameObject bolhaDialogo;
    [SerializeField] private TMPro.TextMeshProUGUI textoDialogo;

    [Header("Patrulha")]
    [SerializeField] private Transform[] pontosPatrulha;
    [SerializeField] private float velocidadePatrulha = 1.5f;

    // ── Estado ──────────────────────────────────────────────────────────────
    public bool EstaEducado { get; private set; } = false;
    private bool estaEmEducacao = false;
    private int indicePatrulha = 0;
    private Rigidbody2D rb;

    // ── Eventos ─────────────────────────────────────────────────────────────
    public static event System.Action<CitizenController> OnCidadaoEducado;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Consciente nunca desperdiça
        if (comportamento == ComportamentoCidadao.Consciente)
            EstaEducado = true;
    }

    void Start()
    {
        AtualizarVisual();

        if (!EstaEducado && comportamento != ComportamentoCidadao.Consciente)
            InvokeRepeating(nameof(AplicarDesperdicio), 1f, 1f);

        if (pontosPatrulha != null && pontosPatrulha.Length > 1)
            StartCoroutine(Patrulhar());
    }

    void Update()
    {
        if (estaEmEducacao) return;

        // Atualizar posição do ícone sobre o sprite
        if (iconeDesperdicio != null)
            iconeDesperdicio.SetActive(!EstaEducado && comportamento == ComportamentoCidadao.Desperdicador);
    }

    // ── Desperdício ──────────────────────────────────────────────────────────

    private void AplicarDesperdicio()
    {
        if (EstaEducado || !GameManager.Instance.EstaJogando()) return;
        CityWaterSystem.Instance?.Reduzir(desperdiciosPorSegundo);
    }

    // ── Interface IInteragivel ──────────────────────────────────────────────

    public bool PodeInteragir() => !EstaEducado && !estaEmEducacao;

    public string ObterDescricao()
    {
        if (EstaEducado) return dialogoJaEducado;
        return $"{nomeCidadao}: {dialogoAntes}\n[Pressione E para conversar]";
    }

    // ── Educação ─────────────────────────────────────────────────────────────

    public void IniciarEducacao()
    {
        estaEmEducacao = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        MostrarDialogo(dialogoAntes);
        spriteRenderer.sprite = spriteDesperdicio;
    }

    public void ConcluirEducacao()
    {
        EstaEducado = true;
        estaEmEducacao = false;

        CancelInvoke(nameof(AplicarDesperdicio));
        AtualizarVisual();
        StartCoroutine(MostrarDialogoTemporario(dialogoDepois, 2.5f));

        OnCidadaoEducado?.Invoke(this);
    }

    // ── Patrulha ────────────────────────────────────────────────────────────

    private IEnumerator Patrulhar()
    {
        while (true)
        {
            if (estaEmEducacao)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            Transform destino = pontosPatrulha[indicePatrulha];
            while (Vector2.Distance(transform.position, destino.position) > 0.1f)
            {
                if (estaEmEducacao) break;

                Vector2 direcao = (destino.position - transform.position).normalized;
                rb.linearVelocity = direcao * velocidadePatrulha;

                // Virar sprite
                if (spriteRenderer != null && direcao.x != 0)
                    spriteRenderer.flipX = direcao.x < 0;

                yield return null;
            }

            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(Random.Range(1f, 3f));

            indicePatrulha = (indicePatrulha + 1) % pontosPatrulha.Length;
        }
    }

    // ── Visual ───────────────────────────────────────────────────────────────

    private void AtualizarVisual()
    {
        if (EstaEducado)
        {
            spriteRenderer.sprite = spriteEducado;
            iconeDesperdicio?.SetActive(false);
            iconeConsciente?.SetActive(true);
        }
        else
        {
            spriteRenderer.sprite = spriteNormal;
            bool desperdicando = comportamento == ComportamentoCidadao.Desperdicador
                              || comportamento == ComportamentoCidadao.Poluidor;
            iconeDesperdicio?.SetActive(desperdicando);
            iconeConsciente?.SetActive(false);
        }
    }

    private void MostrarDialogo(string texto)
    {
        if (bolhaDialogo == null) return;
        bolhaDialogo.SetActive(true);
        if (textoDialogo != null) textoDialogo.text = texto;
    }

    private IEnumerator MostrarDialogoTemporario(string texto, float duracao)
    {
        MostrarDialogo(texto);
        yield return new WaitForSeconds(duracao);
        bolhaDialogo?.SetActive(false);
    }

    // ── Debug ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = EstaEducado ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.8f);

        if (pontosPatrulha != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < pontosPatrulha.Length; i++)
            {
                if (pontosPatrulha[i] == null) continue;
                Gizmos.DrawSphere(pontosPatrulha[i].position, 0.15f);
                if (i + 1 < pontosPatrulha.Length && pontosPatrulha[i + 1] != null)
                    Gizmos.DrawLine(pontosPatrulha[i].position, pontosPatrulha[i + 1].position);
            }
        }
    }
}
