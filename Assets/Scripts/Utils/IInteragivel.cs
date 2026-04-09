/// <summary>
/// Interface para todos os objetos com os quais o jogador pode interagir.
/// Implementada por: WaterLeak, CitizenController, WaterTreatmentStation.
/// </summary>
public interface IInteragivel
{
    /// <summary>Retorna true se o objeto pode ser interagido agora.</summary>
    bool PodeInteragir();

    /// <summary>Texto de dica exibido ao jogador quando estiver próximo.</summary>
    string ObterDescricao();
}
