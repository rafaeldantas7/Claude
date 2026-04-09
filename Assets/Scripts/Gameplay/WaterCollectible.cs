using UnityEngine;

/// <summary>
/// Coletável de água — reabastecer o reservatório quando coletado pelo jogador.
/// Aparece no mapa como bônus durante a fase.
/// </summary>
public class WaterCollectible : MonoBehaviour
{
    [SerializeField] private float quantidadeAgua = 10f;
    [SerializeField] private int   pontosBônus    = 25;
    [SerializeField] private GameObject efeitoColeta;
    [SerializeField] private AudioClip  somColeta;

    private bool coletado = false;

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (coletado) return;
        if (!outro.CompareTag("Player")) return;

        coletado = true;

        CityWaterSystem.Instance?.AdicionarAgua(quantidadeAgua);
        GameManager.Instance?.AdicionarPontos(pontosBônus);
        AudioManager.Instance?.TocarEfeito("coleta_agua");
        UIManager.Instance?.MostrarFeedback($"+{quantidadeAgua}L de água coletada!");

        if (efeitoColeta != null)
        {
            var efeito = Instantiate(efeitoColeta, transform.position, Quaternion.identity);
            Destroy(efeito, 1.5f);
        }

        Destroy(gameObject);
    }

    // Animação de flutuação
    void Update()
    {
        transform.position += Vector3.up * Mathf.Sin(Time.time * 2f) * 0.002f;
    }
}
