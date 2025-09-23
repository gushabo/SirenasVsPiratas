using UnityEngine;

public class TowerUpgradable : MonoBehaviour
{
    public Transform mountPoint;

    [SerializeField]
    private TypeUpgrade upgradeType;

    private Tower Torreta;

    public enum TypeUpgrade
    {
        Damage,
        FireRate,
        Range
    }

    void Start()
    {
        Torreta = GetComponent<Tower>();
    }

    public void ApplyUpgrade(GameObject upgradePrefab)
    {
        Transform parent = mountPoint != null ? mountPoint : transform;

        var module = Instantiate(upgradePrefab, parent);
        module.transform.localPosition = Vector3.zero;
        module.transform.localRotation = Quaternion.identity;
        module.transform.localScale = Vector3.one;

        ApplyStatUpgrade(upgradeType, module); 
    }

    private void ApplyStatUpgrade(TypeUpgrade type, GameObject upgradePrefab)
    {
        switch (type)
        {
            case TypeUpgrade.Damage:
                Torreta.damageBullet += 15;
                break;
            case TypeUpgrade.FireRate:
                Torreta.fireRate += 0.5f;
                break;
            case TypeUpgrade.Range:
                Torreta.range += 1f;
                break;
            default:
                Debug.LogWarning("NBo exoiaodiwdjoiakróuooe");
                break;
        }
    }

   
}
