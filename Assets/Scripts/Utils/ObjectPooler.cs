using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pool de objetos genérico para evitar Instantiate/Destroy frequentes.
/// Usado para efeitos de partícula, textos flutuantes e projéteis.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string   tag;
        public GameObject prefab;
        public int      tamanho;
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> dicionarioPools = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var pool in pools)
        {
            var fila = new Queue<GameObject>();
            for (int i = 0; i < pool.tamanho; i++)
            {
                var obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                fila.Enqueue(obj);
            }
            dicionarioPools[pool.tag] = fila;
        }
    }

    public GameObject Obter(string tag, Vector3 posicao, Quaternion rotacao)
    {
        if (!dicionarioPools.TryGetValue(tag, out var fila))
        {
            Debug.LogWarning($"[ObjectPooler] Pool '{tag}' não encontrada.");
            return null;
        }

        var obj = fila.Dequeue();
        obj.SetActive(true);
        obj.transform.SetPositionAndRotation(posicao, rotacao);

        fila.Enqueue(obj);
        return obj;
    }

    public void Devolver(string tag, GameObject obj)
    {
        obj.SetActive(false);
    }
}
