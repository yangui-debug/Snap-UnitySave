using UnityEngine;
public class LockFPS : MonoBehaviour
{
    void Start()
    {
        QualitySettings.vSyncCount = 0;     // 关垂直同步
        Application.targetFrameRate = 60; // 锁60帧
    }
}