using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public List<Transform> targets = new List<Transform>();
    public int currentTarget = 0;
    public float speed;
    public bool isPaused = false;
    
    private void Start()
    {
        GameManager.GetInstance().onChangeGameState += OnChangeGameStateCallback;
        
        currentTarget = 0;
        if (targets == null || targets.Count == 0)
        {
            enabled = false;
            return;
        }
        
        transform.position = targets[0].position;
        currentTarget++;
    }

    public void OnChangeGameStateCallback(GameState newState)
    {
        isPaused = newState != GameState.Play;
    }

    private void Update()
    {

        if (currentTarget >= targets.Count || isPaused) return;
        Vector3 destination = targets[currentTarget].position;
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        if ((transform.position - destination).sqrMagnitude < 0.1f)
        {
            currentTarget++;
        }

        if (currentTarget >= targets.Count)
        {
            currentTarget = 0;
        }
    }
}
