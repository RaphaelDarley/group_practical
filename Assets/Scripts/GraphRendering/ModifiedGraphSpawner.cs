using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class ModfiedGraphSpawner : MonoBehaviour
{

    // CSV file to read, treating project directory as root folder
    // Can safely reorder columns but must contain "stablecoin" "high" and "low" headers
    // ! Currently assumes consistent initial timestamp and interval for each coin
    // ! Data for each coin must be in a continuous block
    public string filepath1 = "GraphData/dai_only";
    public string filepath2 = "GraphData/Stablecoins_dataset";

    // ================================== to be used for toggling graphs, ignore these for now =================================
    private GameObject graph1;
    private GameObject graph2;

    // Material for the graph
    public Material graphMat;

    // Prefabs for positive and negative event markers
    public GameObject PosPrefab;
    public GameObject NegPrefab;

    // Minimum line width to ensure it's actually visible even if it would have zero width
    public float MinLineThickness = 0.05f;

    // Height scaling (height of value=1)
    public float GlobalHeightScale = 1;

    // Array of relative heights for each coin, will be multiplied by global height
    // Useful if coins of different orders of magnitude need to be compared
    // Extra scales of 1 will be used if more coins than entries
    // ! Must contain at least one entry
    public float[] RelativeHeightScale = {1};

    // Height of 0 value, used to translate all data up or down for easier view
    public float HeightZero = 0;

    // Width between data for different coins
    public float CoinGap = 5;

    // Horizontal gap between data entries
    public float TimeEntryWidth = 0.2f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        graph1 = new GameObject("graph1");
        graph2 = new GameObject("graph2");

        graph1.transform.parent = this.transform.parent;
        graph2.transform.parent = this.transform.parent;

        //RenderGraph(filepath1, graph1);
        RenderGraph(filepath2, graph2);

        //graph2.SetActive(false);
    }

    // ignore the parent attribute thats also for turning stuff on and off
    private void RenderGraph(string filepath, GameObject parent)
    {
        // Read CSV file
        TextAsset file = Resources.Load(filepath) as TextAsset;
        string[] lines = (file.text.TrimEnd()).Split("\n");

        // Get indices of relevant column headers
        string[] headers = lines[0].Split(',');
        int timestamp = Array.IndexOf(headers, "timestamp");
        int high = Array.IndexOf(headers, "high");
        int low = Array.IndexOf(headers, "low");
        int stablecoin = Array.IndexOf(headers, "stablecoin");
        int event_result = Array.IndexOf(headers, "event_result");

        // Get dimensions of csv array, excluding header row
        int length = lines.Length-1;
        int width = headers.Length;

        // Split each row into entries, excluding header row
        List<string[]> rows = new List<string[]>();
        for (int i=1; i<=length; i++)
        {
            rows.Add(lines[i].Split(','));
        }

        // Generate vertices, triangles and event markers from rows
        Vector3[] vertices = new Vector3[length*2]; //Coordinates of high and low values for each timestamp and coin
        int[] triangles = new int[length*12];       //Joining high and low points of adjacent timestamps with quadrilaterals. Will have a few trailing zeros left at the end, but just creates extra triangles with zero area so won't render
        List<GameObject> events = new List<GameObject>();   //Store references to event marker objects
        string currentcoin = rows[0][stablecoin];   //Name of current coin so we can see when it's changed
        float timepos = 0;                          //x coordinate, equals (current_timestamp - initial_timestamp)*TimeEntryWidth/400
        int coinnum = 0;                            //Counts how many different coins we've done, used for fetching relative scaling data
        float coinpos = 0;                          //z coordinate. equals coinnum*CoinGap (storing separately to avoid repeat calculation)
        float heightscale = GlobalHeightScale*RelativeHeightScale[coinnum]; //Height multiplier for current coin. By default RelativeHeightScale will contain at least one entry so shouldn't throw an error
        // Iterate through rows
        for (int i = 0; i < length; i++) 
        {
            string[] row = rows[i];
        // Reset timepos and move to next coinpos if coin has changed
            if (row[stablecoin]!=currentcoin)
            {
                coinnum ++;
                coinpos = coinpos+CoinGap;
                timepos = 0;
                currentcoin = row[stablecoin];
                try
                {
                    heightscale = GlobalHeightScale*RelativeHeightScale[coinnum];
                }
                catch
                {
                    heightscale = GlobalHeightScale;
                }
            }
        // Read and scale high and low values for time interval
            float h = float.Parse(row[high])*heightscale+HeightZero;
            float l = float.Parse(row[low])*heightscale+HeightZero;
            if (h==l) 
            {
                l = h-MinLineThickness;
            }
        // Create vertices for high and low point
            vertices[2*i] = new Vector3(timepos,h,coinpos);
            vertices[2*i+1] = new Vector3(timepos,l,coinpos);
        // If not the start of a new coin, connect to previous vertices with triangles
            if (timepos>0)
            {
                triangles[12*i] = 2*i-2;         // Can prob improve with iteration
                triangles[12*i+1] = 2*i-1;
                triangles[12*i+2] = 2*i;
                triangles[12*i+3] = 2*i;
                triangles[12*i+4] = 2*i-1;
                triangles[12*i+5] = 2*i-2;
                triangles[12*i+6] = 2*i-1;     
                triangles[12*i+7] = 2*i;
                triangles[12*i+8] = 2*i+1;
                triangles[12*i+9] = 2*i+1;
                triangles[12*i+10] = 2*i;
                triangles[12*i+11] = 2*i-1;
            }
        // If event at this timestamp, add marker
            if (row[event_result]=="positive")
            {
                events.Add(Instantiate(
                    PosPrefab,
                    vertices[2*i],          //Place marker centred at top of line. May change later
                    Quaternion.identity,
                    transform
                ));
            }
            if (row[event_result]=="negative")
            {
                events.Add(Instantiate(
                    NegPrefab,
                    vertices[2*i],
                    Quaternion.identity,
                    transform
                ));
            }
        // Move x position
            timepos = timepos+TimeEntryWidth;
        }


        foreach (Vector3 vertex in vertices) {print(vertex);}

        // Render mesh
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshFilter.sharedMesh = mesh;
        meshRenderer.material = graphMat;
    }
}