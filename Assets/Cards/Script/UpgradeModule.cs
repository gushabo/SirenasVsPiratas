using UnityEngine;

[DisallowMultipleComponent]
public class UpgradeModule : MonoBehaviour
{
    public enum TypeUpgrade { Damage, FireRate, Range }

    [Header("Tipo y magnitud")]
    public TypeUpgrade type = TypeUpgrade.Damage;
    public float amount = 1f;

    [Header("Stacking")]
    public bool stackable = true;
    [Min(0)] public int maxStacks = 0; // 0 = sin tope

    [Header("Adorno visual opcional (queda colgado)")]
    public GameObject cosmeticChild;

    [Header("VFX al aplicar (PERSISTENTE)")]
    public GameObject vfxOnApply;
    public bool vfxFollowTarget = true;

    [Header("Ajustes de posición/orientación (Locales al mountPoint)")]
    public Vector3 vfxLocalOffset = Vector3.zero;
    public Vector3 vfxLocalEuler = Vector3.zero;
    public Vector3 vfxLocalScale = Vector3.one;
    public bool useAnchorRotation = true;

    [Header("Forzar persistencia de ParticleSystem")]
    public bool forceLoop = true;          // pone PS.main.loop = true
    public bool clearOnPlay = true;        // PS.Clear() antes de Play
    public bool disableStopAction = true;  // PS.main.stopAction = None (evita Destroy)
}