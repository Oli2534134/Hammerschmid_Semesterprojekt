using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyType
    {
        Melee,
        Ranged,
        Tank
    }

    public EnemyType type;

    public float health;
    public float speed;
    public float damage;

    void Start()
    {
        SetupEnemy();
    }

    void SetupEnemy()
    {
        switch (type)
        {
            case EnemyType.Melee:
                health = 100;
                speed = 4;
                damage = 10;
                break;

            case EnemyType.Ranged:
                health = 70;
                speed = 3;
                damage = 15;
                break;

            case EnemyType.Tank:
                health = 200;
                speed = 1.5f;
                damage = 25;
                break;
        }
    }
}