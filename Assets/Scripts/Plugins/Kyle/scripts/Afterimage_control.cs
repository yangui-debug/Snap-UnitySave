using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Afterimage_control : MonoBehaviour
{
    [Header("All mesh that will show afterimages Contains skinnedmesh ")]
    public SkinnedMeshRenderer[] array_skinned_mesh_renderer;
    public MeshFilter[] array_mesh_filter;

    [Header("The material of the afterimage")]
    public Material material_afterimage;

    [Header("The total duration of the afterimage generated during one play In seconds")]
    public float time_total_duration;

    [Header("Time between each generation of afterimages In seconds")]
    public float time_interval;

    [Header("Afterimage display time, in seconds")]
    public float time_afterimage_show;

    private List<Afterimage_mesh> list_afterimage_mesh = new List<Afterimage_mesh>();

    private Coroutine coroutine;

    void Start()
   {
     play();
   }

    void LateUpdate()
    {
        bool is_need_remove = false;

        foreach (var afterimage in this.list_afterimage_mesh)
        {


            Graphics.DrawMesh(afterimage.mesh, Matrix4x4.identity, afterimage.material, gameObject.layer, null, 0, null, false);
            afterimage.left_time -= Time.deltaTime;


            if (afterimage.material.HasProperty("_BaseColor"))
            {

                Color c = Color.white;
                c.a = Mathf.Max(0, afterimage.left_time / this.time_afterimage_show * 0.8f);

                afterimage.material.SetColor("_BaseColor", c);
            }
            if (afterimage.left_time <= 0)
                is_need_remove = true;
        }

        if (is_need_remove)
            this.list_afterimage_mesh.RemoveAll(x => x.left_time <= 0);
    }

    public void play()
    {
        if (this.coroutine != null)
            StopCoroutine(this.coroutine);
        this.list_afterimage_mesh.Clear();

        this.coroutine = StartCoroutine(this.start_enumerator());
    }

    public void play(float time_total_duration, float time_interval, float time_afterimage_show)
    {
        this.time_afterimage_show = time_total_duration;
        this.time_interval = time_interval;
        this.time_afterimage_show = time_afterimage_show;

        if (this.coroutine != null)
            StopCoroutine(this.coroutine);
        this.list_afterimage_mesh.Clear();

        this.coroutine = StartCoroutine(this.start_enumerator());
    }

    public void stop()
    {
        if (this.coroutine != null)
            StopCoroutine(this.coroutine);
    }

    public IEnumerator start_enumerator()
    {
        if ((this.array_skinned_mesh_renderer.Length + this.array_mesh_filter.Length) > 0)
        {
            float time = time_total_duration;
            while (time > 0)
            {
                this.create_single_afterimage();
                yield return new WaitForSeconds(this.time_interval);
                //time -= this.time_interval;
            }
        }
        yield return null;
    }

    //combine mesh
    private Mesh combine_all_mesh()
    {
        CombineInstance[] conbine_instacne = new CombineInstance[this.array_skinned_mesh_renderer.Length + this.array_mesh_filter.Length];

        int index = 0;
        for (int i = 0; i < this.array_skinned_mesh_renderer.Length; i++)
        {
            SkinnedMeshRenderer render = this.array_skinned_mesh_renderer[i];
            Mesh mesh = new Mesh();
            render.BakeMesh(mesh);

            conbine_instacne[index] = new CombineInstance
            {
                mesh = mesh,
                transform = render.gameObject.transform.localToWorldMatrix,
                subMeshIndex = 0
            };

            index++;
        }

        for (int i = 0; i < this.array_mesh_filter.Length; i++)
        {

            MeshFilter render = this.array_mesh_filter[i];
            Mesh mesh = Instantiate((render.sharedMesh != null) ? render.sharedMesh : render.mesh);

            conbine_instacne[index] = new CombineInstance
            {
                mesh = mesh,
                transform = render.gameObject.transform.localToWorldMatrix,
                subMeshIndex = 0
            };
            index++;
        }


        Mesh combined_mesh = new Mesh();
        combined_mesh.CombineMeshes(conbine_instacne, true, true);


        return combined_mesh;
    }

    //create single afterimage
    private void create_single_afterimage()
    {
        Afterimage_mesh afterimage_mesh = new Afterimage_mesh()
        {
            mesh = this.combine_all_mesh(),
            //material = this.material_afterimage,
            material = new Material(this.material_afterimage),

            left_time = this.time_afterimage_show,
        };

        this.list_afterimage_mesh.Add(afterimage_mesh);
    }
}
public class Afterimage_mesh
{
    public Mesh mesh;
    public Material material;
    public Matrix4x4 matrix;
    public float show_time;
    public float left_time;
}


