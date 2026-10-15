using System.Collections.Generic;
using UnityEngine;

namespace ThirdParty.AnimationInstancingPlugIn
{
    public class MeshInstancingMgr : MonoBehaviour
    {
        private const int instanceCount = 1023;
        public static MeshInstancingMgr Instance;

        private List<MeshInstancing> instancingList;
        private Queue<int> emptyIndexes;

        private Dictionary<int, List<MeshMatrix4x4>> worldMatrix;
        private Dictionary<int, MeshVertex> meshVertexes;

        private BoundingSphere[] boundingSphere;
        private CullingGroup cullingGroup;

        public bool autoSetCamera;

        private void Awake()
        {
            Instance = this;

            // Initialize the transformation matrices array.
            instancingList = new List<MeshInstancing>(instanceCount);
            worldMatrix = new Dictionary<int, List<MeshMatrix4x4>>(instanceCount);
            meshVertexes = new Dictionary<int, MeshVertex>();
            boundingSphere = new BoundingSphere[instanceCount];

            emptyIndexes = new Queue<int>();

            if (autoSetCamera)
            {
                SetCamera(Camera.main);
            }
        }

        private void OnDestroy()
        {
            cullingGroup?.Dispose();
            cullingGroup = null;
        }

        public void SetCamera(Camera camera)
        {
            InitializeCullingGroup(camera);
        }

        public void AddInstance(MeshInstancing instancing)
        {
            bool useCache = emptyIndexes.Count > 0;

            int idx;
            if (useCache)
            {
                idx = emptyIndexes.Dequeue();
            }
            else
            {
                idx = instancingList.Count;
                instancingList.Add(null);
            }

            instancingList[idx] = instancing;
            AddVertexes(instancing);

            EnsureBoundingSphereCapacity(instancingList.Count);
            boundingSphere[idx] = instancing.boundingSphere;

            if (!useCache)
            {
                cullingGroup.SetBoundingSphereCount(instancingList.Count);
            }
        }

        public void RemoveInstance(MeshInstancing instancing)
        {
            if (!Instance)
            {
                return;
            }

            int index = instancingList.IndexOf(instancing);
            if (index >= 0)
            {
                instancingList[index] = null;
                emptyIndexes.Enqueue(index);
            }
        }

        private void AddVertexes(MeshInstancing instancing)
        {
            foreach (MeshPackage pkg in instancing.packages)
            {
                if (!meshVertexes.TryGetValue(pkg.nameCode, out MeshVertex vertex))
                {
                    meshVertexes[pkg.nameCode] = vertex = new MeshVertex();
                    vertex.nameCode = pkg.nameCode;
                    vertex.meshPackage = pkg;
                }

                foreach (Material material in pkg.materials)
                {
                    material.enableInstancing = true;
                }
            }
        }

        private void EnsureBoundingSphereCapacity(int capacity)
        {
            if (EnsureCapacity(boundingSphere, capacity, out boundingSphere))
            {
                cullingGroup.SetBoundingSpheres(boundingSphere);
            }
        }

        private bool EnsureCapacity<T>(T[] array, int capacity, out T[] newArray)
        {
            if (array.Length < capacity)
            {
                T[] temp = array;
                newArray = new T[capacity * 2];
                temp.CopyTo(newArray, 0);

                return true;
            }

            newArray = array;
            return false;
        }

        private void InitializeCullingGroup(Camera camera)
        {
            cullingGroup?.Dispose();
            cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = camera;
            cullingGroup.onStateChanged = CullingStateChanged;
            cullingGroup.SetBoundingSpheres(boundingSphere);
            cullingGroup.SetBoundingSphereCount(0);
        }

        private void Update()
        {
            if (cullingGroup == null || !cullingGroup.targetCamera)
            {
                return;
            }

            int count = instancingList.Count;
            for (int i = 0; i < count; i++)
            {
                MeshInstancing instancing = instancingList[i];
                if (!instancing)
                {
                    continue;
                }

                instancing.boundingSphere.position = instancing.worldTransform.position;
                boundingSphere[i] = instancing.boundingSphere;

                // TODO: Optimize the culling.
                instancing.visible = cullingGroup.IsVisible(i);
                if (!instancing.visible)
                {
                    continue;
                }

                ApplyMatrix(instancing);
            }

            // Draw the instanced meshes.
            foreach (MeshVertex vertex in meshVertexes.Values)
            {
                Draw(vertex);
            }
        }

        private void CullingStateChanged(CullingGroupEvent evt)
        {
            if (evt.hasBecomeVisible)
            {
                if (instancingList[evt.index])
                {
                    instancingList[evt.index].visible = true;
                }
            }

            if (evt.hasBecomeInvisible)
            {
                if (instancingList[evt.index])
                {
                    instancingList[evt.index].visible = false;
                }
            }
        }

        private void ApplyMatrix(MeshInstancing instancing)
        {
            foreach (MeshPackage pkg in instancing.packages)
            {
                if (!worldMatrix.TryGetValue(pkg.nameCode, out List<MeshMatrix4x4> matrices))
                {
                    worldMatrix[pkg.nameCode] = matrices = new List<MeshMatrix4x4>();
                }

                MeshMatrix4x4 matrix4x4 = null;
                for (int i = 0; i < matrices.Count; i++)
                {
                    if (matrices[i].instancingCount < instanceCount)
                    {
                        matrix4x4 = matrices[i];
                        break;
                    }
                }

                if (matrix4x4 == null)
                {
                    matrix4x4 = new MeshMatrix4x4 {matrices = new Matrix4x4[instanceCount]};
                    matrices.Add(matrix4x4);
                }

                matrix4x4.matrices[matrix4x4.instancingCount++] = pkg.transform.localToWorldMatrix;
            }
        }

        private void Draw(MeshVertex vertex)
        {
            if (!worldMatrix.TryGetValue(vertex.nameCode, out List<MeshMatrix4x4> matrices))
            {
                return;
            }

            MeshPackage pkg = vertex.meshPackage;
            foreach (MeshMatrix4x4 matrix4X4 in matrices)
            {
                for (int i = 0; i < pkg.materials.Length; i++)
                {
                    Graphics.DrawMeshInstanced(pkg.mesh, i, pkg.materials[i],
                        matrix4X4.matrices, matrix4X4.instancingCount,
                        null, pkg.shadowCastingMode, pkg.receiveShadow, pkg.layer);
                }

                matrix4X4.instancingCount = 0;
            }
        }
    }
}