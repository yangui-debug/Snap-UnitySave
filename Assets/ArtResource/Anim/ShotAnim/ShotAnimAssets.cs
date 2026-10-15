using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShotAnim
{
    [Serializable]
    public class ShotAnimCurve
    {
        [SerializeField] private string name = default;
        [SerializeField] private AnimationCurve curve = default;

        public string Name => name;
        public AnimationCurve Curve => curve;
    }

    public class ShotAnimAssets : ScriptableObject
    {
        [SerializeField] private List<ShotAnimCurve> curveList = default;

        public List<ShotAnimCurve> CurveList => curveList;
    }
}