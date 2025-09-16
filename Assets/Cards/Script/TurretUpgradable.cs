using UnityEngine;

public class TowerUpgradable : MonoBehaviour
{
    // Nivel actual por si quieres limitar/mostrar UI
    public int level = 0;
    public int maxLevel = 3;

    // Punto donde “montar” la mejora (si no se asigna, usa transform)
    public Transform mountPoint;

    public void ApplyUpgrade(GameObject upgradePrefab)
    {
        if (level >= maxLevel) { Debug.Log("Nivel máximo alcanzado."); return; }

        Transform parent = mountPoint != null ? mountPoint : transform;

        // Puedes destruir módulo anterior si quieres una sola pieza
        // foreach (Transform child in parent) Destroy(child.gameObject);

        var module = Instantiate(upgradePrefab, parent);
        module.transform.localPosition = Vector3.zero;
        module.transform.localRotation = Quaternion.identity;
        module.transform.localScale = Vector3.one;

        level++;

        // Aquí pon lógica real de mejora (daño, rango, cadencia, etc.)
        // GetComponent<TuTorre>()?.ApplyStats(level);
    }
}
