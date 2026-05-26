using UnityEngine;
using System.Collections;

public class RecargaRapidaSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefabRecargaRapida;

    [SerializeField] private float tiempoReaparicion = 12f;

    [SerializeField] private Vector3[] posicionesSpawn = new Vector3[]
    {
        new Vector3(55f,  0.5f, 60f),
        new Vector3(135f, 0.5f, 60f),
        new Vector3(55f,  0.5f, 140f),
        new Vector3(135f, 0.5f, 140f),
    };

    private GameObject[] instancias;

    void Start()
    {
        instancias = new GameObject[posicionesSpawn.Length];
        for (int i = 0; i < posicionesSpawn.Length; i++)
            SpawnearEn(i);
    }

    void SpawnearEn(int indice)
    {
        if (prefabRecargaRapida == null) return;
        instancias[indice] = Instantiate(prefabRecargaRapida, posicionesSpawn[indice], Quaternion.identity);
        StartCoroutine(VigilarSlot(indice));
    }

    IEnumerator VigilarSlot(int indice)
    {
        while (true)
        {
            yield return new WaitUntil(() => instancias[indice] == null);
            yield return new WaitForSeconds(tiempoReaparicion);
            SpawnearEn(indice);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        foreach (Vector3 pos in posicionesSpawn)
        {
            Gizmos.DrawWireSphere(pos, 1.2f);
            Gizmos.DrawLine(pos, pos + Vector3.up * 2f);
        }
    }
}