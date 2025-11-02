using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TowerUpgradable : MonoBehaviour
{
    [Header("Dónde colgar adornos/VFX")]
    public Transform mountPoint; // si no se asigna, usa transform

    private Tower torre;

    // Conteo por tipo para stacking
    private readonly Dictionary<UpgradeModule.TypeUpgrade, int> stacksPorTipo =
        new Dictionary<UpgradeModule.TypeUpgrade, int>();

    // Referencias de VFX persistentes para limpieza manual (opcional)
    private readonly List<GameObject> _persistentVfx = new List<GameObject>();

    void Awake()
    {
        torre = GetComponent<Tower>();
        if (torre == null)
            Debug.LogError($"[{name}] No encontré componente Tower. Agrega 'Tower' al prefab de la torreta.");
    }

    void OnDestroy()
    {
        // Limpieza cuando destruyan la torre o cambies de escena
        for (int i = 0; i < _persistentVfx.Count; i++)
            if (_persistentVfx[i]) Destroy(_persistentVfx[i]);
        _persistentVfx.Clear();
    }

    /// Aplica la mejora leyendo el UpgradeModule del prefab de la carta.
    public void ApplyUpgrade(GameObject upgradePrefab)
    {
        if (torre == null || upgradePrefab == null) return;

        // Lee datos desde el prefab (también busca en hijos)
        var data = upgradePrefab.GetComponent<UpgradeModule>();
        if (data == null) data = upgradePrefab.GetComponentInChildren<UpgradeModule>(true);

        if (data == null)
        {
            Debug.LogWarning($"[{name}] El upgradePrefab '{upgradePrefab.name}' no tiene UpgradeModule (ni en hijos).");
            return;
        }

        // Reglas de acumulación
        stacksPorTipo.TryGetValue(data.type, out int currentStacks);
        if (!data.stackable && currentStacks >= 1)
        {
            Debug.Log($"[{name}] Ya se aplicó {data.type} y no es acumulable.");
            return;
        }
        if (data.stackable && data.maxStacks > 0 && currentStacks >= data.maxStacks)
        {
            Debug.Log($"[{name}] Tope de acumulación para {data.type} ({data.maxStacks}).");
            return;
        }

        // 1) Adorno opcional (queda colgado)
        Transform anchor = mountPoint != null ? mountPoint : transform;
        if (data.cosmeticChild != null)
        {
            var visual = Instantiate(data.cosmeticChild, anchor);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
        }

        // 2) Stats
        ApplyStatUpgrade(data.type, data.amount);

        // 3) VFX persistente (SIN offsets; local 0/0/0; NO se destruye)
        if (data.vfxOnApply != null)
        {
            var vfx = Instantiate(data.vfxOnApply);
            vfx.SetActive(true);

            // Parentar sin conservar mundo y clavar en local 0/0/0
            vfx.transform.SetParent(anchor, worldPositionStays: false);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.identity;
            vfx.transform.localScale   = Vector3.one;

            PlayVfxPersistent(vfx, data);      // reproduce en loop / sin stopAction
            _persistentVfx.Add(vfx);           // guarda referencia por si luego quieres limpiarlo
        }
        else
        {
            Debug.Log($"[{name}] No hay VFX asignado en UpgradeModule '{upgradePrefab.name}'.");
        }

        // 4) Incrementa contador
        stacksPorTipo[data.type] = currentStacks + 1;
    }

    // Fuerza PS en loop y sin auto-destroy; arranca VFX Graph si existe
    private void PlayVfxPersistent(GameObject vfxGo, UpgradeModule data)
    {
        var systems = vfxGo.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            if (data.forceLoop) main.loop = true;
#if UNITY_2021_2_OR_NEWER
            if (data.disableStopAction) main.stopAction = ParticleSystemStopAction.None;
#endif
            if (data.clearOnPlay) ps.Clear(true);
            ps.Play(true);
        }

#if UNITY_VISUAL_EFFECT_GRAPH
        var vfxs = vfxGo.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
        foreach (var v in vfxs)
        {
            v.Reinit();
            v.Play();
        }
#endif
    }

    // Stats -> usa tus métodos ya existentes en Tower
    private void ApplyStatUpgrade(UpgradeModule.TypeUpgrade type, float amount)
    {
        switch (type)
        {
            case UpgradeModule.TypeUpgrade.Damage:
                torre.AddDamage(amount);   break;
            case UpgradeModule.TypeUpgrade.FireRate:
                torre.AddFireRate(amount); break;
            case UpgradeModule.TypeUpgrade.Range:
                torre.AddRange(amount);    break;
        }
    }

    // ===== Opcional: limpiar cuando tú quieras =====
    public void ClearAllUpgradeVfx()
    {
        for (int i = 0; i < _persistentVfx.Count; i++)
            if (_persistentVfx[i]) Destroy(_persistentVfx[i]);
        _persistentVfx.Clear();
    }

    public void ClearLastVfx()
    {
        for (int i = _persistentVfx.Count - 1; i >= 0; i--)
        {
            if (_persistentVfx[i])
            {
                Destroy(_persistentVfx[i]);
                _persistentVfx.RemoveAt(i);
                break;
            }
        }
    }
}
