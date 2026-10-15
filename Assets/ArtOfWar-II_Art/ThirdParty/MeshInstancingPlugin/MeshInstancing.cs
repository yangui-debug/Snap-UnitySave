using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdParty.AnimationInstancingPlugIn
{
    public class MeshPackage
    {
        public int nameCode;
        public Mesh mesh;
        public Material[] materials;

        public ShadowCastingMode shadowCastingMode;
        public bool receiveShadow;
        public int layer;
        public Transform transform;
    }

    public class MeshInstancing : MonoBehaviour
    {
        public BoundingSphere boundingSphere;

        [NonSerialized] public bool visible = true;
        [NonSerialized] public Transform worldTransform;

        public List<MeshPackage> packages;
        private float radius;

        private void Awake()
        {
            worldTransform = transform;
        }

        private void OnEnable()
        {
            Init();
            MeshInstancingMgr.Instance.AddInstance(this);
        }

        private void OnDisable()
        {
            MeshInstancingMgr.Instance.RemoveInstance(this);
        }

        private void Init()
        {
            if (packages == null)
            {
                packages = new List<MeshPackage>();

                CreateMeshRendererPackage();

                CreateSkinnedMeshRendererPackage();

                CalcBoundingSphere();
            }

            boundingSphere = new BoundingSphere(transform.position, radius);
        }

        private void CreateMeshRendererPackage()
        {
            MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                // 只按mesh分组, 不按材质分组, 如后续有需要可以加材质分组, 不影响其他流程
                MeshRenderer r = filter.GetComponent<MeshRenderer>();

                MeshPackage package = new MeshPackage();
                package.mesh = filter.sharedMesh;
                package.materials = r.sharedMaterials;
                package.nameCode = CalcNameHash(package);

                package.shadowCastingMode = r.shadowCastingMode;
                package.receiveShadow = r.receiveShadows;
                package.layer = r.gameObject.layer;
                package.transform = r.transform;

                packages.Add(package);
            }
        }

        private void CreateSkinnedMeshRendererPackage()
        {
            SkinnedMeshRenderer[] skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshRenderers)
            {
                // 只按mesh分组, 不按材质分组, 如后续有需要可以加材质分组, 不影响其他流程
                MeshPackage package = new MeshPackage();
                package.mesh = skinnedMesh.sharedMesh;
                package.materials = skinnedMesh.sharedMaterials;
                package.nameCode = CalcNameHash(package);

                package.shadowCastingMode = skinnedMesh.shadowCastingMode;
                package.receiveShadow = skinnedMesh.receiveShadows;
                package.layer = skinnedMesh.gameObject.layer;
                package.transform = skinnedMesh.transform;

                packages.Add(package);
            }
        }

        private int CalcNameHash(MeshPackage package)
        {
            int hash = package.mesh.name.GetHashCode();
            for (int i = 0; i < package.materials.Length; i++)
            {
                hash += package.materials[i].name.GetHashCode();
            }

            return hash;
        }

        private void CalcBoundingSphere()
        {
            Bounds bound = new Bounds(transform.position, Vector3.one);

            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i != meshRenderers.Length; ++i)
            {
                MeshRenderer meshRenderer = meshRenderers[i];
                bound.Encapsulate(meshRenderer.bounds);
                meshRenderer.enabled = false;
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i != skinnedMeshRenderers.Length; ++i)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
                bound.Encapsulate(skinnedMeshRenderer.bounds);
                skinnedMeshRenderer.enabled = false;
            }

            radius = bound.size.x > bound.size.y ? bound.size.x : bound.size.y;
            radius = radius > bound.size.z ? radius : bound.size.z;
        }
    }
}