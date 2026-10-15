using UnityEngine;

public class BillboardNew : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }
}
