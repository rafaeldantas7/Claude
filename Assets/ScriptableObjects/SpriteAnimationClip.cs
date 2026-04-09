using UnityEngine;

/// <summary>
/// ScriptableObject que define um clipe de animação por sprites.
/// Criar via: Assets > Create > Cagepa > Clipe de Animação
/// </summary>
[CreateAssetMenu(fileName = "AnimClip_Novo", menuName = "Cagepa/Clipe de Animação")]
public class SpriteAnimationClip : ScriptableObject
{
    [Header("Identificação")]
    public string nome = "idle";

    [Header("Frames")]
    public Sprite[] sprites;

    [Header("Reprodução")]
    [Tooltip("Frames por segundo")]
    [Range(1, 60)]
    public int fps = 8;
    public bool loop = true;

    [Header("Eventos de frame (opcional)")]
    public FrameEvent[] eventosDeFrame;

    [System.Serializable]
    public struct FrameEvent
    {
        public int    frame;
        public string nomeEvento; // Disparado via SendMessage
    }

    // ── Catálogo de nomes padrão ─────────────────────────────────────────────
    // Use estas constantes para evitar erros de digitação.
    public static class Nomes
    {
        // Player
        public const string PlayerIdleDown    = "player_idle_down";
        public const string PlayerIdleUp      = "player_idle_up";
        public const string PlayerIdleSide    = "player_idle_side";
        public const string PlayerWalkDown    = "player_walk_down";
        public const string PlayerWalkUp      = "player_walk_up";
        public const string PlayerWalkSide    = "player_walk_side";
        public const string PlayerReparo      = "player_reparo";
        public const string PlayerEducando    = "player_educando";
        public const string PlayerVitoria     = "player_vitoria";

        // Vazamento
        public const string VazIdle           = "vaz_idle";
        public const string VazReparando      = "vaz_reparando";
        public const string VazConcluido      = "vaz_concluido";

        // Cidadão
        public const string CidadaoIdleDown   = "cidadao_idle_down";
        public const string CidadaoWalkSide   = "cidadao_walk_side";
        public const string CidadaoDesperd    = "cidadao_desperdicio";
        public const string CidadaoEducado    = "cidadao_educado";

        // Estação
        public const string EstacaoDesligada  = "estacao_off";
        public const string EstacaoIniciando  = "estacao_init";
        public const string EstacaoAtiva      = "estacao_on";
        public const string EstacaoSobrecarga = "estacao_overheat";

        // Coletável
        public const string ColetavelIdle     = "colet_idle";
        public const string ColetavelColetado = "colet_coletado";
    }
}
