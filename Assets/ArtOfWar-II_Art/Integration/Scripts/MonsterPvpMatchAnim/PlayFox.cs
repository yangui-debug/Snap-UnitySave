using UnityEngine;

public class PlayFox : MonoBehaviour
{
    [SerializeField] private MonsterPvpMatchAnim monsterPvpMatchAnim;

    private void OnFoxPlay()
    {
        monsterPvpMatchAnim.OnFoxPlay();
    }
}