using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using Newtonsoft.Json;

public class PCASpawner : MonoBehaviour
{

    [Header("Meshes")]
    public UnityEngine.Mesh nodeMesh;

    [Header("Materials")]
    public Material greenMat;
    public Material redMat;
    public Material defaultMat;

    [Header("Data")]
    public TextAsset file;

    public float positionScale = 1f;

    public string filepath = "GraphData/Pca_companies";

    public int sectorHighlight = 0;

    // ================================== to be used for toggling graphs, ignore these for now =================================
    private GameObject graph;

    // Minimum line width to ensure it's actually visible even if it would have zero width
    public float MinLineThickness = 0.05f;

    // Height scaling (height of value=1)
    public float GlobalHeightScale = 1;

    // Array of relative heights for each coin, will be multiplied by global height
    // Useful if coins of different orders of magnitude need to be compared
    // Extra scales of 1 will be used if more coins than entries
    // ! Must contain at least one entry
    public float[] RelativeHeightScale = { 1 };

    // Height of 0 value, used to translate all data up or down for easier view
    public float HeightZero = 0;

    public List<CompanyInfo> companies = new List<CompanyInfo>();
    public Dictionary<string, List<Matrix4x4>> sectorNodes = new();
    public List<string> sectors = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //graph = new GameObject("pca_graph");

        //graph.transform.parent = this.transform.parent;

        // Load data file
        // TextAsset file = Resources.Load(filepath) as TextAsset;



        // Debug.Log($"LOG: {file.text}");

        string[] lines = (file.text.TrimEnd()).Split("\n");

        int length = lines.Length - 1;


        for (int i = 1; i <= length; i++)
        {
            CompanyInfo tmp = CompanyInfo.ParseSemiSeperated(lines[i]);
            RegisterCompany(tmp);
        }

        RenderGraph(companies, graph);
    }

    void RegisterCompany(CompanyInfo company)
    {
        if (!sectorNodes.ContainsKey(company.sector))
        {
            sectorNodes[company.sector] = new List<Matrix4x4>();
            sectors.Add(company.sector);
        }
        sectorNodes[company.sector].Add(company.Node(positionScale));
        companies.Add(company);
    }

    void Update()
    {
        RenderGraph(companies, graph);
    }

    // ignore the parent attribute thats also for turning stuff on and off
    private void RenderGraph(List<CompanyInfo> companies, GameObject parent)
    {
        // List<Matrix4x4> nodes = new List<Matrix4x4>();

        // Debug.Log("hi");

        // for (int i=0; i < companies.Count; i++) {
        //     Matrix4x4 trans = Matrix4x4.TRS(companies[i].Position(), Quaternion.identity, 0.03f * Vector3.one);
        //     nodes.Add(trans);
        // }

        // nodes.Add(Matrix4x4.identity);

        for (int i = 0; i < sectors.Count; i++)
        {
            Material toUse = defaultMat;
            if (i == sectorHighlight % sectors.Count)
            {
                toUse = redMat;
            }
            Graphics.DrawMeshInstanced(nodeMesh, 0, toUse, sectorNodes[sectors[i]]);
        }
    }

    public void CycleSectors()
    {
        sectorHighlight = (sectorHighlight + 1) % sectors.Count;
    }
}

public class CompanyInfo
{
    public string name;
    public string index;
    public string industry;
    public string sector;
    public float pca_0;
    public float pca_1;
    public float pca_2;

    public static CompanyInfo ParseSemiSeperated(string line)
    {
        string[] parts = line.Split(";");

        CompanyInfo company = new CompanyInfo
        {
            name = parts[0],
            index = parts[1],
            industry = parts[2],
            sector = parts[3],
            pca_0 = float.Parse(parts[4]),
            pca_1 = float.Parse(parts[5]),
            pca_2 = float.Parse(parts[6])
        };

        return company;
    }

    public Vector3 Position()
    {
        return new Vector3(pca_0, pca_1, pca_2);
    }

    private Vector3 offsetPos = new Vector3(-50f, 0f, -10f);

    public Matrix4x4 Node(float positionScale)
    {
        return Matrix4x4.TRS(12f * positionScale * Position() + offsetPos, Quaternion.identity, 0.3f * Vector3.one);
    }

    void Process()
    {

    }


}