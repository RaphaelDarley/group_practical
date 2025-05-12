using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnifiedGraphSpawner : MonoBehaviour
{
    // === FILEPATHS ===
    public string stablecoinFilepath = "GraphData/Stablecoins_dataset";
    public string transactionFilepath = "GraphData/Wluna_transfers";

    // === GRAPH MATERIALS ===
    public Material graphMat;

    // === PREFABS ===
    public GameObject NodePrefab;
    public GameObject EdgePrefab;
    public GameObject PosPrefab;
    public GameObject NegPrefab;


    // === STABLECOIN GRAPH SETTINGS ===
    public float MinLineThickness = 0.05f;
    public float GlobalHeightScale = 1;
    public float[] RelativeHeightScale = { 1 };
    public float HeightZero = 0;
    public float CoinGap = 5;
    public float TimeEntryWidth = 0.2f;

    private Coroutine timeStepCoroutine;


    public string first;
    public string last;
    public int debug;

    // === VARIABLE FOR PAUSING/RESUMING THE GRAPH ANIMATION ===
    private bool isRunning = true;

    void Start()
    {
        if (NodePrefab == null || EdgePrefab == null || PosPrefab == null || NegPrefab == null)
        {
            Debug.LogError("One or more prefabs not assigned in the inspector.");
            return;
        }

        GameObject stablecoinGraph = new GameObject("StablecoinGraph");
        stablecoinGraph.transform.parent = this.transform.parent;
        RenderStablecoinGraph(stablecoinFilepath, stablecoinGraph);

        //StartCoroutine(LoadAndRenderGraph(transactionFilepath));
    }

    // ============================ STABLECOIN GRAPH ============================
    private void RenderStablecoinGraph(string filepath, GameObject parent)
    {
        TextAsset file = Resources.Load(filepath) as TextAsset;
        string[] lines = (file.text.TrimEnd()).Split("\n");

        string[] headers = lines[0].Split(',');
        int timestamp = Array.IndexOf(headers, "timestamp");
        int high = Array.IndexOf(headers, "high");
        int low = Array.IndexOf(headers, "low");
        int stablecoin = Array.IndexOf(headers, "stablecoin");
        int event_result = Array.IndexOf(headers, "event_result");

        int length = lines.Length - 1;
        List<string[]> rows = new List<string[]>();
        for (int i = 1; i <= length; i++)
            rows.Add(lines[i].Split(','));

        Vector3[] vertices = new Vector3[length * 2];
        int[] triangles = new int[length * 12];
        string currentcoin = rows[0][stablecoin];
        float timepos = 0;
        int coinnum = 0;
        float coinpos = 0;
        float heightscale = GlobalHeightScale * RelativeHeightScale[0];

        for (int i = 0; i < length; i++)
        {
            string[] row = rows[i];
            if (row[stablecoin] != currentcoin)
            {
                coinnum++;
                coinpos += CoinGap;
                timepos = 0;
                currentcoin = row[stablecoin];
                heightscale = (coinnum < RelativeHeightScale.Length)
                    ? GlobalHeightScale * RelativeHeightScale[coinnum]
                    : GlobalHeightScale;
            }

            float h = float.Parse(row[high]) * heightscale + HeightZero;
            float l = float.Parse(row[low]) * heightscale + HeightZero;
            if (h == l) l = h - MinLineThickness;

            vertices[2 * i] = new Vector3(timepos, h, coinpos);
            vertices[2 * i + 1] = new Vector3(timepos, l, coinpos);

            if (timepos > 0)
            {
                int idx = 12 * i;
                int a = 2 * i, b = 2 * i + 1, c = a - 2, d = a - 1;
                triangles[idx] = c; triangles[idx + 1] = d; triangles[idx + 2] = a;
                triangles[idx + 3] = a; triangles[idx + 4] = d; triangles[idx + 5] = c;
                triangles[idx + 6] = d; triangles[idx + 7] = a; triangles[idx + 8] = b;
                triangles[idx + 9] = b; triangles[idx + 10] = a; triangles[idx + 11] = d;
            }

            if (row[event_result] == "positive")
                Instantiate(PosPrefab, vertices[2 * i], Quaternion.identity, parent.transform);
            else if (row[event_result] == "negative")
                Instantiate(NegPrefab, vertices[2 * i], Quaternion.identity, parent.transform);

            timepos += TimeEntryWidth;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        var meshFilter = parent.AddComponent<MeshFilter>();
        var meshRenderer = parent.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        meshRenderer.material = graphMat;
    }


}