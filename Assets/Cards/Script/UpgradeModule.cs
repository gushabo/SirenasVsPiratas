// UpgradeModule.cs
using UnityEngine;

[DisallowMultipleComponent]
public class UpgradeModule : MonoBehaviour
{
    public TowerUpgradable.TypeUpgrade type = TowerUpgradable.TypeUpgrade.Damage;

    [Header("Valores de mejora")]
    public float amount = 10f;         // +daño, +rango, +fireRate (según tipo)
    public bool stackable = true;      // ¿Se puede aplicar varias veces?
    public int maxStacks = 3;          // tope de acumulación (si aplica)

    [Header("Visual")]
    public GameObject cosmeticChild;   // opcional: si quieres un modelo/adorno
}
