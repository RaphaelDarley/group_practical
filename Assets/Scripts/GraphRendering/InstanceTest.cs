using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InstanceTest : MonoBehaviour
{
    [Header("Settings")]
    public int num_batches;
    public int num_instances;
    public Mesh mesh;
    public Material material;
    public float radius;

    // internal settings
    private List<List<Matrix4x4>> batches = new();

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < num_batches; i++)
        {
            batches.Add(CreateBatch());
        }
    }

    // Update is called once per frame
    void Update()
    {
        RenderBatches();
    }

    private void RenderBatches()
    {
        foreach (List<Matrix4x4> batch in batches)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, batch);
        }
    }

    private List<Matrix4x4> CreateBatch()
    {
        List<Matrix4x4> batch = new();
        for (int i = 0; i < num_instances; i++)
        {
            batch.Add(MakeMatrix());
        }

        return batch;
    }

    private Matrix4x4 MakeMatrix()
    {
        return Matrix4x4.TRS(Random.insideUnitSphere * radius, Random.rotationUniform, Random.value * Vector3.one);
    }
}
