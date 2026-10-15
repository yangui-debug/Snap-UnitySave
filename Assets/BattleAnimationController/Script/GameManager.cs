using UnityEngine;

public class GameManager : MonoBehaviour
{
    private bool gameEnded = false;

    void Update()
    {
        if (gameEnded) return;

        BattleAnimationController[] allUnits = FindObjectsOfType<BattleAnimationController>();

        bool redAlive = false;
        bool blueAlive = false;

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;
            if (unit.team == Team.Red) redAlive = true;
            if (unit.team == Team.Blue) blueAlive = true;
        }

        if (redAlive && !blueAlive)
        {
            Debug.Log("红队胜利！");
            TriggerVictory(Team.Red);
            gameEnded = true;
        }
        else if (blueAlive && !redAlive)
        {
            Debug.Log("蓝队胜利！");
            TriggerVictory(Team.Blue);
            gameEnded = true;
        }
    }

    void TriggerVictory(Team winner)
    {
        BattleAnimationController[] allUnits = FindObjectsOfType<BattleAnimationController>();
        foreach (var unit in allUnits)
        {
            if (unit.team == winner)
            {
                unit.TriggerVictory(); // 播放胜利动画
            }
        }
    }
}
