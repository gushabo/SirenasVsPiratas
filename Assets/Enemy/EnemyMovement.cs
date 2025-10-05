using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public List<Transform> targets = new List<Transform>();
    public int currentTarget = 0;
    public float speed;
    public float turnSpeed = 10f;
    public bool isPaused = false;
    
    private void Start()
    {
        GameManager.GetInstance().onChangeGameState += OnChangeGameStateCallback;
        if(GameManager.GetInstance().gameState == GameState.Pause) isPaused = true;
        currentTarget = 0;
        if (targets == null || targets.Count == 0)
        {
            enabled = false;
        }
    }

    public void OnChangeGameStateCallback(GameState newState)
    {
        isPaused = newState != GameState.Play;
    }

    private void Update()
    {

        if (currentTarget >= targets.Count || isPaused) return;
        Vector3 destination = targets[currentTarget].position;
        
        // Rotacion
        Vector3 toDest = destination - transform.position;
        Vector3 flat = new Vector3(toDest.x, 0f, toDest.z);
        if (flat.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
        
        // Moverse
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        const float arrive = 0.1f;
        if ((transform.position - destination).sqrMagnitude < arrive * arrive)
        {
            currentTarget++;
            if (currentTarget < targets.Count)
                FaceTowards(targets[currentTarget].position);
        }
    }
    
    void FaceTowards(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f; // yaw solamente
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
    
}
