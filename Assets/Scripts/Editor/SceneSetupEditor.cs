// Assets/Scripts/Editor/SceneSetupEditor.cs
// Menu Unity: Cagepa > Criar Cena > ...
// Gera automaticamente a hierarquia completa de cada cena do jogo.

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class SceneSetupEditor
{
    // ── Menu principal ───────────────────────────────────────────────────────
    [MenuItem("Cagepa/Criar Cena/Menu Principal")]
    public static void CriarCenaMenuPrincipal() => SetupMenuPrincipal();

    [MenuItem("Cagepa/Criar Cena/Fase de Jogo")]
    public static void CriarCenaFaseDeJogo() => SetupFaseJogo();

    [MenuItem("Cagepa/Criar Cena/Créditos")]
    public static void CriarCenaCreditos() => SetupCreditos();

    [MenuItem("Cagepa/Configurar Tags e Layers")]
    public static void ConfigurarTagsLayers() => SetupTagsLayers();

    // ════════════════════════════════════════════════════════════════════════
    //  MENU PRINCIPAL
    // ════════════════════════════════════════════════════════════════════════
    private static void SetupMenuPrincipal()
    {
        // ── Câmera ──
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.05f, 0.15f, 0.3f);
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // ── Canvas Principal ──
        var canvas = CriarCanvas("Canvas_Menu");

        // Fundo com gradiente (imagem)
        var fundo = CriarPainel(canvas.transform, "Fundo", new Color(0.05f, 0.12f, 0.25f));
        EsticarParenteFull(fundo.GetComponent<RectTransform>());

        // Logo Cagepa
        var logoGO = CriarTexto(canvas.transform, "Logo_Cagepa",
            "CAGEPA", 52, FontStyle.Bold, Color.white);
        var logoRT = logoGO.GetComponent<RectTransform>();
        logoRT.anchoredPosition = new Vector2(0, 150);
        logoRT.sizeDelta = new Vector2(600, 80);

        // Subtítulo
        var subGO = CriarTexto(canvas.transform, "Subtitulo",
            "Guardião das Águas", 28, FontStyle.Italic, new Color(0.4f, 0.8f, 1f));
        subGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 90);

        // Botões
        float[] alturasBotoes = { 0f, -70f, -140f };
        string[] labelsBotoes  = { "JOGAR", "OPÇÕES", "SAIR" };
        string[] metodosBotoes = { "BotaoIniciar", "BotaoOpcoes", "Application.Quit" };

        for (int i = 0; i < labelsBotoes.Length; i++)
        {
            var btn = CriarBotao(canvas.transform, $"Btn_{labelsBotoes[i]}",
                                  labelsBotoes[i], alturasBotoes[i]);
        }

        // Versão
        var versaoGO = CriarTexto(canvas.transform, "Versao",
            "v1.0  |  Cagepa © 2025", 12, FontStyle.Normal, new Color(0.6f, 0.6f, 0.6f));
        var versaoRT = versaoGO.GetComponent<RectTransform>();
        versaoRT.anchorMin = new Vector2(0, 0);
        versaoRT.anchorMax = new Vector2(1, 0);
        versaoRT.anchoredPosition = new Vector2(0, 20);

        // ── Managers ──
        var managersGO = new GameObject("_Managers");
        CriarFilho(managersGO.transform, "GameManager").AddComponent<GameManager>();
        CriarFilho(managersGO.transform, "AudioManager").AddComponent<AudioManager>();
        CriarFilho(managersGO.transform, "UIManager").AddComponent<UIManager>();

        // ── EventSystem ──
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("[Cagepa] Cena 'MenuPrincipal' criada com sucesso.");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  FASE DE JOGO
    // ════════════════════════════════════════════════════════════════════════
    private static void SetupFaseJogo()
    {
        // ─── Câmera ───────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor  = new Color(0.53f, 0.81f, 0.98f); // céu azul
        camGO.AddComponent<AudioListener>();
        // Script de câmera seguindo o player
        camGO.AddComponent<CameraFollow>();

        // ─── Tilemap ──────────────────────────────────────────────────────
        var gridGO = new GameObject("Grid");
        gridGO.AddComponent<UnityEngine.Tilemaps.Grid>();

        // Camadas do tilemap
        string[] camadas = { "Chao", "Construcoes", "Detalhes", "Colisao" };
        foreach (var nome in camadas)
        {
            var tmGO = CriarFilho(gridGO.transform, $"Tilemap_{nome}");
            tmGO.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var renderer = tmGO.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            if (nome == "Colisao")
            {
                tmGO.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
                tmGO.AddComponent<CompositeCollider2D>();
                tmGO.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                tmGO.layer = LayerMask.NameToLayer("Ground");
            }
        }

        // ─── Player ───────────────────────────────────────────────────────
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.layer = LayerMask.NameToLayer("Player");
        playerGO.AddComponent<SpriteRenderer>();
        var rbPlayer = playerGO.AddComponent<Rigidbody2D>();
        rbPlayer.gravityScale = 0f;
        rbPlayer.freezeRotation = true;
        rbPlayer.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var capPlayer = playerGO.AddComponent<CapsuleCollider2D>();
        capPlayer.size = new Vector2(0.6f, 0.8f);
        playerGO.AddComponent<Animator>();
        playerGO.AddComponent<PlayerController>();
        playerGO.AddComponent<PlayerStats>();

        // ─── Pontos de Spawn de Vazamento ─────────────────────────────────
        var spawnRoot = new GameObject("SpawnPoints_Vazamento");
        string[] posNomes = { "A", "B", "C", "D", "E", "F" };
        Vector2[] positions = {
            new(-8, 3), new(8, 3), new(-8, -3), new(8, -3), new(0, 5), new(0, -5)
        };
        for (int i = 0; i < posNomes.Length; i++)
        {
            var sp = CriarFilho(spawnRoot.transform, $"Spawn_{posNomes[i]}");
            sp.transform.position = positions[i];
        }

        // ─── Pontos de Coletáveis ─────────────────────────────────────────
        var coletRoot = new GameObject("SpawnPoints_Coletavel");
        Vector2[] posCol = { new(-5, 2), new(5, 2), new(-5, -2), new(5, -2) };
        for (int i = 0; i < posCol.Length; i++)
        {
            var cp = CriarFilho(coletRoot.transform, $"Coletavel_{i}");
            cp.transform.position = posCol[i];
        }

        // ─── Estação de Tratamento ─────────────────────────────────────────
        var estacaoGO = new GameObject("EstacaoTratamento");
        estacaoGO.transform.position = new Vector3(10, 0, 0);
        estacaoGO.layer = LayerMask.NameToLayer("Interagivel");
        estacaoGO.AddComponent<SpriteRenderer>();
        var boxEstacao = estacaoGO.AddComponent<BoxCollider2D>();
        boxEstacao.isTrigger = true;
        boxEstacao.size = new Vector2(2f, 2f);
        estacaoGO.AddComponent<WaterTreatmentStation>();

        // ─── Canvas HUD ───────────────────────────────────────────────────
        var canvasHUD = CriarCanvas("Canvas_HUD");
        canvasHUD.GetComponent<Canvas>().sortingOrder = 10;
        ConfigurarHUD(canvasHUD);

        // ─── Canvas Menus in-game ──────────────────────────────────────────
        var canvasMenu = CriarCanvas("Canvas_Menus");
        canvasMenu.GetComponent<Canvas>().sortingOrder = 20;
        ConfigurarMenusInGame(canvasMenu);

        // ─── Managers de cena ─────────────────────────────────────────────
        var managersGO = new GameObject("_Managers");
        var citySystem = CriarFilho(managersGO.transform, "CityWaterSystem");
        citySystem.AddComponent<CityWaterSystem>();

        var levelMgr = CriarFilho(managersGO.transform, "LevelManager");
        levelMgr.AddComponent<LevelManager>();

        var pooler = CriarFilho(managersGO.transform, "ObjectPooler");
        pooler.AddComponent<ObjectPooler>();

        // ─── Limites do mundo ─────────────────────────────────────────────
        CriarLimitesColisao();

        // ─── EventSystem ─────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("[Cagepa] Cena de Fase criada. Configure prefabs no LevelManager e a câmera no CameraFollow.");
    }

    // ─── HUD ─────────────────────────────────────────────────────────────────
    private static void ConfigurarHUD(GameObject canvas)
    {
        var t = canvas.transform;

        // Barra de água — topo esquerdo
        var painelAgua = CriarPainel(t, "Painel_Agua", new Color(0, 0, 0, 0.5f));
        var rtAgua = painelAgua.GetComponent<RectTransform>();
        rtAgua.anchorMin = new Vector2(0, 1);
        rtAgua.anchorMax = new Vector2(0, 1);
        rtAgua.pivot     = new Vector2(0, 1);
        rtAgua.anchoredPosition = new Vector2(20, -20);
        rtAgua.sizeDelta = new Vector2(250, 60);

        CriarTexto(painelAgua.transform, "Label_Agua", "Nível de Água", 14,
                   FontStyle.Bold, Color.white).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0, 15);

        var sliderAgua = CriarSlider(painelAgua.transform, "Slider_Agua");
        sliderAgua.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

        CriarTexto(painelAgua.transform, "Texto_Percentual", "100%", 13,
                   FontStyle.Normal, Color.white);

        // Pontuação — topo centro
        var textoPts = CriarTexto(t, "Texto_Pontuacao", "Pontos: 0", 20,
                                  FontStyle.Bold, Color.white);
        var rtPts = textoPts.GetComponent<RectTransform>();
        rtPts.anchorMin = new Vector2(0.5f, 1);
        rtPts.anchorMax = new Vector2(0.5f, 1);
        rtPts.anchoredPosition = new Vector2(0, -30);

        // Tempo — topo direito
        var textoTempo = CriarTexto(t, "Texto_Tempo", "03:00", 24,
                                    FontStyle.Bold, Color.white);
        var rtTempo = textoTempo.GetComponent<RectTransform>();
        rtTempo.anchorMin = new Vector2(1, 1);
        rtTempo.anchorMax = new Vector2(1, 1);
        rtTempo.pivot     = new Vector2(1, 1);
        rtTempo.anchoredPosition = new Vector2(-20, -20);

        // Contador de vazamentos — baixo esquerdo
        var textoVaz = CriarTexto(t, "Texto_Vazamentos", "Vazamentos: 0", 16,
                                   FontStyle.Normal, Color.white);
        var rtVaz = textoVaz.GetComponent<RectTransform>();
        rtVaz.anchorMin = new Vector2(0, 0);
        rtVaz.anchorMax = new Vector2(0, 0);
        rtVaz.pivot     = new Vector2(0, 0);
        rtVaz.anchoredPosition = new Vector2(20, 20);

        // Barra de progresso de ação — centro baixo
        var painelAcao = CriarPainel(t, "Painel_Progresso", new Color(0, 0, 0, 0.7f));
        var rtAcao = painelAcao.GetComponent<RectTransform>();
        rtAcao.anchorMin = new Vector2(0.5f, 0);
        rtAcao.anchorMax = new Vector2(0.5f, 0);
        rtAcao.anchoredPosition = new Vector2(0, 80);
        rtAcao.sizeDelta = new Vector2(300, 50);
        CriarTexto(painelAcao.transform, "Texto_Acao", "Trabalhando...", 14,
                   FontStyle.Normal, Color.white);
        CriarSlider(painelAcao.transform, "Slider_Progresso");
        painelAcao.SetActive(false);

        // Painel de alerta — centro
        var painelAlerta = CriarPainel(t, "Painel_Alerta", new Color(0.8f, 0.1f, 0.1f, 0.85f));
        var rtAlerta = painelAlerta.GetComponent<RectTransform>();
        rtAlerta.anchorMin = new Vector2(0.5f, 0.5f);
        rtAlerta.anchorMax = new Vector2(0.5f, 0.5f);
        rtAlerta.anchoredPosition = new Vector2(0, 100);
        rtAlerta.sizeDelta = new Vector2(450, 60);
        CriarTexto(painelAlerta.transform, "Texto_Alerta", "ALERTA!", 22,
                   FontStyle.Bold, Color.white);
        painelAlerta.SetActive(false);

        // Botão Pausa — topo direito
        var btnPausa = CriarBotao(t, "Btn_Pausa", "||", -30, 30);
        var rtPausa = btnPausa.GetComponent<RectTransform>();
        rtPausa.anchorMin = new Vector2(1, 1);
        rtPausa.anchorMax = new Vector2(1, 1);
        rtPausa.pivot     = new Vector2(1, 1);
        rtPausa.anchoredPosition = new Vector2(-20, -20);
        rtPausa.sizeDelta = new Vector2(50, 40);
    }

    private static void ConfigurarMenusInGame(GameObject canvas)
    {
        var t = canvas.transform;

        // ── Pausa ──
        var painelPausa = CriarPainel(t, "Painel_Pausa", new Color(0, 0, 0, 0.85f));
        EsticarParenteFull(painelPausa.GetComponent<RectTransform>());
        CriarTexto(painelPausa.transform, "Titulo_Pausa", "PAUSADO", 36,
                   FontStyle.Bold, Color.white).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0, 100);
        CriarBotao(painelPausa.transform, "Btn_Retomar",     "RETOMAR", 20);
        CriarBotao(painelPausa.transform, "Btn_MenuPausa",   "MENU PRINCIPAL", -60);
        painelPausa.SetActive(false);

        // ── Game Over ──
        var painelGO = CriarPainel(t, "Painel_GameOver", new Color(0.15f, 0f, 0f, 0.92f));
        EsticarParenteFull(painelGO.GetComponent<RectTransform>());
        CriarTexto(painelGO.transform, "Titulo_GameOver", "FIM DE JOGO", 40,
                   FontStyle.Bold, Color.red).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0, 130);
        CriarTexto(painelGO.transform, "Texto_Motivo", "A cidade ficou sem água!", 20,
                   FontStyle.Normal, Color.white).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0, 70);
        CriarTexto(painelGO.transform, "Texto_PontuacaoFinal", "Pontuação: 0", 24,
                   FontStyle.Bold, Color.yellow).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0, 20);
        CriarBotao(painelGO.transform, "Btn_Reiniciar",  "TENTAR NOVAMENTE", -40);
        CriarBotao(painelGO.transform, "Btn_MenuGO",     "MENU PRINCIPAL", -110);
        painelGO.SetActive(false);

        // ── Vitória ──
        var painelVit = CriarPainel(t, "Painel_Vitoria", new Color(0f, 0.1f, 0.2f, 0.92f));
        EsticarParenteFull(painelVit.GetComponent<RectTransform>());
        CriarTexto(painelVit.transform, "Titulo_Vitoria", "MISSÃO CUMPRIDA!", 38,
                   FontStyle.Bold, new Color(0.3f, 1f, 0.5f)).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0, 130);
        // Estrelas (3 filhos com Image)
        var estrelas = new GameObject("Estrelas");
        estrelas.transform.SetParent(painelVit.transform, false);
        for (int i = 0; i < 3; i++)
        {
            var eGO = new GameObject($"Estrela_{i + 1}");
            eGO.transform.SetParent(estrelas.transform, false);
            eGO.AddComponent<Image>().color = Color.yellow;
            var rt = eGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60, 60);
            rt.anchoredPosition = new Vector2(-80 + i * 80, 60);
        }
        CriarTexto(painelVit.transform, "Texto_PontuacaoVit", "Pontuação: 0", 26,
                   FontStyle.Bold, Color.white).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0, 10);
        CriarBotao(painelVit.transform, "Btn_ProximaFase",  "PRÓXIMA FASE", -50);
        CriarBotao(painelVit.transform, "Btn_MenuVit",      "MENU PRINCIPAL", -120);
        painelVit.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CRÉDITOS
    // ════════════════════════════════════════════════════════════════════════
    private static void SetupCreditos()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = new Color(0.02f, 0.08f, 0.18f);
        camGO.AddComponent<AudioListener>();

        var canvas = CriarCanvas("Canvas_Creditos");

        CriarTexto(canvas.transform, "Titulo", "Guardião das Águas", 36,
                   FontStyle.Bold, Color.white).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0, 200);

        string[] linhas = {
            "Desenvolvido para a Cagepa",
            "Companhia de Água e Esgotos da Paraíba",
            "",
            "Programação: Unity C#",
            "Arte: 2D Sprite",
            "",
            "Economize água. É vida."
        };

        for (int i = 0; i < linhas.Length; i++)
        {
            CriarTexto(canvas.transform, $"Linha_{i}", linhas[i], 18,
                       FontStyle.Normal, Color.white)
                .GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(0, 120 - i * 35);
        }

        CriarBotao(canvas.transform, "Btn_Menu", "MENU PRINCIPAL", -220);
        Debug.Log("[Cagepa] Cena 'Creditos' criada.");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TAGS E LAYERS
    // ════════════════════════════════════════════════════════════════════════
    private static void SetupTagsLayers()
    {
        Debug.Log("[Cagepa] Tags e Layers necessários:\n" +
                  "Tags: Player, Interagivel, Coletavel, Limite\n" +
                  "Layers: Ground (8), Player (9), Interagivel (10), Efeitos (11)\n" +
                  "Configure manualmente em Edit > Project Settings > Tags and Layers.");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════════
    private static GameObject CriarCanvas(string nome)
    {
        var go = new GameObject(nome);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject CriarPainel(Transform pai, string nome, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var img = go.AddComponent<Image>();
        img.color = cor;
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CriarTexto(Transform pai, string nome, string conteudo,
                                          int tamanho, FontStyle estilo, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = conteudo;
        tmp.fontSize  = tamanho;
        tmp.fontStyle = estilo == FontStyle.Bold
                            ? TMPro.FontStyles.Bold
                            : estilo == FontStyle.Italic
                                ? TMPro.FontStyles.Italic
                                : TMPro.FontStyles.Normal;
        tmp.color     = cor;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 40);
        return go;
    }

    private static GameObject CriarBotao(Transform pai, string nome, string label,
                                          float posY, float posX = 0)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.4f, 0.7f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250, 50);
        rt.anchoredPosition = new Vector2(posX, posY);

        var textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(go.transform, false);
        var tmp = textoGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.color     = Color.white;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        var rtT = textoGO.GetComponent<RectTransform>();
        EsticarParenteFull(rtT);
        return go;
    }

    private static GameObject CriarSlider(Transform pai, string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var slider = go.AddComponent<Slider>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 20);

        var bg = new GameObject("Background"); bg.transform.SetParent(go.transform, false);
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        EsticarParenteFull(bg.GetComponent<RectTransform>());

        var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(go.transform, false);
        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1f);
        EsticarParenteFull(fill.GetComponent<RectTransform>());
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.value = 1f;
        return go;
    }

    private static void EsticarParenteFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject CriarFilho(Transform pai, string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        return go;
    }

    private static void CriarLimitesColisao()
    {
        var limites = new GameObject("Limites");
        float w = 20f, h = 12f;
        (string nome, Vector2 pos, Vector2 tam)[] bordas =
        {
            ("Top",    new(0,  h), new(w * 2 + 2, 1)),
            ("Bottom", new(0, -h), new(w * 2 + 2, 1)),
            ("Left",   new(-w, 0), new(1, h * 2 + 2)),
            ("Right",  new( w, 0), new(1, h * 2 + 2)),
        };
        foreach (var (nome, pos, tam) in bordas)
        {
            var b = CriarFilho(limites.transform, nome);
            b.transform.position = pos;
            b.tag = "Limite";
            var col = b.AddComponent<BoxCollider2D>();
            col.size = tam;
        }
    }
}
#endif
