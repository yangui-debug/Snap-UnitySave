using UnityEngine;

public class MonsterScript : MonoBehaviour
{
    // 怪物的生命值等其他内容
    public int health = 100;

    // 处理生命值的减少等逻辑
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject); // 销毁怪物
        }
    }
}
