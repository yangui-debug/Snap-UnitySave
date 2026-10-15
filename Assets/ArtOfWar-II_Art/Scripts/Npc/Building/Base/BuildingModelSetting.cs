using System.Collections.Generic;
using UnityEngine;

namespace Npc
{
    public class BuildingModelSetting : MonoBehaviour
    {
        [SerializeField] private GameObject obstacleGroup;
        [SerializeField] private List<Transform> entryPath;
        [SerializeField] private Transform lvFlagPoint;

        public GameObject ObstacleGroup => obstacleGroup;
        public List<Transform> EntryPath => entryPath;
        public Transform LvFlagPoint => lvFlagPoint;
    }
}