// Assets/Scripts/Editor/PrefabCreatorEditor.cs
// Menu Unity: Cagepa > Criar Prefab > ...
// Gera os prefabs configurados e os salva em Assets/Prefabs/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class PrefabCreatorEditor
{
    private const string PASTA_PREFABS = "Assets/Prefabs/";

    // ── Menu ────────────────────────────────────────────────────────────────
    [MenuItem("Cagepa/Criar Prefabs/Todos")]
    public static void CriarTodos()
    {
        CriarPrefabPlayer();
        CriarPrefabsVazamento();
        CriarPrefabCidadao();
        CriarPrefabEstacao();
        CriarPrefabColetavel();
        CriarPrefabsUI();
        CriarPrefabsEfeitos();
        AssetDatabase.Refresh();
        Debug.Log("[Cagepa] Todos os prefabs criados em Assets/Prefabs/");
    }

    [MenuItem("Cagepa/Criar Prefabs/Player")]
    public static void CriarPrefabPlayer() => SalvarPrefab(MontarPlayer(), "Player");

    [MenuItem("Cagepa/Criar Prefabs/Vazamentos")]
    public static void CriarPrefabsVazamento()
    {
        SalvarPrefab(MontarVazamento("Encanamento", new Color(0.3f, 0.6f, 1f)), "Vazamento_Encanamento");
        SalvarPrefab(MontarVazamento("Hidrante",    new Color(1f,   0.3f, 0.3f)), "Vazamento_Hidrante");
        SalvarPrefab(MontarVazamento("Calcada",     new Color(0.5f, 0.8f, 1f)),   "Vazamento_Calcada");
        SalvarPrefab(MontarVazamento("Principal",   Color.red),                   "Vazamento_Principal");
    }

    [MenuItem("Cagepa/Criar Prefabs/Cidadão")]
    public static void CriarPrefabCidadao() => SalvarPrefab(MontarCidadao(), "Cidadao");

    [MenuItem("Cagepa/Criar Prefabs/Estação de Tratamento")]
    public static void CriarPrefabEstacao() => SalvarPrefab(MontarEstacao(), "EstacaoTratamento");

    [MenuItem("Cagepa/Criar Prefabs/Coletável de Água")]
    public static void CriarPrefabColetavel() => SalvarPrefab(MontarColetavel(), "Coletavel_Agua");

    [MenuItem("Cagepa/Criar Prefabs/UI")]
    public static void CriarPrefabsUI()
    {
        SalvarPrefab(MontarFeedbackFlutuante(), "UI_FeedbackFlutuante");
        SalvarPrefab(MontarBolhaDialogo(),      "UI_BolhaDialogo");
    }

    [MenuItem("Cagepa/Criar Prefabs/Efeitos")]
    public static void CriarPrefabsEfeitos()
    {
        SalvarPrefab(MontarEfeitoReparo(),   "Efeito_Reparo");
        SalvarPrefab(MontarEfeitoColeta(),   "Efeito_Coleta");
        SalvarPrefab(MontarEfeitoEducacao(), "Efeito_Educacao");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  MONTAGEM DOS PREFABS
    // ════════════════════════════════════════════════════════════════════════

    // ── PLAYER ───────────────────────────────────────────────────────────────
    private static GameObject MontarPlayer()
    {
        var root = new GameObject("Player");
        root.tag   = "Player";
        root.layer = LayerMask.NameToLayer("Default"); // mudar para "Player" após configurar layers

        // Sprite
        var sr = root.AddComponent<SpriteRenderer>();
        sr.color     = new Color(0.2f, 0.5f, 1f);
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 5;

        // Física
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale        = 0f;
        rb.freezeRotation      = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = root.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.55f, 0.75f);
        col.offset = Vector2.zero;

        // Scripts
        root.AddComponent<Animator>();
        root.AddComponent<PlayerController>();
        root.AddComponent<PlayerStats>();

        // Filho: ponto de detecção de interação (trigger)
        var detecGO = Filho(root.transform, "DeteccaoInteracao");
        var detecCol = detecGO.AddComponent<CircleCollider2D>();
        detecCol.radius    = 1f;
        detecCol.isTrigger = true;

        // Filho: sombra
        var sombraGO = Filho(root.transform, "Sombra");
        sombraGO.transform.localPosition = new Vector3(0, -0.3f, 0);
        var srSombra = sombraGO.AddComponent<SpriteRenderer>();
        srSombra.color          = new Color(0, 0, sombra: 0, a: 0.35f);
        srSombra.sortingLayerName = "Ground";
        srSombra.sortingOrder   = 1;
        srSombra.transform.localScale = new Vector3(0.7f, 0.35f, 1f);

        // Filho: efeito de reparo (desativado por padrão)
        var reparoGO = Filho(root.transform, "Efeito_Reparo");
        reparoGO.SetActive(false);
        var partReparo = reparoGO.AddComponent<ParticleSystem>();
        ConfigurarParticula(partReparo, new Color(0.3f, 0.7f, 1f), 0.5f, 15, 1.5f);

        // Filho: efeito educação
        var educGO = Filho(root.transform, "Efeito_Educacao");
        educGO.SetActive(false);
        var partEduc = educGO.AddComponent<ParticleSystem>();
        ConfigurarParticula(partEduc, Color.yellow, 0.3f, 8, 1f);

        return root;
    }

    // ── VAZAMENTO ────────────────────────────────────────────────────────────
    private static GameObject MontarVazamento(string tipo, Color cor)
    {
        var root = new GameObject($"Vazamento_{tipo}");
        root.layer = LayerMask.NameToLayer("Default"); // mudar para "Interagivel"

        // Sprite de fundo (mancha de água)
        var sr = root.AddComponent<SpriteRenderer>();
        sr.color = new Color(cor.r, cor.g, cor.b, 0.6f);
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = 2;

        // Collider de interação
        var col = root.AddComponent<CircleCollider2D>();
        col.radius    = 0.8f;
        col.isTrigger = true;

        // Script principal
        root.AddComponent<WaterLeak>();
        root.AddComponent<Animator>();

        // Filho: partículas de água
        var aguaGO = Filho(root.transform, "Particulas_Agua");
        var part = aguaGO.AddComponent<ParticleSystem>();
        ConfigurarParticulaVazamento(part, cor, tipo);

        // Filho: ícone de alerta pulsando
        var alertaGO = Filho(root.transform, "Icone_Alerta");
        alertaGO.transform.localPosition = new Vector3(0, 0.8f, 0);
        var srAlerta = alertaGO.AddComponent<SpriteRenderer>();
        srAlerta.color = Color.red;
        srAlerta.sortingLayerName = "UI";
        srAlerta.sortingOrder = 10;
        alertaGO.AddComponent<IconePulsante>();

        // Filho: texto flutuante de perda (debug/informativo)
        var textoGO = Filho(root.transform, "Texto_Flutuante");
        textoGO.SetActive(false);

        return root;
    }

    // ── CIDADÃO ──────────────────────────────────────────────────────────────
    private static GameObject MontarCidadao()
    {
        var root = new GameObject("Cidadao");
        root.layer = LayerMask.NameToLayer("Default"); // mudar para "Interagivel"

        // Sprite
        var sr = root.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.9f, 0.7f, 0.5f);
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 4;

        // Física (para patrulha)
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale    = 0f;
        rb.freezeRotation  = true;

        var col = root.AddComponent<CapsuleCollider2D>();
        col.size   = new Vector2(0.5f, 0.7f);
        col.offset = Vector2.zero;

        // Collider de interação (trigger maior)
        var trigGO = Filho(root.transform, "Trigger_Interacao");
        var trigCol = trigGO.AddComponent<CircleCollider2D>();
        trigCol.radius    = 1f;
        trigCol.isTrigger = true;

        // Scripts
        root.AddComponent<Animator>();
        root.AddComponent<CitizenController>();

        // Ícone de desperdício
        var iconeDespGO = Filho(root.transform, "Icone_Desperdicio");
        iconeDespGO.transform.localPosition = new Vector3(0, 0.9f, 0);
        var srIcDes = iconeDespGO.AddComponent<SpriteRenderer>();
        srIcDes.color = Color.red;
        srIcDes.sortingLayerName = "UI";
        srIcDes.sortingOrder = 10;
        iconeDespGO.AddComponent<IconePulsante>();

        // Ícone consciente
        var iconeConsGO = Filho(root.transform, "Icone_Consciente");
        iconeConsGO.transform.localPosition = new Vector3(0, 0.9f, 0);
        var srIcCons = iconeConsGO.AddComponent<SpriteRenderer>();
        srIcCons.color = Color.green;
        srIcCons.sortingLayerName = "UI";
        srIcCons.sortingOrder = 10;
        iconeConsGO.SetActive(false);

        // Pontos de patrulha (2 por padrão, configurar no editor)
        var patrulhaGO = Filho(root.transform, "PontosPatrulha");
        for (int i = 0; i < 2; i++)
        {
            var pGO = Filho(patrulhaGO.transform, $"Ponto_{i}");
            pGO.transform.localPosition = new Vector3(i == 0 ? -2f : 2f, 0, 0);
        }

        return root;
    }

    // ── ESTAÇÃO DE TRATAMENTO ─────────────────────────────────────────────────
    private static GameObject MontarEstacao()
    {
        var root = new GameObject("EstacaoTratamento");
        root.layer = LayerMask.NameToLayer("Default");

        // Sprite base
        var sr = root.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.4f, 0.5f, 0.6f);
        sr.sortingLayerName = "Buildings";
        sr.sortingOrder = 3;

        // Collider de interação
        var col = root.AddComponent<BoxCollider2D>();
        col.size      = new Vector2(2.5f, 2.5f);
        col.isTrigger = true;

        // Script
        root.AddComponent<Animator>();
        root.AddComponent<WaterTreatmentStation>();

        // Luz indicadora
        var luzGO = Filho(root.transform, "Luz_Indicadora");
        luzGO.transform.localPosition = new Vector3(0.7f, 0.7f, 0);
        var luz = luzGO.AddComponent<Light>();
        luz.type      = LightType.Point;
        luz.range     = 3f;
        luz.intensity = 2f;
        luz.color     = Color.gray;

        // Partículas de vapor (desativado)
        var vaporGO = Filho(root.transform, "Particulas_Vapor");
        vaporGO.SetActive(false);
        var partVapor = vaporGO.AddComponent<ParticleSystem>();
        ConfigurarParticula(partVapor, new Color(0.9f, 0.9f, 0.9f, 0.5f), 1f, 20, 2f);

        // Partículas de fumaça de sobrecarga (desativado)
        var fumacaGO = Filho(root.transform, "Particulas_Fumaca");
        fumacaGO.SetActive(false);
        var partFumaca = fumacaGO.AddComponent<ParticleSystem>();
        ConfigurarParticula(partFumaca, new Color(0.3f, 0.3f, 0.3f, 0.7f), 0.5f, 10, 3f);

        // Collider sólido (separado)
        var colSolidoGO = Filho(root.transform, "Colisao_Solida");
        var colSolido = colSolidoGO.AddComponent<BoxCollider2D>();
        colSolido.size = new Vector2(2f, 2f);

        return root;
    }

    // ── COLETÁVEL DE ÁGUA ─────────────────────────────────────────────────────
    private static GameObject MontarColetavel()
    {
        var root = new GameObject("Coletavel_Agua");
        root.tag = "Coletavel";

        // Sprite (gota d'água)
        var sr = root.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.2f, 0.6f, 1f);
        sr.sortingLayerName = "Items";
        sr.sortingOrder = 5;

        // Trigger de coleta
        var col = root.AddComponent<CircleCollider2D>();
        col.radius    = 0.4f;
        col.isTrigger = true;

        // Script
        root.AddComponent<WaterCollectible>();
        root.AddComponent<Animator>();

        // Brilho
        var brilhoGO = Filho(root.transform, "Brilho");
        brilhoGO.transform.localScale = Vector3.one * 1.5f;
        var srBrilho = brilhoGO.AddComponent<SpriteRenderer>();
        srBrilho.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        srBrilho.sortingLayerName = "Items";
        srBrilho.sortingOrder = 4;
        brilhoGO.AddComponent<RotacaoConstante>();

        return root;
    }

    // ── UI: FEEDBACK FLUTUANTE ─────────────────────────────────────────────────
    private static GameObject MontarFeedbackFlutuante()
    {
        var root = new GameObject("UI_FeedbackFlutuante");
        root.AddComponent<RectTransform>();
        root.AddComponent<FeedbackFlutuante>();

        var textoGO = Filho(root.transform, "Texto");
        textoGO.AddComponent<RectTransform>();
        var tmp = textoGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = "+100 pts";
        tmp.fontSize  = 22;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.color     = Color.yellow;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        return root;
    }

    // ── UI: BOLHA DE DIÁLOGO ──────────────────────────────────────────────────
    private static GameObject MontarBolhaDialogo()
    {
        var root = new GameObject("UI_BolhaDialogo");
        root.SetActive(false);

        var srBolha = root.AddComponent<SpriteRenderer>();
        srBolha.color = new Color(1f, 1f, 0.9f, 0.95f);
        srBolha.sortingLayerName = "UI";
        srBolha.sortingOrder = 15;

        var textoGO = Filho(root.transform, "Texto_Dialogo");
        // Usaremos TextMeshPro 3D para aparecer no mundo
        var tmp3d = textoGO.AddComponent<TMPro.TextMeshPro>();
        tmp3d.text     = "";
        tmp3d.fontSize = 3f;
        tmp3d.color    = Color.black;
        tmp3d.alignment = TMPro.TextAlignmentOptions.Center;
        textoGO.transform.localPosition = Vector3.zero;

        return root;
    }

    // ── EFEITOS ──────────────────────────────────────────────────────────────
    private static GameObject MontarEfeitoReparo()
    {
        var root = new GameObject("Efeito_Reparo");
        var part = root.AddComponent<ParticleSystem>();
        ConfigurarParticula(part, new Color(0.3f, 0.7f, 1f), 0.4f, 20, 1f);
        var main = part.main;
        main.loop = true;
        return root;
    }

    private static GameObject MontarEfeitoColeta()
    {
        var root = new GameObject("Efeito_Coleta");
        var part = root.AddComponent<ParticleSystem>();
        ConfigurarParticula(part, new Color(0.5f, 0.9f, 1f), 0.8f, 30, 0.5f);
        var main = part.main;
        main.loop = false;
        return root;
    }

    private static GameObject MontarEfeitoEducacao()
    {
        var root = new GameObject("Efeito_Educacao");
        var part = root.AddComponent<ParticleSystem>();
        ConfigurarParticula(part, Color.yellow, 0.5f, 15, 0.8f);
        var main = part.main;
        main.loop = false;
        return root;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════════
    private static void SalvarPrefab(GameObject go, string nome)
    {
        System.IO.Directory.CreateDirectory(PASTA_PREFABS);
        string caminho = $"{PASTA_PREFABS}{nome}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, caminho);
        Object.DestroyImmediate(go);
        Debug.Log($"[Cagepa] Prefab salvo: {caminho}");
    }

    private static GameObject Filho(Transform pai, string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        return go;
    }

    private static void ConfigurarParticula(ParticleSystem ps, Color cor,
                                             float velocidade, int quantidade, float duracao)
    {
        var main = ps.main;
        main.startColor    = cor;
        main.startSpeed    = velocidade;
        main.maxParticles  = quantidade;
        main.startLifetime = duracao;
        main.startSize     = 0.15f;
        main.loop          = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = quantidade / duracao;
    }

    private static void ConfigurarParticulaVazamento(ParticleSystem ps, Color cor, string tipo)
    {
        float taxa      = tipo == "Principal" ? 40 : tipo == "Hidrante" ? 25 : 12;
        float velocidade = tipo == "Principal" ? 2f : tipo == "Hidrante" ? 1.2f : 0.6f;
        ConfigurarParticula(ps, cor, velocidade, (int)taxa * 3, 0.8f);

        var shape = ps.shape;
        shape.enabled    = true;
        shape.shapeType  = ParticleSystemShapeType.Circle;
        shape.radius     = 0.15f;
    }
}

// Componente utilitário: rotação constante (usado no brilho do coletável)
public class RotacaoConstante : MonoBehaviour
{
    [SerializeField] private float velocidade = 45f;
    void Update() => transform.Rotate(0, 0, velocidade * Time.deltaTime);
}

// Componente utilitário: ícone que pulsa
public class IconePulsante : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.15f;
    [SerializeField] private float frequencia = 2f;
    private Vector3 escalaBase;
    void Start() => escalaBase = transform.localScale;
    void Update()
    {
        float t = 1f + Mathf.Sin(Time.time * frequencia * Mathf.PI * 2f) * amplitude;
        transform.localScale = escalaBase * t;
    }
}

// Componente: texto flutuante que sobe e some
public class FeedbackFlutuante : MonoBehaviour
{
    [SerializeField] private float velocidadeSubida = 1f;
    [SerializeField] private float duracao          = 1.5f;
    private float tempo = 0f;
    private TMPro.TextMeshProUGUI tmp;

    void Start()
    {
        tmp = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        Destroy(gameObject, duracao);
    }

    void Update()
    {
        tempo += Time.deltaTime;
        transform.position += Vector3.up * velocidadeSubida * Time.deltaTime;
        if (tmp != null)
        {
            var c = tmp.color;
            c.a = 1f - (tempo / duracao);
            tmp.color = c;
        }
    }
}
#endif
