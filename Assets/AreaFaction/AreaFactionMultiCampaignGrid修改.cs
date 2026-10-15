using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.i89.AreaFaction
{
    public class AreaFactionMultiCampaignGrid : MonoBehaviour
    {
        [System.Serializable]
        public class FactionConfig
        {
            [Tooltip("方便你在面板里辨认的阵营名字")] public string factionName = "阵营名称";

            [Tooltip("该阵营日常驻守的底色 (支持HDR高亮！进攻时也用作波浪色)")] [ColorUsage(true, true)]
            public Color baseColor = Color.white;

            [Tooltip("分配给该阵营的初始格子（可以直接拖拽包含多个格子的父物体！）")]
            public List<GameObject> initialCells;

            [HideInInspector] public Material runtimeMat;
        }

        [System.Serializable]
        public class CampaignPhase
        {
            [Tooltip("要吞并的目标阵营ID (对应上方列表带的 ID 数字)")]
            public int targetFactionIndex = 1;

            [Tooltip("发动进攻前，休整/间隔多长时间？(秒)")] public float delayBeforeAttack = 1.0f;

            [Tooltip("该战役阶段的推进耗时倍率 (必须大于0！推荐填 1)")]
            public float durationMult = 1.0f;

            [Space(5)] [Tooltip("此次进攻是否可以触发受阻")]
            public bool allowBlockage = true;

            [Tooltip("此次进攻是否可以触发反攻")]
            public bool allowCounterAttack = false;
        }

        [Tooltip("是否展示地图上的分队Id")]
        [SerializeField] private bool OpenMapId = true;

        [Header("公共网格与材质模板")] public Mesh sharedMeshTemplate;
        public Material sharedMaterialTemplate;

        [Header("多阵营配置 (索引0永远是进攻方)")] public List<FactionConfig> factions = new List<FactionConfig>();

        [Header("宏观吞并剧本 (剧本推演序列)")] public List<CampaignPhase> campaignScript = new List<CampaignPhase>();

        [Header("宏观战局剧本 - 随机事件 (纯概率触发)")] [Tooltip("是否开启大战略的随机事件推演(受阻/反击)")]
        public bool enableMacroCampaign = true;

        [Tooltip("每次开始攻打新阵营时，前几波绝对安全不触发事件")] public int safeWaveCount = 3;
        [Tooltip("事件冷却：两次突发事件之间至少间隔几波平推")] public int minEventInterval = 2;

        [Space(10)] [Tooltip("触发【进攻受阻】的单步概率 (建议0.15左右)")] [Range(0f, 1f)]
        public float blockageProbability = 0.15f;

        [Tooltip("【每场局部战争】最多受阻几次")] public int maxBlockageCountPerPhase = 1;
        [Tooltip("受阻时原地焦灼的最少波次（最少受阻格子数）")] public int minBlockageSteps = 2;
        [Tooltip("受阻时原地焦灼的最多波次（最多受阻格子数）")] public int maxBlockageSteps = 4;

        [Space(10)] [Tooltip("触发【绝地反击】的单步概率 (建议0.1左右)")] [Range(0f, 1f)]
        public float counterAttackProbability = 0.10f;

        [Tooltip("【每场局部战争】最多被反推几次")] public int maxCounterAttackCountPerPhase = 1;
        [Tooltip("被反推的最少波次（格子数）")] public int minCounterAttackSteps = 2;
        [Tooltip("被反推的最多波次（格子数）")] public int maxCounterAttackSteps = 4;
        [Tooltip("反攻时的耗时倍率 (默认2倍，表示反推极其艰难)")] public float counterAttackDurationMult = 2.0f;

        [Header("推进节奏")] [Tooltip("相邻判定距离：如果报地理隔离，请把这个值调大（比如 5 或 10）")] [SerializeField]
        private float neighborThreshold = 6.0f;

        [Tooltip("每一波吞并的基础耗时(秒)")] [SerializeField]
        private float invasionDuration = 1.0f;

        [Tooltip("波浪叠加率：控制下一波浪提前多久出发 (0-1)")] [Range(0f, 1f)] [SerializeField]
        private float advanceOverlap = 0.3f;

        [Tooltip("推进速度曲线")] [SerializeField]
        private AnimationCurve invasionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("视觉微调参数")] 
        [Tooltip("前线交战带的边框发光颜色")] [ColorUsage(true, true)] [SerializeField]
        private Color borderGlowColor = new Color(1f, 0.9f, 0.9f, 1f);

        [Tooltip("前线边缘发光带的宽度（值越大，光圈越粗）")]
        [Range(0.01f, 0.2f)] [SerializeField] private float borderWidth = 0.04f;
        
        [Tooltip("前线发光带的亮度/HDR强度（值越大，越刺眼）")]
        [Range(0f, 3f)] [SerializeField] private float borderIntensity = 1.8f;
        
        [Tooltip("前线波浪的呼吸/闪烁频率（值越大，闪得越快）")]
        [Range(0f, 20f)] [SerializeField] private float flickerSpeed = 8f;
        
        [Tooltip("前线波浪呼吸时的波动幅度（控制边缘的收缩伸展范围）")]
        [Range(0f, 0.5f)] [SerializeField] private float flickerAmount = 0.28f;
        
        [Tooltip("边缘侵蚀的噪点密集度（值越小波浪越平缓宽大，值越大波浪越细碎）")]
        [Range(0f, 2f)] [SerializeField] private float noiseScale = 0.25f;
        
        [Tooltip("边缘侵蚀的扭曲剧烈程度（值越大，边缘的狗牙/锯齿拉扯得越深）")]
        [Range(0f, 10f)] [SerializeField] private float noiseAmount = 6f;

        [Header("前线交战特效控制")]
        [Tooltip("总开关：是否开启前线交战时的火花、流弹、爆炸闪光等视觉特效")]
        [SerializeField] private bool showBattleEffects = true;

        [Tooltip("交战特效的全局密集度倍率。值越大，阵线上的战火越密集（建议 1 左右）")]
        [Range(0f, 2f)] [SerializeField] private float battleEffectDensityMult = 1.0f;
        
        [Tooltip("交战火花的发光强度（叠加在底色上的亮度）")]
        [Range(0f, 3f)] [SerializeField] private float battleIntensity = 1.25f;

        [Header("Timeline Control")]
        public bool useTimelineDuration = false;

        // --- 隐藏的底层特效参数（不在面板显示） ---
        private float battleBandWidth = 0.18f;
        private float battleDotDensity = 18f;
        private float battleDotSize = 0.16f;
        private float battleFlowSpeed = 8f;

        private static readonly int p_Maincolor = Shader.PropertyToID("_Maincolor");
        private static readonly int p_Color = Shader.PropertyToID("_Color");
        private static readonly int p_InvasionProgress = Shader.PropertyToID("_InvasionProgress");
        private static readonly int p_InvasionNewColor = Shader.PropertyToID("_InvasionNewColor");
        private static readonly int p_BorderCenter = Shader.PropertyToID("_BorderCenter");
        private static readonly int p_BorderDirection = Shader.PropertyToID("_BorderDirection");
        private static readonly int p_InvasionTileRadius = Shader.PropertyToID("_InvasionTileRadius");
        private static readonly int p_InvasionBorderGlowColor = Shader.PropertyToID("_InvasionBorderGlowColor");
        private static readonly int p_InvasionBorderWidth = Shader.PropertyToID("_InvasionBorderWidth");
        private static readonly int p_InvasionBorderIntensity = Shader.PropertyToID("_InvasionBorderIntensity");
        private static readonly int p_FlickerSpeed = Shader.PropertyToID("_FlickerSpeed");
        private static readonly int p_FlickerAmount = Shader.PropertyToID("_FlickerAmount");
        private static readonly int p_NoiseScale = Shader.PropertyToID("_NoiseScale");
        private static readonly int p_NoiseAmount = Shader.PropertyToID("_NoiseAmount");
        private static readonly int p_BattleBandWidth = Shader.PropertyToID("_BattleBandWidth");
        private static readonly int p_BattleDotDensity = Shader.PropertyToID("_BattleDotDensity");
        private static readonly int p_BattleDotSize = Shader.PropertyToID("_BattleDotSize");
        private static readonly int p_BattleIntensity = Shader.PropertyToID("_BattleIntensity");
        private static readonly int p_BattleFlowSpeed = Shader.PropertyToID("_BattleFlowSpeed");

        private class TileData
        {
            public GameObject gameObject;
            public int initialFaction;
            public int faction;
            public Vector3 worldPosition;
            public MeshRenderer renderer;
            public int currentAnimId = 0;
        }

        private struct InvasionTask
        {
            public TileData target;
            public TileData source;
        }

        private List<TileData> allTiles = new List<TileData>();
        private HashSet<GameObject> addedTilesTracker = new HashSet<GameObject>();
        private bool hasInitialized = false;
        [Header("Timeline 控制")]
        public float phaseDurationOverride = -1f;
        private float timelineStepDuration = 0.1f;
        [Header("Timeline Phase Durations")]
        public float[] phaseDurations;

        private void Start()
        {
            if (!Application.isPlaying) return;
            BuildInitial();
        }

        [ContextMenu("1. 构建数据 (Build Initial)")]
        public void BuildInitial()
        {
            allTiles.Clear();
            addedTilesTracker.Clear();

            if (sharedMaterialTemplate != null)
            {
                for (int i = 0; i < factions.Count; i++)
                {
                    var fac = factions[i];
                    fac.runtimeMat = new Material(sharedMaterialTemplate);
                    fac.runtimeMat.name = $"Faction_{i}_{fac.factionName}_Mat";

                    if (fac.runtimeMat.HasProperty(p_Maincolor))
                        fac.runtimeMat.SetColor(p_Maincolor, fac.baseColor);
                    else if (fac.runtimeMat.HasProperty(p_Color))
                        fac.runtimeMat.SetColor(p_Color, fac.baseColor);
                }
            }

            for (int i = 0; i < factions.Count; i++)
            {
                AddTilesToPool(factions[i].initialCells, i);
            }

            hasInitialized = true;
        }

        private void AddTilesToPool(List<GameObject> list, int faction)
        {
            if (list == null) return;
            foreach (var go in list)
            {
                if (!go) continue;
                MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();

                foreach (var mr in renderers)
                {
                    GameObject childGo = mr.gameObject;
                    if (addedTilesTracker.Contains(childGo)) continue;
                    addedTilesTracker.Add(childGo);

                    var mf = childGo.GetComponent<MeshFilter>();

                    if (sharedMeshTemplate != null && mf != null) mf.sharedMesh = sharedMeshTemplate;
                    if (factions[faction].runtimeMat != null) mr.sharedMaterial = factions[faction].runtimeMat;

                    allTiles.Add(new TileData
                    {
                        gameObject = childGo,
                        initialFaction = faction,
                        faction = faction,
                        worldPosition = childGo.transform.position,
                        renderer = mr
                    });
                }
            }
        }
        int EstimateTotalWaves(
            int attackerFaction,
            int defenderFaction)
        {
            // 复制 faction 状态

            List<int> simFaction =
                new List<int>();

            foreach (var t in allTiles)
                simFaction.Add(t.faction);

            int waveCount = 0;

            while (true)
            {
                List<InvasionTask> frontier =
                    GetDynamicFrontier(
                        attackerFaction,
                        defenderFaction);

                if (frontier.Count == 0)
                    break;

                foreach (var task in frontier)
                {
                    task.target.faction =
                        attackerFaction;
                }

                waveCount++;

                if (waveCount > 500)
                    break;
            }

            // 恢复 faction

            for (int i = 0; i < allTiles.Count; i++)
                allTiles[i].faction =
                    simFaction[i];

            return waveCount;
        }
        private IEnumerator CampaignRunnerSinglePhase(int stepIndex)
        {
            Random.InitState(stepIndex);
            int attackerId = 0;

            if (stepIndex < 0 || stepIndex >= campaignScript.Count)
                yield break;

            CampaignPhase phase = campaignScript[stepIndex];

            float safeDurationMult =
                Mathf.Max(0.1f, phase.durationMult);

            int defenderId =
                phase.targetFactionIndex;

            // ⭐ 新增：预计算总波数

            int totalWaves =
                EstimateTotalWaves(
                    attackerId,
                    defenderId);

            if (totalWaves <= 0)
                yield break;

            float stepTime =
                phaseDurationOverride /
                totalWaves;

            timelineStepDuration =
            (stepTime * safeDurationMult)
             / (1.0f - advanceOverlap);

            float phaseStartTime =
                Time.time;

            while (true)
            {
                List<InvasionTask> forwardTasks =
                    GetDynamicFrontier(
                        attackerId,
                        defenderId);

                if (forwardTasks.Count == 0)
                    break;

                foreach (var task in forwardTasks)
                {
                    StartCoroutine(
                        AnimateSingleTile(
                            task.target,
                            task.source,
                            safeDurationMult,
                            attackerId,
                            defenderId,
                            false));
                }

                // ⭐ 核心时间控制

                float elapsed =
                    Time.time - phaseStartTime;

                float remaining =
                    phaseDurationOverride - elapsed;

                if (remaining <= 0f)
                    break;

                float waitTime =
                timelineStepDuration *(1.0f - advanceOverlap);

                

                yield return new WaitForSeconds(waitTime);
            }
        }


        [ContextMenu("2. 开启剧本推演 (Start Campaign)")]
        public void StartCampaign()
        {
            if (!Application.isPlaying) return;
            if (!hasInitialized) BuildInitial();
            StopAllCoroutines();

            foreach (var t in allTiles) t.currentAnimId = 0;
            StartCoroutine(CampaignRunner());
        }

        // ========================================================
        // Timeline调用：播放指定Phase
        // ========================================================
        // Timeline 调用入口
        public void StartPhaseWithDuration(int phaseIndex, float duration)
        {
            if (!Application.isPlaying) return;

            useTimelineDuration = true;

            phaseDurationOverride = duration;

            StopAllCoroutines();

            foreach (var t in allTiles)
                t.currentAnimId = 0;

            StartCoroutine(CampaignRunnerSinglePhase(phaseIndex));
        }

        public void PlaySinglePhase(int phaseIndex)
        {
            if (!Application.isPlaying)
                return;

            if (!hasInitialized)
                BuildInitial();

            if (phaseIndex < 0 ||
                phaseIndex >= campaignScript.Count)
            {
                Debug.LogWarning(
                    "Invalid phase index: "
                    + phaseIndex);

                return;
            }

            StopAllCoroutines();

            foreach (var t in allTiles)
                t.currentAnimId = 0;

            StartCoroutine(
                SinglePhaseRunner(phaseIndex));
        }

        private IEnumerator CampaignRunner()
        {


            int attackerId = 0;
            string attackerName = factions[attackerId].factionName;

            for (int stepIndex = 0; stepIndex < campaignScript.Count; stepIndex++)
            {
                CampaignPhase phase = campaignScript[stepIndex];
                float safeDurationMult = Mathf.Max(0.1f, phase.durationMult);

                if (phase.delayBeforeAttack > 0) yield return new WaitForSeconds(phase.delayBeforeAttack);

                int defenderId = phase.targetFactionIndex;
                if (defenderId <= 0 || defenderId >= factions.Count) continue;

                int forwardProgressDepth = 0;
                int currentBlockageCount = 0;
                int currentCounterAttackCount = 0;
                int lastEventWave = -999;

                while (true)
                {
                    bool canTriggerBlockage = enableMacroCampaign && phase.allowBlockage &&
                                              forwardProgressDepth >= safeWaveCount;
                    bool canTriggerCounter = enableMacroCampaign && phase.allowCounterAttack &&
                                             forwardProgressDepth >= safeWaveCount;

                    if (canTriggerCounter &&
                        currentCounterAttackCount < maxCounterAttackCountPerPhase &&
                        (forwardProgressDepth - lastEventWave) >= minEventInterval &&
                        Random.value < counterAttackProbability)
                    {
                        currentCounterAttackCount++;
                        lastEventWave = forwardProgressDepth;

                        int actualMax = Mathf.Min(maxCounterAttackSteps, forwardProgressDepth);
                        int actualMin = Mathf.Min(minCounterAttackSteps, actualMax);
                        int pushBackSteps = Random.Range(actualMin, actualMax + 1);

                        for (int s = 0; s < pushBackSteps; s++)
                        {
                            List<InvasionTask> counterTasks = GetDynamicFrontier(defenderId, attackerId);
                            if (counterTasks.Count == 0) break;

                            foreach (var task in counterTasks)
                                StartCoroutine(AnimateSingleTile(task.target, task.source, counterAttackDurationMult,
                                    attackerId, defenderId, isCounterAttack: true));

                            forwardProgressDepth--;
                            yield return new WaitForSeconds(Mathf.Max(0.01f,
                                (invasionDuration * counterAttackDurationMult) * (1.0f - advanceOverlap)));
                        }

                        continue;
                    }

                    List<InvasionTask> forwardTasks = GetDynamicFrontier(attackerId, defenderId);
                    if (forwardTasks.Count == 0) break;

                    if (canTriggerBlockage &&
                        currentBlockageCount < maxBlockageCountPerPhase &&
                        (forwardProgressDepth - lastEventWave) >= minEventInterval &&
                        Random.value < blockageProbability)
                    {
                        currentBlockageCount++;
                        lastEventWave = forwardProgressDepth;

                        int blockSteps = Random.Range(minBlockageSteps, maxBlockageSteps + 1);
                        float fwdMult = Random.Range(2.5f, 4.5f);

                        foreach (var task in forwardTasks)
                            StartCoroutine(AnimateSingleTile(task.target, task.source, fwdMult * safeDurationMult,
                                attackerId, defenderId, false));
                        forwardProgressDepth++;
                        yield return new WaitForSeconds((invasionDuration * fwdMult * safeDurationMult) *
                                                        (1.0f - advanceOverlap));

                        for (int s = 1; s < blockSteps; s++)
                        {
                            List<InvasionTask> subsequentBlockTasks = GetDynamicFrontier(attackerId, defenderId);
                            if (subsequentBlockTasks.Count == 0) break;

                            fwdMult = Random.Range(2.5f, 4.5f);
                            foreach (var task in subsequentBlockTasks)
                                StartCoroutine(AnimateSingleTile(task.target, task.source, fwdMult * safeDurationMult,
                                    attackerId, defenderId, false));
                            forwardProgressDepth++;
                            yield return new WaitForSeconds((invasionDuration * fwdMult * safeDurationMult) *
                                                            (1.0f - advanceOverlap));
                        }

                        continue;
                    }

                    foreach (var task in forwardTasks)
                        StartCoroutine(AnimateSingleTile(task.target, task.source, safeDurationMult, attackerId,
                            defenderId, false));

                    forwardProgressDepth++;
                    yield return new WaitForSeconds((invasionDuration * safeDurationMult) * (1.0f - advanceOverlap));
                }
            }
        }

        // ========================================================
        // Timeline专用：只运行单个Phase
        // ========================================================

        private IEnumerator SinglePhaseRunner(int stepIndex)
        {
            int attackerId = 0;

            CampaignPhase phase = campaignScript[stepIndex];
            float safeDurationMult = Mathf.Max(0.1f, phase.durationMult);

            if (phase.delayBeforeAttack > 0)
                yield return new WaitForSeconds(phase.delayBeforeAttack);

            int defenderId = phase.targetFactionIndex;

            if (defenderId <= 0 || defenderId >= factions.Count)
                yield break;

            int forwardProgressDepth = 0;
            int currentBlockageCount = 0;
            int currentCounterAttackCount = 0;
            int lastEventWave = -999;

            while (true)
            {
                bool canTriggerBlockage =
                    enableMacroCampaign &&
                    phase.allowBlockage &&
                    forwardProgressDepth >= safeWaveCount;

                bool canTriggerCounter =
                    enableMacroCampaign &&
                    phase.allowCounterAttack &&
                    forwardProgressDepth >= safeWaveCount;

                // ===== 反攻 =====

                if (canTriggerCounter &&
                    currentCounterAttackCount < maxCounterAttackCountPerPhase &&
                    (forwardProgressDepth - lastEventWave) >= minEventInterval &&
                    Random.value < counterAttackProbability)
                {
                    currentCounterAttackCount++;
                    lastEventWave = forwardProgressDepth;

                    int actualMax =
                        Mathf.Min(maxCounterAttackSteps,
                                  forwardProgressDepth);

                    int actualMin =
                        Mathf.Min(minCounterAttackSteps,
                                  actualMax);

                    int pushBackSteps =
                        Random.Range(actualMin,
                                     actualMax + 1);

                    for (int s = 0; s < pushBackSteps; s++)
                    {
                        List<InvasionTask> counterTasks =
                            GetDynamicFrontier(
                                defenderId,
                                attackerId);

                        if (counterTasks.Count == 0)
                            break;

                        foreach (var task in counterTasks)
                        {
                            StartCoroutine(
                                AnimateSingleTile(
                                    task.target,
                                    task.source,
                                    counterAttackDurationMult,
                                    attackerId,
                                    defenderId,
                                    true));
                        }

                        forwardProgressDepth--;

                        yield return new WaitForSeconds(
                            Mathf.Max(
                                0.01f,
                                (invasionDuration *
                                 counterAttackDurationMult)
                                *
                                (1.0f - advanceOverlap)));
                    }

                    continue;
                }

                // ===== 正常推进 =====

                List<InvasionTask> forwardTasks =
                    GetDynamicFrontier(
                        attackerId,
                        defenderId);

                if (forwardTasks.Count == 0)
                    break;

                // ===== 受阻 =====

                if (canTriggerBlockage &&
                    currentBlockageCount < maxBlockageCountPerPhase &&
                    (forwardProgressDepth - lastEventWave) >= minEventInterval &&
                    Random.value < blockageProbability)
                {
                    currentBlockageCount++;
                    lastEventWave = forwardProgressDepth;

                    int blockSteps =
                        Random.Range(
                            minBlockageSteps,
                            maxBlockageSteps + 1);

                    float fwdMult =
                        Random.Range(
                            2.5f,
                            4.5f);

                    foreach (var task in forwardTasks)
                    {
                        StartCoroutine(
                            AnimateSingleTile(
                                task.target,
                                task.source,
                                fwdMult * safeDurationMult,
                                attackerId,
                                defenderId,
                                false));
                    }

                    forwardProgressDepth++;

                    yield return new WaitForSeconds(
                        (invasionDuration *
                         fwdMult *
                         safeDurationMult)
                        *
                        (1.0f - advanceOverlap));

                    continue;
                }

                // ===== 正常推进 =====

                foreach (var task in forwardTasks)
                {
                    StartCoroutine(
                        AnimateSingleTile(
                            task.target,
                            task.source,
                            safeDurationMult,
                            attackerId,
                            defenderId,
                            false));
                }

                forwardProgressDepth++;

                yield return new WaitForSeconds(
                    (invasionDuration *
                     safeDurationMult)
                    *
                    (1.0f - advanceOverlap));
            }
        }

        private List<InvasionTask> GetDynamicFrontier(int attackerFaction, int defenderFaction)
        {
            List<InvasionTask> frontier = new List<InvasionTask>();
            for (int targetIdx = 0; targetIdx < allTiles.Count; targetIdx++)
            {
                if (allTiles[targetIdx].faction == defenderFaction)
                {
                    TileData targetTile = allTiles[targetIdx];
                    TileData bestSourceTile = null;
                    float minScore = float.MaxValue;

                    for (int sourceIdx = 0; sourceIdx < allTiles.Count; sourceIdx++)
                    {
                        if (allTiles[sourceIdx].faction == attackerFaction)
                        {
                            float dist = Vector3.Distance(targetTile.worldPosition, allTiles[sourceIdx].worldPosition);
                            if (dist <= neighborThreshold)
                            {
                                // 左右优先核心算法：提取 Z轴（上下）距离作为额外惩罚权重
                                float dz = Mathf.Abs(targetTile.worldPosition.z - allTiles[sourceIdx].worldPosition.z);
                                float score = dist + (dz * 0.1f);

                                if (score < minScore)
                                {
                                    minScore = score;
                                    bestSourceTile = allTiles[sourceIdx];
                                }
                            }
                        }
                    }

                    if (bestSourceTile != null)
                        frontier.Add(new InvasionTask { target = targetTile, source = bestSourceTile });
                }
            }

            return frontier;
        }

        private IEnumerator AnimateSingleTile(TileData target, TileData source, float durationMult, int attackerId,
            int defenderId, bool isCounterAttack)
        {
            if (!target.renderer) yield break;

            target.currentAnimId++;
            int myAnimId = target.currentAnimId;

            Material defenderMat = factions[defenderId].runtimeMat;
            Material attackerMat = factions[attackerId].runtimeMat;

            int defColorID = (defenderMat != null && defenderMat.HasProperty(p_Maincolor)) ? p_Maincolor : p_Color;
            int attColorID = (attackerMat != null && attackerMat.HasProperty(p_Maincolor)) ? p_Maincolor : p_Color;

            Vector4 defRawGPUVector = defenderMat != null ? defenderMat.GetVector(defColorID) : Vector4.one;
            Vector4 attRawGPUVector = attackerMat != null ? attackerMat.GetVector(attColorID) : Vector4.one;

            Vector4 currentDefenderVector = isCounterAttack ? attRawGPUVector : defRawGPUVector;
            Vector4 currentAttackerVector = isCounterAttack ? defRawGPUVector : attRawGPUVector;
            Material currentAttackerMat = isCounterAttack ? defenderMat : attackerMat;

            target.faction = isCounterAttack ? defenderId : attackerId;

            Vector2 sPos = new Vector2(source.worldPosition.x, source.worldPosition.z);
            Vector2 tPos = new Vector2(target.worldPosition.x, target.worldPosition.z);
            Vector2 center = (sPos + tPos) * 0.5f;
            Vector2 forward = (tPos - sPos).normalized;
            Vector2 borderDir = new Vector2(forward.y, -forward.x);

            float actualRadius = Vector3.Distance(source.worldPosition, target.worldPosition) * 0.5f;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            float actualDuration;

            if (useTimelineDuration)
            {
                actualDuration =
                    timelineStepDuration * durationMult;
            }
            else
            {
                actualDuration =
                    invasionDuration * durationMult;
            }
            float timer = 0f;

            while (timer < actualDuration)
            {
                if (target.currentAnimId != myAnimId) yield break;
                timer += Time.deltaTime;
                float p = invasionCurve.Evaluate(timer / actualDuration);

                target.renderer.GetPropertyBlock(mpb);

                // 这次绝对对上了！全部使用最高效的底层 ID！
                mpb.SetVector(p_Maincolor, currentDefenderVector);
                mpb.SetFloat(p_InvasionProgress, p);
                mpb.SetVector(p_InvasionNewColor, currentAttackerVector);

                mpb.SetFloat(p_InvasionTileRadius, actualRadius);
                mpb.SetVector(p_BorderCenter, new Vector4(center.x, 0, center.y, 0));
                mpb.SetVector(p_BorderDirection, new Vector4(borderDir.x, 0, borderDir.y, 0));

                mpb.SetColor(p_InvasionBorderGlowColor, borderGlowColor);
                mpb.SetFloat(p_InvasionBorderWidth, borderWidth);
                mpb.SetFloat(p_InvasionBorderIntensity, borderIntensity);
                mpb.SetFloat(p_FlickerSpeed, flickerSpeed);
                mpb.SetFloat(p_FlickerAmount, flickerAmount);
                mpb.SetFloat(p_NoiseScale, noiseScale);
                mpb.SetFloat(p_NoiseAmount, noiseAmount);
                
                // ✨ 根据面板控制动态覆盖底层特效参数
                float finalIntensity = showBattleEffects ? battleIntensity : 0f;
                float finalDensity = battleDotDensity * battleEffectDensityMult;

                mpb.SetFloat(p_BattleBandWidth, battleBandWidth);
                mpb.SetFloat(p_BattleDotDensity, finalDensity);
                mpb.SetFloat(p_BattleDotSize, battleDotSize);
                mpb.SetFloat(p_BattleIntensity, finalIntensity);
                mpb.SetFloat(p_BattleFlowSpeed, battleFlowSpeed);

                target.renderer.SetPropertyBlock(mpb);
                yield return null;
            }

            if (target.currentAnimId != myAnimId) yield break;

            target.renderer.GetPropertyBlock(mpb);
            mpb.Clear();
            target.renderer.SetPropertyBlock(mpb);
            if (currentAttackerMat != null) target.renderer.sharedMaterial = currentAttackerMat;
        }

        [ContextMenu("3. 重置所有阵营 (Reset Grid)")]
        public void ResetGrid()
        {
            StopAllCoroutines();
            if (allTiles == null || allTiles.Count == 0) return;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            foreach (var t in allTiles)
            {
                if (t.renderer)
                {
                    t.faction = t.initialFaction;
                    var fac = factions[t.faction];
                    if (fac.runtimeMat != null)
                        t.renderer.sharedMaterial = fac.runtimeMat;

                    t.renderer.GetPropertyBlock(mpb);
                    mpb.Clear();
                    t.renderer.SetPropertyBlock(mpb);
                }

                t.currentAnimId = 0;
            }
        }

        float GetPhaseDuration(int index)
        {
            if (phaseDurations == null)
                return 5f;

            if (index >= phaseDurations.Length)
                return 5f;

            return phaseDurations[index];
        }

        // ========================================================
        // Timeline绑定用函数
        // ========================================================

        public void PlayPhase0()
        {
            StartPhaseWithDuration(0, GetPhaseDuration(0));
        }

        public void PlayPhase1()
        {
            StartPhaseWithDuration(1, GetPhaseDuration(1));
        }

        public void PlayPhase2()
        {
            StartPhaseWithDuration(2, GetPhaseDuration(2));
        }

        public void PlayPhase3()
        {
            StartPhaseWithDuration(3, GetPhaseDuration(3));
        }

        public void PlayPhase4()
        {
            StartPhaseWithDuration(4, GetPhaseDuration(4));
        }

        public void PlayPhase5()
        {
            StartPhaseWithDuration(5, GetPhaseDuration(5));
        }

        public void PlayPhase6()
        {
            StartPhaseWithDuration(6, GetPhaseDuration(6));
        }

        public void PlayPhase7()
        {
            StartPhaseWithDuration(7, GetPhaseDuration(7));
        }

        public void PlayPhase8()
        {
            StartPhaseWithDuration(8, GetPhaseDuration(8));
        }

        public void PlayPhase9()
        {
            StartPhaseWithDuration(9, GetPhaseDuration(9));
        }

        public void PlayPhase10()
        {
            StartPhaseWithDuration(10, GetPhaseDuration(10));
        }

        public void PlayPhase11()
        {
            StartPhaseWithDuration(11, GetPhaseDuration(11));
        }

        public void PlayPhase12()
        {
            StartPhaseWithDuration(12, GetPhaseDuration(12));
        }

        public void PlayPhase13()
        {
            StartPhaseWithDuration(13, GetPhaseDuration(13));
        }

        // =========================================================================================
        // 在 Scene 视图的领土中心上空，全息投影显示阵营 ID！
        // =========================================================================================
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (factions == null || factions.Count == 0 || !OpenMapId) return;

            // 设置全息字体的样式
            GUIStyle style = new GUIStyle();
            style.fontSize = 14; // 字体大小（觉得小可以改大）
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < factions.Count; i++)
            {
                var fac = factions[i];
                if (fac == null || fac.initialCells == null || fac.initialCells.Count == 0) continue;

                Vector3 center = Vector3.zero;
                int count = 0;

                // 精准计算该国家所有领土的几何中心点
                foreach (var parentGo in fac.initialCells)
                {
                    if (parentGo == null) continue;
                    MeshRenderer[] renderers = parentGo.GetComponentsInChildren<MeshRenderer>();
                    foreach (var mr in renderers)
                    {
                        center += mr.transform.position;
                        count++;
                    }
                }

                if (count > 0)
                {
                    center /= count;
                    center.y += 3.5f; // Y轴抬高，悬浮在格子上方

                    string displayText = $"[ID: {i}]\n{fac.factionName}";

                    // 1. 先画一层黑色描边/阴影，防止在浅色底板上看不清字
                    style.normal.textColor = Color.black;
                    UnityEditor.Handles.Label(center + new Vector3(0.05f, -0.05f, 0), displayText, style);
                    UnityEditor.Handles.Label(center + new Vector3(-0.05f, 0.05f, 0), displayText, style);

                    // 2. 再画原本的阵营颜色文字（如果阵营颜色太暗，就强行显示白色）
                    Color textColor = fac.baseColor;
                    if (textColor.r < 0.2f && textColor.g < 0.2f && textColor.b < 0.2f)
                        textColor = Color.white;

                    style.normal.textColor = textColor;
                    UnityEditor.Handles.Label(center, displayText, style);
                }
            }
        }
#endif
    }

// =========================================================================================
// 让列表折叠栏自动显示 [ID: X]！
// =========================================================================================
#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(AreaFactionMultiCampaignGrid.FactionConfig))]
    public class FactionConfigDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            string path = property.propertyPath;
            int startIndex = path.LastIndexOf('[');
            int endIndex = path.LastIndexOf(']');
            string indexStr = "0";
            if (startIndex >= 0 && endIndex > startIndex)
            {
                indexStr = path.Substring(startIndex + 1, endIndex - startIndex - 1);
            }

            UnityEditor.SerializedProperty nameProp = property.FindPropertyRelative("factionName");
            string factionName = nameProp != null ? nameProp.stringValue : "未命名";

            label.text = $"[ID: {indexStr}]  {factionName}";

            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            return UnityEditor.EditorGUI.GetPropertyHeight(property, true);
        }
    }
#endif
}

