using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.i89.AreaFaction
{
    public class AreaFactionInvasionExampleGrid : MonoBehaviour
    {
        [Header("公共网格与材质模板")]
        [Tooltip("将你要用的那个统一Mesh拖到这里。代码会自动把它分发给所有的格子")]
        public Mesh sharedMeshTemplate;
        [Tooltip("将你要用的那个统一材质球拖到这里。代码会自动复印两份并染成AB的颜色")]
        public Material sharedMaterialTemplate;

        [Header("阵营格子分配 (初始归属)")]
        [SerializeField] private List<GameObject> factionACells;
        [SerializeField] private List<GameObject> factionBCells;

        [Header("推进节奏")]
        [SerializeField] private float neighborThreshold = 2.1f;
        [SerializeField] private float invasionDuration = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float advanceOverlap = 0.3f;
        [SerializeField] private AnimationCurve invasionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("宏观战局剧本")]
        public bool enableMacroCampaign = true;
        
        [Tooltip("整场战役中，最多出现几次进攻受阻？(建议填 1)")]
        public int maxBlockageCount = 1;
        
        [Tooltip("整场战役中，最多出现几次B阵营反攻？(建议填 1)")]
        public int maxCounterAttackCount = 1;

        [Space(10)]
        [Tooltip("B阵营发起反攻时，最少往回推几格(几层)？")]
        public int minCounterAttackSteps = 2;
        [Tooltip("B阵营发起反攻时，最多往回推几格(几层)？")]
        public int maxCounterAttackSteps = 5;

        [Header("阵营统一颜色")]
        [Tooltip("A 阵营的统一颜色 (日常驻守与进攻波浪色)")]
        public Color factionABaseColor = new Color(0.85f, 0.45f, 0.4f, 1f); 
        [Tooltip("B 阵营的统一颜色 (日常驻守与反推波浪色)")]
        public Color factionBBaseColor = new Color(0.8f, 0.2f, 0.2f, 1f); 

        [Header("视觉微调参数")]
        [Tooltip("侵蚀推进时，最前线那道发光边界的颜色（建议设为高亮的金黄、亮白或浅粉色）")]
        [SerializeField] private Color borderGlowColor = new Color(1f, 0.9f, 0.9f, 1f);
        [Tooltip("发光边界的粗细程度。值越大，光带越宽（建议0.02~0.05之间保持锐利）")]
        [Range(0.01f, 0.2f)] [SerializeField] private float borderWidth = 0.04f;
        [Tooltip("发光边界的亮度倍增。结合后期处理(Bloom)可以产生强烈的泛光效果")]
        [Range(0f, 3f)] [SerializeField] private float borderIntensity = 1.8f;
        [Tooltip("战线前后呼吸/抖动的频率速度。模拟交火时阵线来回拉扯的动态感")]
        [Range(0f, 20f)] [SerializeField] private float flickerSpeed = 8f;
        [Tooltip("战线前后抖动的剧烈程度（幅度）。设为0则匀速推进，毫无抖动")]
        [Range(0f, 0.5f)] [SerializeField] private float flickerAmount = 0.28f;
        [Tooltip("波浪密集度/频率。值越大，边缘越像细碎的锯齿或撕裂状；值越小，越像平缓的大河弯曲")]
        [Range(0f, 5f)] [SerializeField] private float noiseScale = 1f;
        [Tooltip("波浪幅度/深度。值越大，犬牙交错的凹凸穿插感越深；设为0时边界是一条笔直的线")]
        [Range(0f, 5f)] [SerializeField] private float noiseAmount = 2f;
        [Tooltip("交火星火/粒子点阵带的宽度。值越大，粒子散布的范围越广")]
        [Range(0.02f, 0.35f)] [SerializeField] private float battleBandWidth = 0.18f;
        [Tooltip("前线交火粒子的密集程度。值越大，星星点点的火花越多")]
        [Range(1f, 40f)] [SerializeField] private float battleDotDensity = 18f;
        [Tooltip("单个火花/粒子的大小")]
        [Range(0.05f, 0.3f)] [SerializeField] private float battleDotSize = 0.16f;
        [Tooltip("交火粒子的整体发光强度与可见度。设为0则完全隐藏交火粒子特效")]
        [Range(0f, 3f)] [SerializeField] private float battleIntensity = 1.25f;
        [Tooltip("交火粒子沿着战线边缘流动的速度。模拟两军交战时子弹/能量沿前线飞扯的视觉")]
        [Range(0f, 20f)] [SerializeField] private float battleFlowSpeed = 8f;

        [Header("Shader 参数映射")]
        [Tooltip("你的材质主颜色属性名。看截图已确认为 _Maincolor")]
        [SerializeField] private string shaderBaseColorName = "_Maincolor"; // <-- 核心修复点：默认改为了 _Maincolor
        [SerializeField] private string shaderProgressName = "_InvasionProgress";
        [SerializeField] private string shaderCenterName = "_BorderCenter";
        [SerializeField] private string shaderDirName = "_BorderDirection";
        [SerializeField] private string shaderInvasionColorName = "_InvasionNewColor"; 
        [SerializeField] private string shaderRadiusName = "_InvasionTileRadius"; 
        [SerializeField] private string shaderBorderColorName = "_InvasionBorderGlowColor";
        [SerializeField] private string shaderBorderWidthName = "_InvasionBorderWidth";
        [SerializeField] private string shaderBorderIntensityName = "_InvasionBorderIntensity";
        [SerializeField] private string shaderFlickerSpeedName = "_FlickerSpeed";
        [SerializeField] private string shaderFlickerAmountName = "_FlickerAmount";
        [SerializeField] private string shaderNoiseScaleName = "_NoiseScale";
        [SerializeField] private string shaderNoiseAmountName = "_NoiseAmount";
        [SerializeField] private string shaderBattleBandWidthName = "_BattleBandWidth";
        [SerializeField] private string shaderBattleDotDensityName = "_BattleDotDensity";
        [SerializeField] private string shaderBattleDotSizeName = "_BattleDotSize";
        [SerializeField] private string shaderBattleIntensityName = "_BattleIntensity";
        [SerializeField] private string shaderBattleFlowSpeedName = "_BattleFlowSpeed";

        private class TileData
        {
            public GameObject gameObject;
            public int initialFaction; 
            public int faction;        
            public Vector3 worldPosition;
            public MeshRenderer renderer;
            public int currentAnimId = 0; 
            public Material realInitialMaterial; 
        }

        private class CampaignStep
        {
            public List<InvasionTask> tasks;
            public bool isCounterAttack; 
            public float durationMult;   
        }

        private struct InvasionTask { public TileData target; public TileData source; }

        private List<TileData> allTiles = new List<TileData>();
        private bool hasInitialized = false;

        // 全局动态克隆的专属材质
        private Material matFactionA;
        private Material matFactionB;

        private void Start()
        {
            if (!Application.isPlaying) return;
            BuildInitial();
        }

        [ContextMenu("1. 构建数据 (Build Initial)")]
        public void BuildInitial()
        {
            allTiles.Clear();

            // 核心逻辑：复印模板材质，并修改底色
            if (sharedMaterialTemplate != null)
            {
                matFactionA = new Material(sharedMaterialTemplate);
                matFactionA.name = "FactionA_Material";
                // 强制改 _Maincolor
                if (matFactionA.HasProperty(shaderBaseColorName)) matFactionA.SetColor(shaderBaseColorName, factionABaseColor);
                else if (matFactionA.HasProperty("_Maincolor")) matFactionA.SetColor("_Maincolor", factionABaseColor); 

                matFactionB = new Material(sharedMaterialTemplate);
                matFactionB.name = "FactionB_Material";
                // 强制改 _Maincolor
                if (matFactionB.HasProperty(shaderBaseColorName)) matFactionB.SetColor(shaderBaseColorName, factionBBaseColor);
                else if (matFactionB.HasProperty("_Maincolor")) matFactionB.SetColor("_Maincolor", factionBBaseColor); 
            }

            AddTilesToPool(factionACells, 0);
            AddTilesToPool(factionBCells, 1);

            hasInitialized = true;
            Debug.Log($"[AreaFaction] 数据构建完毕。A:{factionACells.Count} B:{factionBCells.Count}");
        }

        private void AddTilesToPool(List<GameObject> list, int faction)
        {
            if (list == null) return;
            foreach (var go in list)
            {
                if (!go) continue;
                
                var mr = go.GetComponent<MeshRenderer>();
                var mf = go.GetComponent<MeshFilter>();
                
                // 统一派发 Mesh
                if (sharedMeshTemplate != null && mf != null) mf.sharedMesh = sharedMeshTemplate;
                
                // 统一派发带有阵营底色的新材质
                if (sharedMaterialTemplate != null && mr != null) 
                {
                    mr.sharedMaterial = faction == 0 ? matFactionA : matFactionB;
                }

                allTiles.Add(new TileData {
                    gameObject = go, initialFaction = faction, faction = faction,
                    worldPosition = go.transform.position, 
                    renderer = mr,
                    realInitialMaterial = mr ? mr.sharedMaterial : null
                });
            }
        }

        [ContextMenu("2. 开始波次侵蚀 (Start)")]
        public void StartSequentialInvasion()
        {
            if (!Application.isPlaying) return;
            if (!hasInitialized) BuildInitial();
            StopAllCoroutines();
            
            foreach (var t in allTiles) t.currentAnimId = 0; 

            int[] virtualFactions = new int[allTiles.Count];
            for (int i = 0; i < allTiles.Count; i++) virtualFactions[i] = allTiles[i].faction;

            List<CampaignStep> finalCampaign = new List<CampaignStep>();
            
            int forwardProgressDepth = 0; 
            int currentBlockageCount = 0;
            int currentCounterAttackCount = 0;
            int lastEventWave = -999;
            
            int safeWaveCount = 2;              
            int minEventInterval = 2;           
            float eventProbability = 0.4f;      
            float counterDurationMult = 2.0f;   

            while (true)
            {
                bool canTriggerEvents = enableMacroCampaign && forwardProgressDepth >= safeWaveCount;

                if (canTriggerEvents && 
                    currentCounterAttackCount < maxCounterAttackCount && 
                    (forwardProgressDepth - lastEventWave) >= minEventInterval &&
                    Random.value < eventProbability)
                {
                    currentCounterAttackCount++;
                    lastEventWave = forwardProgressDepth; 

                    int actualMax = Mathf.Min(maxCounterAttackSteps, forwardProgressDepth);
                    int actualMin = Mathf.Min(minCounterAttackSteps, actualMax);
                    int pushBackSteps = Random.Range(actualMin, actualMax + 1);

                    for (int s = 0; s < pushBackSteps; s++)
                    {
                        List<InvasionTask> counterTasks = GetDynamicFrontier(virtualFactions, attackerFaction: 1, defenderFaction: 0);
                        if (counterTasks.Count == 0) break; 

                        finalCampaign.Add(new CampaignStep { tasks = counterTasks, isCounterAttack = true, durationMult = counterDurationMult });
                        foreach (var task in counterTasks) virtualFactions[allTiles.IndexOf(task.target)] = 1;
                        forwardProgressDepth--; 
                    }
                    continue; 
                }

                List<InvasionTask> forwardTasks = GetDynamicFrontier(virtualFactions, attackerFaction: 0, defenderFaction: 1);
                if (forwardTasks.Count == 0) break; 

                float fwdMult = 1f;
                if (canTriggerEvents && 
                    currentBlockageCount < maxBlockageCount && 
                    (forwardProgressDepth - lastEventWave) >= minEventInterval &&
                    Random.value < eventProbability)
                {
                    currentBlockageCount++;
                    lastEventWave = forwardProgressDepth; 
                    fwdMult = Random.Range(2.0f, 4.0f); 
                }

                finalCampaign.Add(new CampaignStep { tasks = forwardTasks, isCounterAttack = false, durationMult = fwdMult });
                foreach (var task in forwardTasks) virtualFactions[allTiles.IndexOf(task.target)] = 0;
                forwardProgressDepth++;
            }

            if (finalCampaign.Count > 0) StartCoroutine(WaveRunner(finalCampaign));
        }

        private List<InvasionTask> GetDynamicFrontier(int[] virtualFactions, int attackerFaction, int defenderFaction)
        {
            List<InvasionTask> frontier = new List<InvasionTask>();
            for (int targetIdx = 0; targetIdx < allTiles.Count; targetIdx++)
            {
                if (virtualFactions[targetIdx] == defenderFaction)
                {
                    TileData targetTile = allTiles[targetIdx];
                    TileData bestSourceTile = null;
                    float minDistance = float.MaxValue;

                    for (int sourceIdx = 0; sourceIdx < allTiles.Count; sourceIdx++)
                    {
                        if (virtualFactions[sourceIdx] == attackerFaction)
                        {
                            float dist = Vector3.Distance(targetTile.worldPosition, allTiles[sourceIdx].worldPosition);
                            if (dist <= neighborThreshold && dist < minDistance)
                            {
                                minDistance = dist;
                                bestSourceTile = allTiles[sourceIdx];
                            }
                        }
                    }
                    if (bestSourceTile != null) frontier.Add(new InvasionTask { target = targetTile, source = bestSourceTile });
                }
            }
            return frontier;
        }

        private IEnumerator WaveRunner(List<CampaignStep> campaign)
        {
            foreach (var step in campaign)
            {
                foreach (var task in step.tasks) StartCoroutine(AnimateSingleTile(task.target, task.source, step.isCounterAttack, step.durationMult));
                float currentWaveDuration = invasionDuration * step.durationMult;
                float waitTime = currentWaveDuration * (1.0f - advanceOverlap);
                yield return new WaitForSeconds(Mathf.Max(0.01f, waitTime));
            }
        }

        private IEnumerator AnimateSingleTile(TileData target, TileData source, bool isCounterAttack, float durationMult)
        {
            if (!target.renderer) yield break;

            target.currentAnimId++;
            int myAnimId = target.currentAnimId;

            // 获取应该派发的材质和波浪色
            Material expectedDefenderMat = isCounterAttack ? matFactionA : matFactionB;
            Material expectedAttackerMat = isCounterAttack ? matFactionB : matFactionA;
            Color waveColor = isCounterAttack ? factionBBaseColor : factionABaseColor; 
            Color defenderColor = isCounterAttack ? factionABaseColor : factionBBaseColor;

            // 动画前，确保它是防守方材质
            if (sharedMaterialTemplate != null) target.renderer.sharedMaterial = expectedDefenderMat;

            Vector2 sPos = new Vector2(source.worldPosition.x, source.worldPosition.z);
            Vector2 tPos = new Vector2(target.worldPosition.x, target.worldPosition.z);
            Vector2 center = (sPos + tPos) * 0.5f;
            Vector2 forward = (tPos - sPos).normalized; 
            Vector2 borderDir = new Vector2(forward.y, -forward.x);

            float actualRadius = Vector3.Distance(source.worldPosition, target.worldPosition) * 0.5f;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            float actualDuration = invasionDuration * durationMult;
            float timer = 0f;

            while (timer < actualDuration)
            {
                if (target.currentAnimId != myAnimId) yield break;
                timer += Time.deltaTime;
                float p = invasionCurve.Evaluate(timer / actualDuration);

                target.renderer.GetPropertyBlock(mpb);
                mpb.SetFloat(shaderProgressName, p);
                mpb.SetColor(shaderInvasionColorName, waveColor); // 波浪色直接读取攻击方的底色
                
                // 保证底色一直为防守方颜色
                mpb.SetColor(shaderBaseColorName, defenderColor); 

                mpb.SetFloat(shaderRadiusName, actualRadius);
                mpb.SetVector(shaderCenterName, new Vector4(center.x, 0, center.y, 0));
                mpb.SetVector(shaderDirName, new Vector4(borderDir.x, 0, borderDir.y, 0));
                mpb.SetColor(shaderBorderColorName, borderGlowColor);
                mpb.SetFloat(shaderBorderWidthName, borderWidth);
                mpb.SetFloat(shaderBorderIntensityName, borderIntensity);
                mpb.SetFloat(shaderFlickerSpeedName, flickerSpeed);
                mpb.SetFloat(shaderFlickerAmountName, flickerAmount);
                mpb.SetFloat(shaderNoiseScaleName, noiseScale);
                mpb.SetFloat(shaderNoiseAmountName, noiseAmount);
                mpb.SetFloat(shaderBattleBandWidthName, battleBandWidth);
                mpb.SetFloat(shaderBattleDotDensityName, battleDotDensity);
                mpb.SetFloat(shaderBattleDotSizeName, battleDotSize);
                mpb.SetFloat(shaderBattleIntensityName, battleIntensity);
                mpb.SetFloat(shaderBattleFlowSpeedName, battleFlowSpeed);
                
                target.renderer.SetPropertyBlock(mpb);
                yield return null;
            }

            if (target.currentAnimId != myAnimId) yield break;
            
            // 动画结束，清除所有波浪数据，直接换上进攻方的纯净材质
            target.renderer.GetPropertyBlock(mpb);
            mpb.Clear(); 
            target.renderer.SetPropertyBlock(mpb);

            target.faction = isCounterAttack ? 1 : 0;
            if (sharedMaterialTemplate != null) target.renderer.sharedMaterial = expectedAttackerMat;
        }

        [ContextMenu("3. 重置格子 (Reset Grid)")]
        public void ResetGrid()
        {
            StopAllCoroutines();
            if (allTiles == null || allTiles.Count == 0) return;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            foreach (var t in allTiles)
            {
                if (t.renderer) {
                    t.faction = t.initialFaction;
                    if (sharedMaterialTemplate != null)
                        t.renderer.sharedMaterial = t.faction == 0 ? matFactionA : matFactionB;
                    else
                        t.renderer.sharedMaterial = t.realInitialMaterial;
                    
                    t.renderer.GetPropertyBlock(mpb);
                    mpb.Clear(); 
                    t.renderer.SetPropertyBlock(mpb);
                }
                t.currentAnimId = 0;
            }
        }
    }
}