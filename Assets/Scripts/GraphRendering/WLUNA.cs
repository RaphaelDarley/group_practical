using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Transaction
{
    public string sender;
    public string receiver;
    public float amount;
    public DateTime timestamp;

    public Transaction(string sender, string receiver, float amount, DateTime timestamp)
    {
        this.sender = sender;
        this.receiver = receiver;
        this.amount = amount;
        this.timestamp = timestamp;
    }
}

internal class GraphNode
{

}

internal class GraphEdge
{

}

public class WLUNA : MonoBehaviour
{
    [Header("Filepath")]
    public string transactionFilepath = "GraphData/Wluna_transfers";

    [Header("Materials")]
    public Material greenMat;
    public Material redMat;
    public Material defaultMat;

    [Header("Meshes")]
    public Mesh nodeMesh;
    public Mesh edgeMesh;

    [Header("GameObjects")]
    public TextMeshProUGUI timeLabel;
    private GameObject graphParent;

    [Header("Settings")]
    public float nodeSize;
    public float edgeThickness;
    public float timeSpeed;


    [Header("Utilities")]
    private List<Transaction>[] transactionsPerDay = new List<Transaction>[16]; // 16 days worth of transactions here
    private List<Transaction> transactions = new List<Transaction>();
    private List<List<Matrix4x4>> graphNodes = new List<List<Matrix4x4>>();
    private List<List<Matrix4x4>> graphEdges = new List<List<Matrix4x4>>();


    // Most to be filled in at runtime
    [Header("Time Variables")]
    private int numDays;
    private float deltaTime;


    // Start is called before the first frame update
    void Start()
    {
        if (greenMat == null | redMat == null | defaultMat == null)
        {
            Debug.Log("One or more materials is null");
        }
        if (nodeMesh == null | edgeMesh == null)
        {
            Debug.Log("One or more meshes is null");
        }

        InitialiseWLUNA();
    }

    // Update is called once per frame
    void Update()
    {
        
    }




    private void InitialiseWLUNA()
    {
        graphParent = new GameObject("WLUNAGraph");
        graphParent.transform.parent = this.transform;

        LoadWLUNAData();
    }

    private void LoadWLUNAData()
    {
        TextAsset file = Resources.Load(transactionFilepath) as TextAsset;
        if (file == null)
        {
            Debug.LogError($"Could not load file at Resources/{transactionFilepath}.txt or .csv");
        }

        string[] lines = file.text.Split('\n');
        string[] headers = lines[0].Split(',').Select(h => h.Trim().ToLower().Replace("\"", "")).ToArray();

        int senderIndex = Array.IndexOf(headers, "from_address");
        int receiverIndex = Array.IndexOf(headers, "to_address");
        int timestampIndex = Array.IndexOf(headers, "time_stamp");
        int amountIndex = Array.IndexOf(headers, "value");

        if (senderIndex == -1 || receiverIndex == -1 || timestampIndex == -1 || amountIndex == -1)
        {
            Debug.LogError("CSV is missing one or more required columns.");
        }

        // Load transactions

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length <= Math.Max(timestampIndex, Math.Max(senderIndex, receiverIndex))) continue;

            for (int j = 0; j < row.Length; j++) row[j] = row[j].Trim();

            string sender = row[senderIndex];
            string receiver = row[receiverIndex];
            float amount = float.TryParse(row[amountIndex], out float a) ? a : 1f;

            if (!long.TryParse(row[timestampIndex], out long unixTimestamp)) continue;

            // Normalize timestamps to nearest second
            DateTime timestamp = UnixTimeStampToDateTime(unixTimestamp).AddMilliseconds(-UnixTimeStampToDateTime(unixTimestamp).Millisecond);

            transactions.Add(new Transaction(sender, receiver, amount, timestamp));
        }

        transactions.Sort((t1, t2) => t1.timestamp.CompareTo(t2.timestamp));

        //HashSet<string> uniqueAddresses = new HashSet<string>();
        //foreach (var transaction in transactions)
        //{
        //    uniqueAddresses.Add(transaction.sender);
        //    uniqueAddresses.Add(transaction.receiver);
        //}

        //foreach (string address in uniqueAddresses)
        //{
        //    GameObject node = Instantiate(NodePrefab, GetRandomPosition(), Quaternion.identity, transform);
        //    node.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        //    node.name = address;
        //    nodes[address] = node;
        //}

        //Dictionary<DateTime, List<Transaction>> groupedByTime = GroupTransactionsByTime();

        //Debug.Log($"Grouped into {groupedByTime.Count} timestamps with total {transactions.Count} transactions.");

    }

    DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        return dateTimeOffset.DateTime;
    }
}
