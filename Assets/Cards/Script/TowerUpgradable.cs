using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TowerUpgradable : MonoBehaviour
{
    public Transform mountPoint;   // d�nde �colgar� el adorno visual (si existe)
    private Tower torre;           // tu script de torreta (debe tener damageBullet, fireRate, range)

    // Lleva conteo por tipo para stacking/duplicados
    private readonly Dictionary<TypeUpgrade, int> stacksPorTipo =
     new Dictionary<TypeUpgrade, int>();

    public enum TypeUpgrade { Damage, FireRate, Range }

    void Awake()
    {
        // Mejor en Awake para usarlo si la mejora llega en el primer frame
        torre = GetComponent<Tower>();
        if (torre == null)
            Debug.LogError($"[{name}] No encontr� componente Tower. Agrega 'Tower' al prefab de la torreta.");
    }

  
    public void ApplyUpgrade(GameObject upgradePrefab)
    {
        if (torre == null || upgradePrefab == null) return;

        // Lee los datos desde el prefab
        var data = upgradePrefab.GetComponent<UpgradeModule>();
        if (data == null)
        {
            Debug.LogWarning($"[{name}] El upgradePrefab no tiene UpgradeModule. Se ignora.");
            return;
        }

        // Reglas de acumulaci�n
        stacksPorTipo.TryGetValue(data.type, out int currentStacks);

        if (!data.stackable && currentStacks >= 1)
        {
            Debug.Log($"[{name}] Ya se aplic� una mejora de tipo {data.type}. No es acumulable.");
            return;
        }
        if (data.stackable && data.maxStacks > 0 && currentStacks >= data.maxStacks)
        {
            Debug.Log($"[{name}] Tope de acumulaci�n alcanzado para {data.type} ({data.maxStacks}).");
            return;
        }

        // 1) Visual (opcional)
        Transform parent = mountPoint != null ? mountPoint : transform;
        if (data.cosmeticChild != null)
        {
            var visual = Instantiate(data.cosmeticChild, parent);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
        }
        else
        {
            // Si prefieres instanciar TODO el prefab como adorno, descomenta:
            // var visual = Instantiate(upgradePrefab, parent);
            // visual.transform.localPosition = Vector3.zero;
            // visual.transform.localRotation = Quaternion.identity;
            // visual.transform.localScale = Vector3.one;
        }

        // 2) Stats
        ApplyStatUpgrade(data.type, data.amount);

        // 3) Incrementa contador
        stacksPorTipo[data.type] = currentStacks + 1;
    }

    // En TowerUpgradable.cs
    private void ApplyStatUpgrade(TypeUpgrade type, float amount)
    {
        switch (type)
        {
            case TypeUpgrade.Damage:
                torre.AddDamage(amount);     // antes: torre.damageBullet += amount;
                break;
            case TypeUpgrade.FireRate:
                torre.AddFireRate(amount);   // antes: torre.fireRate += amount;
                break;
            case TypeUpgrade.Range:
                torre.AddRange(amount);      // antes: torre.range += amount; // sin retarget inmediato
                break;
        }
    }

}
