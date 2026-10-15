using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public enum DrawMatchType
{
    human = 1,
    monster = 2,
}

public class MonsterPvpMatchAnim : MonoBehaviour
{
    [SerializeField] private Animator ground;
    [SerializeField] private Animator monsterSuccess;
    [SerializeField] private Animator humanSuccess;
    [SerializeField] private SkeletonGraphic humanWalking;
    [SerializeField] private SkeletonGraphic monsterWalking;
    [SerializeField] private GameObject humanHouse;
    [SerializeField] private GameObject monsterHouse;
    [SerializeField] private Button matchBtn;
    [SerializeField] private GameObject fox;

    public void Setup(int matchType)
    {
        ground.enabled = true;
        fox.SetActive(false);
        OnMatchEnable((DrawMatchType) matchType);
    }

    private void OnMatchEnable(DrawMatchType matchType)
    {
        bool isMonster = matchType == DrawMatchType.monster;

        humanSuccess.gameObject.SetActive(isMonster);
        monsterWalking.gameObject.SetActive(isMonster);
        monsterSuccess.gameObject.SetActive(!isMonster);
        humanWalking.gameObject.SetActive(!isMonster);

        monsterHouse.SetActive(isMonster);
        humanHouse.SetActive(!isMonster);
    }

    public void OnMatchSuccess(int matchType)
    {
        ground.enabled = false;
        switch ((DrawMatchType) matchType)
        {
            case DrawMatchType.monster:
                humanSuccess.Play("human");

                // 将第 0 层的动画设置为空动画并停止
                monsterWalking.AnimationState.ClearTrack(0);
                break;
            case DrawMatchType.human:
                monsterSuccess.Play("monster");

                // 将第 0 层的动画设置为空动画并停止
                humanWalking.AnimationState.ClearTrack(0);
                break;
        }
    }

    public void OnFoxPlay()
    {
        fox.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}