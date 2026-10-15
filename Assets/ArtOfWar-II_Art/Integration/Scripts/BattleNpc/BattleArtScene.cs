using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Art.View
{
    public class BattleArtScene : MonoBehaviour, IBattleArtScene
    {
        private const string StartNpc = "StartNpc";
        private const string EndNpc = "EndNpc";
        private const string AtoB = "AtoB";
        private const string BtoC = "BtoC";
        private const string CtoB = "CtoB";

        [SerializeField] private GlobalWindController windController;

        [Space, SerializeField] private PlayableDirector environmentDirector; // 环境timeline
        [SerializeField] private PlayableDirector npcGroupDirector; // npc timeline
        [SerializeField] private PlayableDirector bossAppearDirector; // boss出现timeline

        [Space, SerializeField] private List<BattleNpc> soldierNpcList;
        [SerializeField] private List<BattleNpc> npcList;
        [SerializeField] private List<Transform> obstacles; // 场景内障碍

        [Space, SerializeField] private float evtTime = 0.1f;
        [SerializeField] private float duration = 0.22f;
        [SerializeField] private float duration2 = 0.1f;
        [SerializeField] private float duration3 = 0.1f;
        [SerializeField] private float animScale = 1.2f;
        [SerializeField] private float jumpPower = 3f;

        private int dangerCounter;
        public List<Transform> Obstacles => obstacles;

        public void Enter()
        {
            dangerCounter = 0;
            npcList.ForEach(x => x.Enter());
            soldierNpcList.ForEach(x => x.OnSoldierNpcEnter());

            // reset timeline
            ResetTimelineState();

            // 播放进入效果
            TimelinePlayHelper.PlayTimeLineGroup(environmentDirector, AtoB);
        }

        public void BattleEnd(bool isWin)
        {
            // do something
        }

        private void ResetTimelineState()
        {
            if (!npcGroupDirector)
            {
                return;
            }

            TimelinePlayHelper.SetTimeLineGroupTime(npcGroupDirector, StartNpc, 0);

            npcGroupDirector.stopped -= OnNpcGroupPlayStopped;
            npcGroupDirector.stopped += OnNpcGroupPlayStopped;
        }

        public void Exit()
        {
            foreach (BattleNpc npc in npcList)
            {
                npc.Exit();
            }

            environmentDirector.Stop();
            if (npcGroupDirector)
            {
                npcGroupDirector.Stop();
            }

            bossAppearDirector.Stop();
            windController.ResetState();
        }

        /// <summary>
        /// 危险
        /// </summary>
        public void OnDanger()
        {
            if (dangerCounter == 0)
            {
                TimelinePlayHelper.PlayTimeLineGroup(environmentDirector, BtoC);
            }

            dangerCounter++;
        }

        /// <summary>
        /// 回复正常
        /// </summary>
        public void OnNormal()
        {
            if (dangerCounter <= 0)
            {
                return;
            }

            dangerCounter--;
            if (dangerCounter <= 0)
            {
                TimelinePlayHelper.PlayTimeLineGroup(environmentDirector, CtoB);
            }
        }

        /// <summary>
        /// boss出现
        /// </summary>
        public void OnBossAppear()
        {
            soldierNpcList.ForEach(x => x.OnSoldierNpcBossAppear());
            bossAppearDirector.Play();
        }

        /// <summary>
        /// 战斗进入
        /// </summary>
        public void OnBattleEnter()
        {
            foreach (BattleNpc npc in npcList)
            {
                npc.OnBattleStart();
            }

            TimelinePlayHelper.PlayTimeLineGroup(npcGroupDirector, StartNpc);
        }

        /// <summary>
        /// 战斗退出
        /// </summary>
        public void OnBattleExit()
        {
            TimelinePlayHelper.PlayTimeLineGroup(npcGroupDirector, EndNpc);
        }

        /// <summary>
        /// 进入第"2"阶段
        /// </summary>
        public void OnStage2()
        {
            // do something
        }

        /// <summary>
        /// 进入第"3"阶段
        /// </summary>
        public void OnStage3()
        {
            npcList.ForEach(x => x.OnStage3());
        }

        /// <summary>
        /// 战斗失败
        /// </summary>
        public void OnFail()
        {
            npcList.ForEach(x => x.OnFail());
        }

        private void OnNpcGroupPlayStopped(PlayableDirector director)
        {
            // 获取 Timeline 资产
            if (director.playableAsset is not TimelineAsset timeline)
            {
                return;
            }

            TrackAsset group = timeline.GetOutputTracks().FirstOrDefault(x => !x.muted && x.parent.name.Equals(StartNpc));
            if (group)
            {
                foreach (BattleNpc npc in npcList)
                {
                    npc.OnBattleStart();
                }
            }
        }
    }
}