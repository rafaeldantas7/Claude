using UnityEngine;

/// <summary>
/// Câmera que segue o jogador suavemente, com limites definidos pelo nível.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Alvo")]
    [SerializeField] private Transform alvo;
    [SerializeField] private Vector3   offset = new(0, 0, -10f);

    [Header("Suavização")]
    [SerializeField] private float velocidadeSuave = 5f;

    [Header("Limites do Mapa")]
    [SerializeField] private bool  usarLimites = true;
    [SerializeField] private float limiteEsq   = -18f;
    [SerializeField] private float limiteDir   =  18f;
    [SerializeField] private float limiteBaixo = -10f;
    [SerializeField] private float limiteCima  =  10f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (alvo == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) alvo = player.transform;
        }
    }

    void LateUpdate()
    {
        if (alvo == null) return;

        Vector3 destino = alvo.position + offset;

        if (usarLimites)
        {
            float meia = cam.orthographicSize;
            float meiaW = meia * cam.aspect;
            destino.x = Mathf.Clamp(destino.x, limiteEsq + meiaW, limiteDir - meiaW);
            destino.y = Mathf.Clamp(destino.y, limiteBaixo + meia, limiteCima - meia);
        }

        transform.position = Vector3.Lerp(transform.position, destino,
                                           velocidadeSuave * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        if (!usarLimites) return;
        Gizmos.color = Color.green;
        Vector3 centro = new((limiteEsq + limiteDir) / 2f, (limiteBaixo + limiteCima) / 2f, 0);
        Vector3 tamanho = new(limiteDir - limiteEsq, limiteCima - limiteBaixo, 0);
        Gizmos.DrawWireCube(centro, tamanho);
    }
}
