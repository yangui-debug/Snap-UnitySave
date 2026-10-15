using UnityEngine;

public class KnockbackLauncher : MonoBehaviour
{
    public Transform[] targets; // 被撞飞的一排单位
    public float forceY = 15f;  // 向上推力
    public float forceZ = 8f;   // 向后推力
    public float destroyDelay = 2.5f;

    public void LaunchAll()
    {
        foreach (var t in targets)
        {
            Rigidbody rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(new Vector3(0, forceY, forceZ), ForceMode.Impulse);
                StartCoroutine(AutoHide(t.gameObject));
            }
        }
    }

    private System.Collections.IEnumerator AutoHide(GameObject obj)
    {
        yield return new WaitForSeconds(destroyDelay);
        // 可选择销毁、隐藏或对象池回收
        obj.SetActive(false); 
        // Destroy(obj); // 或销毁
    }
}
