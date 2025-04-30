using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class GraphSpawnerCopy : MonoBehaviour
{
    // CSV file to read, treating project directory as root folder
    public string filepath = "Assets/Data/Stablecoins_dataset.csv";

    // Prefab for data points
    public GameObject prefab;

    // Width between data for different coins
    public int gap = 5;
    public int heightscale = 10;

    // Function to instantiate a data point at the given coordinates
    public GameObject MakePoint(float x, float y, float z)
    {
        GameObject datapoint = Instantiate(
            prefab,
            new Vector3(x,y,z),
            Quaternion.identity,
            transform
        );
        return datapoint;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Read CSV file
        string[] lines = File.ReadAllLines(filepath);

        // Get indices of relevant column headers
        string[] headers = lines[0].Split(',');
        int timestamp = Array.IndexOf(headers, "timestamp");
        int open = Array.IndexOf(headers, "open");
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

        // Extract initial timestamp
        float starttime = float.Parse(rows[0][timestamp]);

        //Create array of datapoint game objects
        GameObject[] datapoints = new GameObject[length];
        string currentcoin = rows[0][stablecoin];
        int z = 0;
        int x = 0;
        for (int i = 0; i < length; i++) 
        {
            string[] row = rows[i];
            if (row[stablecoin]!=currentcoin)
            {
                z = z+gap;
                x = 0;
                currentcoin = row[stablecoin];
            }
            float y = float.Parse(row[open])*heightscale;
            datapoints[i] = MakePoint(x,y,z);
            x++;
        }
    }
}