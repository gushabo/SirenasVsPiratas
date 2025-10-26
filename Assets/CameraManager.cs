using UnityEngine;

public class CameraManager : MonoBehaviour
{
    // ----- SingleTon ---------
    #region Singleton
    public static CameraManager instance { get; private set; }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public static CameraManager GetInstance() => instance;

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
        
    }
    #endregion
    // ------ Fin del singleton  ---------
}
