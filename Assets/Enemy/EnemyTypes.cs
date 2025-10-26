using UnityEngine;

[CreateAssetMenu(fileName = "EnemyType", menuName = "Scriptable Objects/EnemyType")]
public class EnemyTypes : ScriptableObject
{
    public int health;
    public int damage;
    public int speed;
    public Sprite image;
}
