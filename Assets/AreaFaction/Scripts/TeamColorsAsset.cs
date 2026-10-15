using UnityEngine;

namespace Com.i89.AreaFaction
{
    [CreateAssetMenu(fileName = "TeamColors", menuName = "AreaFaction/Team Colors")]
    public class TeamColorsAsset : ScriptableObject
    {
        [Header("队伍默认颜色")]
        [ColorUsage(true, true)] public Color teamAColor = new Color(1.275f, 0.375f, 0.3f, 1f);   // 红 (A) - 强度 1.5
        [ColorUsage(true, true)] public Color teamBColor = new Color(0.375f, 0.675f, 1.275f, 1f);  // 蓝 (B) - 强度 1.5
        [ColorUsage(true, true)] public Color teamCColor = new Color(0.45f, 1.05f, 0.45f, 1f);     // 绿 (C) - 强度 1.5
        [ColorUsage(true, true)] public Color teamDColor = new Color(1.2f, 1.05f, 0.3f, 1f);      // 黄 (D) - 强度 1.5
        [ColorUsage(true, true)] public Color teamEColor = new Color(1.05f, 0.45f, 1.05f, 1f);     // 紫 (E) - 强度 1.5
        [ColorUsage(true, true)] public Color teamFColor = new Color(0.3f, 1.2f, 1.2f, 1f);       // 青 (F) - 强度 1.5

        public Color GetTeamColor(string teamName)
        {
            if (teamName.Length == 1)
            {
                char c = teamName[0];
                switch (c)
                {
                    case 'A': return teamAColor;
                    case 'B': return teamBColor;
                    case 'C': return teamCColor;
                    case 'D': return teamDColor;
                    case 'E': return teamEColor;
                    case 'F': return teamFColor;
                }
            }
            return Color.white;
        }
    }
}