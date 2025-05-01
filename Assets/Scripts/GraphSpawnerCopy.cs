using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class GraphSpawnerCopy : MonoBehaviour
{
    // CSV file to read, treating project directory as root folder
    // Can safely reorder columns but must contain "stablecoin" "high" and "low" headers
    // ! Currently assumes consistent initial timestamp and interval for each coin
    // ! Data for each coin must be in a continuous block
    public string Filepath = "Assets/Data/Stablecoins_dataset.csv";

    // Material for the graph
    public Material graphMat;

    // Height scaling (height of value=1)
    public float GlobalHeightScale = 1;

    // Array of relative heights for each coin, will be multiplied by global height
    // Useful if coins of different orders of magnitude need to be compared
    // Extra scales of 1 will be used if more coins than entries
    public float[] RelativeHeightScale = {1};

    // Width between data for different coins
    public float CoinGap = 5;

    // Horizontal gap between data entries
    public float TimeEntryWidth = 0.2f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Read CSV file
        string[] lines = File.ReadAllLines(Filepath);

        // Get indices of relevant column headers
        string[] headers = lines[0].Split(',');
        int timestamp = Array.IndexOf(headers, "timestamp");
        int high = Array.IndexOf(headers, "high");
        int low = Array.IndexOf(headers, "low");
        int stablecoin = Array.IndexOf(headers, "stablecoin");

        // Get dimensions of csv array, excluding header row
        int length = lines.Length-1;
        int width = headers.Length;

        // Split each row into entries, excluding header row
        List<string[]> rows = new List<string[]>();
        for (int i=1; i<=length; i++)
        {
            rows.Add(lines[i].Split(','));
        }

        // Generate vertices from rows
        Vector3[] vertices = new Vector3[length*2];
        string currentcoin = rows[0][stablecoin];
        float timepos = 0;
        int coinnum = 0;
        float coinpos = 0; // Storing separately to avoid unneccessary calculation, but should always equal coinnum*CoinGap
        float heightscale = GlobalHeightScale*RelativeHeightScale[coinnum];
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
            float h = float.Parse(row[high])*heightscale;
            float l = float.Parse(row[low])*heightscale;
        // Create vertices for high and low point
            vertices[2*i] = new Vector3(timepos,h,coinpos);
            vertices[2*i+1] = new Vector3(timepos,l,coinpos);
        // Move x position
            timepos = timepos+TimeEntryWidth;
        }

        // Turn vertices into triangles
        int[] triangles = new int[length*12-6];
        for (int i=0; i<(length*2-2); i++)
        {
            triangles[6*i] = i;
            triangles[6*i+1] = i+1;
            triangles[6*i+2] = i+2;
            triangles[6*i+3] = i+2;
            triangles[6*i+4] = i+1;
            triangles[6*i+5] = i;
        }

        foreach (var item in vertices)
        {
            Console.Write($" ==> {item}");
        }
        foreach (var item in triangles)
        {
            Console.Write($" ==> {item}");
        }

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