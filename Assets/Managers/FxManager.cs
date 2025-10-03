using System;
using System.Collections.Generic;
using UnityEngine;

public class FxManager : MonoBehaviour
{
    public static FxManager instance;
    public static FxManager GetInstance() => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void OnDestroy()
    {
        if (instance == this) instance = null;
    }


    public FxPool[] fx_pools;
    public void SetFX(FxType type)
    {
        for (int i = 0; i < fx_pools.Length; i++)
        {
            if (type == fx_pools[i].type)
            {
                CreateFx(i);
            }
        }
    }

    public void SetFX(FxType type, Vector3 position, Quaternion rotation)
    {
        for (int i = 0; i < fx_pools.Length; i++)
        {
            if (type == fx_pools[i].type)
            {
                GameObject fx = CreateFx(i);
                fx.transform.position = position;
                fx.transform.rotation = rotation;
            }
        }
    }

    public void SetFX(FxType type, Vector3 position, Quaternion rotation, Transform target, int dmg)
    {
        for (int i = 0; i < fx_pools.Length; i++)
        {
            if (type == fx_pools[i].type)
            {
                GameObject fx = CreateFx(i);
                fx.transform.position = position;
                fx.transform.rotation = rotation;
                fx.GetComponent<Bullet>().damage = dmg;
                fx.GetComponent<Bullet>().Shoot(target);
            }
        }
    }

    public GameObject CreateFx(int indexOfPool)
    {
        for (int i = 0; i < fx_pools[indexOfPool].pool.Count; i++)
        {
            if (!fx_pools[indexOfPool].pool[i].activeInHierarchy)
            {
                fx_pools[indexOfPool].pool[i].SetActive(true);
                return fx_pools[indexOfPool].pool[i];
            }
        }

        // ni habia balas chidas miamor
        GameObject newFx = Instantiate(fx_pools[indexOfPool].fxPrefab);
        fx_pools[indexOfPool].pool.Add(newFx);
        return newFx;

    }



}

[Serializable]
public class FxPool
{
    public FxType type;
    public GameObject fxPrefab;
    public List<GameObject> pool = new List<GameObject>();
}


public enum FxType { Bullet, Bomb }
