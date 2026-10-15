using UnityEngine;

public class BattleUnit_43_Exert : MonoBehaviour
{
    [SerializeField] private Transform root = default;

    void Start()
    {
        root.localEulerAngles = new Vector3(Random.Range(0, 360), 90, 90);
    }
}