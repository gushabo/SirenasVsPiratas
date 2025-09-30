using UnityEngine;

public class AutoOff : MonoBehaviour
{

    public float timeToDeactivate = 3;
    float Counter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        Counter = 0;
    }


    // Update is called once per frame
    void Update()
    {
        Counter += Time.deltaTime;
        if(Counter > timeToDeactivate)
        {
            gameObject.SetActive(false);

        }
    }
}
