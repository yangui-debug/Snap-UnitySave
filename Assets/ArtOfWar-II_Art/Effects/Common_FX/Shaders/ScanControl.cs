using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ScanControl : MonoBehaviour
{
    [SerializeField] private Material targetMat;
    [SerializeField] private float manualProgress = 0.0f;
    
    // Update is called once per frame
    void Update()
    {
        targetMat.SetFloat("_ManualProgress", manualProgress); // 10秒循环一次
    }
}
