// =====================================================================
// Copyright 2013-2022 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Pools;
using UnityEngine;
using FluffyUnderware.DevTools;
using ToolBuddy.Pooling.Collections;

namespace FluffyUnderware.Curvy.Components
{
    /// <summary>
    /// Class to drive a LineRenderer with a CurvySpline
    /// </summary>
    [AddComponentMenu("Curvy/Misc/Curvy Line Renderer")]
    [RequireComponent(typeof(LineRenderer))]
    [HelpURL(CurvySpline.DOCLINK + "curvylinerenderer")]
    public class CurvyLineRenderer : SplineProcessor
    {
        private LineRenderer mRenderer;

        protected override void Awake()
        {
            mRenderer = GetComponent<LineRenderer>();
           base.Awake();
        }

        protected override void OnEnable()
        {
            mRenderer = GetComponent<LineRenderer>();
            base.OnEnable();
        }

        private void Update()
        {
            EnforceWorldSpaceUsage();
        }

        private void EnforceWorldSpaceUsage()
        {
            if (mRenderer.useWorldSpace == false)
            {
                mRenderer.useWorldSpace = true;
                DTLog.Log("[Curvy] CurvyLineRenderer: Line Renderer's Use World Space was overriden to true");
            }
        }

        /// <summary>
        /// Update the <see cref="LineRenderer"/>'s points with the cache points of the <see cref="CurvySpline"/>
        /// </summary>
        public override void Refresh()
        {
            if (Spline && Spline.IsInitialized && Spline.Dirty == false)
            {
                EnforceWorldSpaceUsage();
                SubArray<Vector3> positions = Spline.GetPositionsCache(Space.World);
                mRenderer.positionCount = positions.Count;
                mRenderer.SetPositions(positions.Array);
                ArrayPools.Vector3.Free(positions);
            }
            else if (mRenderer != null)
            {
                EnforceWorldSpaceUsage();
                mRenderer.positionCount = 0;
            }
        }
    }
}