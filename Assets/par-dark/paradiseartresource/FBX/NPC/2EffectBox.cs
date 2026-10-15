using UnityEngine;

namespace Tool
{
    public class EffectBox : MonoBehaviour
    {
        [Header("效果对象")] [SerializeField] private GameObject effectPrefab;
        [Header("复制数量")] [SerializeField] private int numberOfEffects = 5;
        [Header("爆炸力度")] [SerializeField] private float explosionForce = 8f;
        [Header("消失时间")] [SerializeField] private float lifeTime = 1f;
        [Header("方向是否随机")] [SerializeField] private bool isRandom = true;
        [Header("方向设置")] [SerializeField] private int dir = 0;
        [Header("爆炸预制大小")] [SerializeField] private float explosionSize = 1f;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Car"))
            {
                return;
            }
            gameObject.SetActive(false);
            for (int i = 0; i < numberOfEffects; i++)
            {
                GameObject effectInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity);
                effectInstance.transform.localScale = Vector3.one * explosionSize;
                Rigidbody rb = effectInstance.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = effectInstance.AddComponent<Rigidbody>();
                }

                Vector3 explosionDirection = isRandom ? GetRandomValue() : GetDirValue();
                rb.AddForce(explosionDirection * explosionForce, ForceMode.Impulse);
                effectInstance.SetActive(true);
                Destroy(effectInstance, lifeTime);
            }
        }

        private Vector3 GetRandomValue()
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            return (randomDirection + new Vector3(0, Mathf.Tan(Mathf.Deg2Rad * 75), 0)).normalized;
        }

        private Vector3 GetDirValue()
        {
            // 定义四个方向的基础向量
            Vector3[] baseDirections = new Vector3[]
            {
                Vector3.forward,
                Vector3.back,
                Vector3.left,
                Vector3.right
            };

            // 随机选择一个方向
            Vector3 selectedBaseDirection = baseDirections[dir];

            // 随机生成绕该方向的角度
            float randomAngle = Random.Range(-45, 45);
            Quaternion rotation = Quaternion.AngleAxis(randomAngle, selectedBaseDirection);

            // 计算向上75度的方向向量（在该方向的基础上）
            Vector3 upwardDirection = new Vector3(0, Mathf.Tan(Mathf.Deg2Rad * 75), 0);
            return rotation * (selectedBaseDirection + upwardDirection).normalized;
        }
    }
}