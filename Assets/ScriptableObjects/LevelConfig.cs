using UnityEngine;

/// <summary>
/// ScriptableObject de configuração de cada fase.
/// Criar via: Assets > Create > Cagepa > Configuração de Fase
/// Um asset por fase: LevelConfig_01, LevelConfig_02, etc.
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig_01", menuName = "Cagepa/Configuração de Fase")]
public class LevelConfig : ScriptableObject
{
    [Header("Identificação")]
    public int    numeroFase   = 1;
    public string nomeFase     = "Centro da Cidade";
    [TextArea]
    public string descricao    = "Conserte os vazamentos e conscientize os cidadãos!";
    public Sprite mapaThumbnail;

    [Header("Tempo")]
    public float duracaoSegundos = 180f;

    [Header("Objetivos")]
    public int   vazamentosNecessarios = 5;
    public int   cidadaosNecessarios   = 3;
    public float nivelAguaMinimoFinal  = 30f;

    [Header("Dificuldade - Vazamentos")]
    public float intervaloSpawnVazamento = 20f;
    public int   maxVazamentosSimultaneos = 3;
    [Range(0f, 1f)]
    public float chancePrincipal = 0f;
    [Range(0f, 1f)]
    public float chanceHidrante  = 0.2f;

    [Header("Dificuldade - Cidadãos")]
    public int totalCidadaosNaFase    = 6;
    [Range(0f, 1f)]
    public float porcentagemDesperdic = 0.5f;

    [Header("Coletáveis")]
    public float intervaloColetavel    = 30f;
    public float quantidadeAguaColet   = 10f;

    [Header("Estação de Tratamento")]
    public bool  estacaoDisponivel     = true;
    public float duracaoOperacaoEstac  = 60f;

    [Header("Trilha Sonora")]
    public AudioClip musicaFase;

    [Header("Pontuação Extra")]
    public int bonusConclusaoFase      = 500;

    // ── Validação rápida no editor ────────────────────────────────────────
    private void OnValidate()
    {
        duracaoSegundos          = Mathf.Max(30f, duracaoSegundos);
        intervaloSpawnVazamento  = Mathf.Max(3f, intervaloSpawnVazamento);
        maxVazamentosSimultaneos = Mathf.Max(1, maxVazamentosSimultaneos);
        nivelAguaMinimoFinal     = Mathf.Clamp(nivelAguaMinimoFinal, 0f, 60f);

        if (chancePrincipal + chanceHidrante > 1f)
            chanceHidrante = 1f - chancePrincipal;
    }

    /// <summary>Gera configs de 5 fases com dificuldade progressiva.</summary>
    public static LevelConfig[] GerarFasesPadrao()
    {
        var fases = new LevelConfig[5];
        for (int i = 0; i < 5; i++)
        {
            var lc = CreateInstance<LevelConfig>();
            lc.numeroFase                = i + 1;
            lc.nomeFase                  = NomeFase(i + 1);
            lc.duracaoSegundos           = 180f - i * 10f;
            lc.vazamentosNecessarios     = 3 + i * 2;
            lc.cidadaosNecessarios       = 2 + i;
            lc.intervaloSpawnVazamento   = Mathf.Max(8f, 22f - i * 3f);
            lc.maxVazamentosSimultaneos  = 2 + i;
            lc.chanceHidrante            = i >= 1 ? 0.2f + i * 0.05f : 0f;
            lc.chancePrincipal           = i >= 3 ? 0.1f + (i - 3) * 0.1f : 0f;
            lc.porcentagemDesperdic      = 0.4f + i * 0.1f;
            fases[i] = lc;
        }
        return fases;
    }

    private static string NomeFase(int n) => n switch
    {
        1 => "Centro da Cidade",
        2 => "Bairro Residencial",
        3 => "Zona Industrial",
        4 => "Periferia",
        5 => "Sertão — Missão Final",
        _ => $"Fase {n}"
    };
}
