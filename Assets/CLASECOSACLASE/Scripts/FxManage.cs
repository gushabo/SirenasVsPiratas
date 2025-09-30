using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class FxManage : MonoBehaviour
{

    public FX_Pool[] Fx_Pool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   


   private static FxManage instance;

   
   

    private void Awake()
    {
        instance = this;
    }

    public static FxManage GetInstance() => instance;

    public void setFX(FX_Type type)
    {
        for (int i = 0; i < Fx_Pool.Length; i++)
        {
            if(Fx_Pool[i].type == type)
            {

                CreateFX(i);
            }

        }

    }

    public void setFX(FX_Type type, Vector3 _pos, Quaternion _rot)
    {
        for (int i = 0; i < Fx_Pool.Length; i++)
        {
            if (Fx_Pool[i].type == type)
            {

                CreateFX(i);
            }

        }

    }

    GameObject CreateFX(int _indexofPool)
    {
        for (int i = 0; i < Fx_Pool[_indexofPool].pool.Count; i++)
        {
            if (!Fx_Pool[_indexofPool].pool[i].activeInHierarchy)
            {

               
                Fx_Pool[_indexofPool].pool[i].SetActive(true);
                return Fx_Pool[_indexofPool].pool[i];
            }

        }

        GameObject newFx = Instantiate(Fx_Pool[_indexofPool].Prefab);
        Fx_Pool[_indexofPool].pool.Add(newFx);
        return newFx;
    }



}

[Serializable]
public class FX_Pool
{

    public FX_Type type;
    public GameObject Prefab;
    public List<GameObject> pool = new List<GameObject>();

}

public enum FX_Type
{
    Bullet,
    Enemy,
    Sphere

}
