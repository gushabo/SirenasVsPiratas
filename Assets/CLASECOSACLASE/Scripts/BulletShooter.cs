using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BulletShooter : MonoBehaviour
{


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {

            FxManage.GetInstance().setFX(FX_Type.Bullet, transform.position, transform.rotation);


        }

        if (Input.GetKeyDown(KeyCode.Q))
        {

            FxManage.GetInstance().setFX(FX_Type.Enemy, transform.position, transform.rotation);


        }

        if (Input.GetKeyDown(KeyCode.W))
        {

            FxManage.GetInstance().setFX(FX_Type.Sphere, transform.position, transform.rotation);


        }
    }

  
}
