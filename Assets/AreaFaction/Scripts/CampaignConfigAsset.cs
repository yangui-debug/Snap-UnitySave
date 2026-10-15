using UnityEngine;
using System.Collections.Generic;

namespace Com.i89.AreaFaction
{
    [CreateAssetMenu(fileName = "CampaignConfig", menuName = "AreaFaction/Campaign Config")]
    public class CampaignConfigAsset : ScriptableObject
    {
        [Header("阵营配置")]
        public List<AreaFactionMultiCampaignGrid.FactionConfig> factions = new List<AreaFactionMultiCampaignGrid.FactionConfig>();

        [Header("剧本配置")]
        public List<AreaFactionMultiCampaignGrid.CampaignPhase> campaignScript = new List<AreaFactionMultiCampaignGrid.CampaignPhase>();

        [Header("配置信息")]
        public string configName = "默认配置";
        public string description = "";
        public string creator = "";
        public System.DateTime creationDate = System.DateTime.Now;

        public void SaveFromGrid(AreaFactionMultiCampaignGrid grid)
        {
            if (grid == null)
                return;

            // 保存阵营配置
            factions.Clear();
            foreach (var faction in grid.factions)
            {
                factions.Add(faction);
            }

            // 保存剧本配置
            campaignScript.Clear();
            foreach (var phase in grid.campaignScript)
            {
                campaignScript.Add(phase);
            }

            creationDate = System.DateTime.Now;
        }

        public void LoadToGrid(AreaFactionMultiCampaignGrid grid)
        {
            if (grid == null)
                return;

            // 加载阵营配置
            grid.factions.Clear();
            foreach (var faction in factions)
            {
                grid.factions.Add(faction);
            }

            // 加载剧本配置
            grid.campaignScript.Clear();
            foreach (var phase in campaignScript)
            {
                grid.campaignScript.Add(phase);
            }
        }
    }
}