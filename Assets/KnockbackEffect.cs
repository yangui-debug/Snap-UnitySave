using UnityEngine;

public class KnockbackEffect : MonoBehaviour
{
    public float knockbackForce = 10f; 

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();

        if (rb != null)
        {

            Vector3 direction = (collision.transform.position - transform.position).normalized;


            rb.AddForce(direction * knockbackForce, ForceMode.Impulse);
        }
    }
}
