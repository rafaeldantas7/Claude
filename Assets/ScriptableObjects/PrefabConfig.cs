using UnityEngine;

/// <summary>
/// ScriptableObject que centraliza todas as referências de prefabs do jogo.
/// Criar via: Assets > Create > Cagepa > Configuração de Prefabs
/// </summary>
[CreateAssetMenu(fileName = "PrefabConfig", menuName = "Cagepa/Configuração de Prefabs")]
public class PrefabConfig : ScriptableObject
{
    [Header("Personagens")]
    public GameObject prefabPlayer;
    public GameObject prefabCidadaoNormal;
    public GameObject prefabCidadaoDesperd;
    public GameObject prefabCidadaoPoluidor;

    [Header("Vazamentos")]
    public GameObject prefabVazamentoEncanamento;
    public GameObject prefabVazamentoHidrante;
    public GameObject prefabVazamentoCalcada;
    public GameObject prefabVazamentoPrincipal;

    [Header("Ambiente")]
    public GameObject prefabEstacaoTratamento;
    public GameObject prefabColetavelAgua;

    [Header("Efeitos")]
    public GameObject prefabEfeitoReparo;
    public GameObject prefabEfeitoColeta;
    public GameObject prefabEfeitoEducacao;
    public GameObject prefabEfeitoExplosaoAgua;

    [Header("UI")]
    public GameObject prefabFeedbackFlutuante;
    public GameObject prefabBolhaDialogo;

    // ── Acesso rápido por tipo de vazamento ─────────────────────────────────
    public GameObject ObterPrefabVazamento(WaterLeak.TipoVazamento tipo) => tipo switch
    {
        WaterLeak.TipoVazamento.Encanamento => prefabVazamentoEncanamento,
        WaterLeak.TipoVazamento.Hidrante    => prefabVazamentoHidrante,
        WaterLeak.TipoVazamento.Calcada     => prefabVazamentoCalcada,
        WaterLeak.TipoVazamento.Principal   => prefabVazamentoPrincipal,
        _                                  => prefabVazamentoEncanamento,
    };
}
