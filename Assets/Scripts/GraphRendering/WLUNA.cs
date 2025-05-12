using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static OVRPlugin;
using UnityEngine.Rendering;


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


public class WLUNABatchData
{
    public List<Matrix4x4>[] edges;
    public List<Matrix4x4>[] dNodes;
    public List<Matrix4x4>[] sNodes;
    public List<Matrix4x4>[] rNodes;

    public WLUNABatchData()
    {
        this.edges = new List<Matrix4x4>[16];
        this.dNodes = new List<Matrix4x4>[16];
        this.sNodes = new List<Matrix4x4>[16];
        this.rNodes = new List<Matrix4x4>[16];

        for(int i = 0; i < 16; i++)
        {
            edges[i] = new();
            dNodes[i] = new();
            sNodes[i] = new();
            rNodes[i] = new();
        }
    }

}

public class WLUNANode
{
    public Vector3 position;
    public WLUNAState[] states;

    public WLUNANode(Vector3 position)
    {
        this.position = position;
        this.states = new WLUNAState[16];
    }

    public void SetState(int day, WLUNAState state)
    {
        states[day] = state;
    }
}

public class WLUNAEdge
{
    public Vector3 midpoint;
    public Quaternion rotation;
    public float[] scales;
    public float[] lengths;

    public WLUNAEdge(Vector3 midpoint, Vector3 up)
    {
        this.midpoint = midpoint;
        this.rotation = Quaternion.FromToRotation(Vector3.up, up);
        this.scales = new float[16];
        this.lengths = new float[16];
    }

    public void SetScale(int day, float scale, float length)
    {
        scales[day] = scale;
        lengths[day] = length;
    }
}

public enum WLUNAState
{
    Default,
    Sender,
    Receiver,
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
    public UnityEngine.Mesh nodeMesh;
    public UnityEngine.Mesh edgeMesh;

    [Header("GameObjects")]
    public TextMeshProUGUI timeLabel;
    private GameObject graphParent;

    [Header("Settings")]
    public float nodeSize;
    public float edgeThickness;
    public float timeSpeed;


    [Header("Utilities")]
    private List<Transaction> transactions = new List<Transaction>();
    private Dictionary<string, WLUNANode> nodes = new Dictionary<string, WLUNANode>();
    private Dictionary<UnorderedPair<WLUNANode>, WLUNAEdge> edges = new();

    private WLUNABatchData graphBatchData = new WLUNABatchData();


    // Most to be filled in at runtime
    [Header("Time Variables")]
    private int numDays;
    private float time = 0;
    private int currentDay = 0;


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
        time += Time.deltaTime;
        if (time > 2f)
        {
            time = 0f;
            currentDay = (currentDay + 1) % 16;
        }

        RenderGraph();
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

        HashSet<string> uniqueAddresses = new HashSet<string>();
        foreach (var transaction in transactions)
        {
            uniqueAddresses.Add(transaction.sender);
            uniqueAddresses.Add(transaction.receiver);
        }

        foreach (string address in uniqueAddresses)
        {
            Vector3 position = GetRandomPosition();
            WLUNANode graphnode = new WLUNANode(position);
            nodes[address] = graphnode;
        }

        
        Dictionary<DateTime, List<Transaction>> groupedByTime = GroupTransactionsByTime();
        List<DateTime> sortedDates = new List<DateTime>(groupedByTime.Keys);

        InitialiseNodesAndEdges(sortedDates, groupedByTime);
    }

    Dictionary<DateTime, List<Transaction>> GroupTransactionsByTime()
    {
        Dictionary<DateTime, List<Transaction>> grouped = new Dictionary<DateTime, List<Transaction>>();

        foreach (var tx in transactions)
        {
            // Strip the time part from the timestamp to group by day
            DateTime dayOnly = tx.timestamp.Date;

            if (!grouped.ContainsKey(dayOnly))
                grouped[dayOnly] = new List<Transaction>();

            grouped[dayOnly].Add(tx);
        }

        return grouped;
    }

    private void InitialiseNodesAndEdges(List<DateTime> sortedDates, Dictionary<DateTime, List<Transaction>> groupedByTime)
    {
        int dayIndex = 0;
        foreach (var day in sortedDates)
        {
            List<Transaction> transactionsForDay = groupedByTime[day];

            foreach (var tx in transactionsForDay)
            {
                // set node values
                if (!nodes.ContainsKey(tx.sender) || !nodes.ContainsKey(tx.receiver)) continue;

                WLUNANode senderNode = nodes[tx.sender];
                WLUNANode receiverNode = nodes[tx.receiver];

                senderNode.SetState(dayIndex, WLUNAState.Sender);
                receiverNode.SetState(dayIndex, WLUNAState.Receiver);


                // set edge values
                UnorderedPair<WLUNANode> edgePair = new UnorderedPair<WLUNANode>(senderNode, receiverNode);

                Vector3 senderPos = senderNode.position;
                Vector3 receiverPos = receiverNode.position;
                Vector3 direction = receiverPos - senderPos;
                float distance = direction.magnitude;

                Vector3 edgePos = (senderPos + receiverPos) / 2f;
                Vector3 edgeUp = direction.normalized;
                float edgeScale = Mathf.Clamp(tx.amount / 1500f, 0.01f, 0.03f);
                float edgeLength = distance;


                // check if edge exists, if not, make it
                if (!edges.ContainsKey(edgePair))
                {
                    WLUNAEdge edge = new WLUNAEdge(edgePos, edgeUp);
                    edges.Add(edgePair, edge);
                }
                edges[edgePair].SetScale(dayIndex, edgeScale, edgeLength);
            }

            dayIndex++;
        }

        InitialiseBatchData(sortedDates, groupedByTime);
    }



    // SINGLE BATCH, NO TWEEN POSSIBILITY, REFACTOR
    private void InitialiseBatchData(List<DateTime> sortedDates, Dictionary<DateTime, List<Transaction>> groupedByTime)
    {
        // node data
        foreach (WLUNANode node in nodes.Values)
        {
            Vector3 pos = node.position;
            for (int day = 0; day < 16; day++)
            {
                if (node.states[day] == WLUNAState.Default)
                {
                    Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, 0.05f * Vector3.one);
                    graphBatchData.dNodes[day].Add(matrix);
                }
                else
                {
                    Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, 0.07f * Vector3.one);
                    if (node.states[day] == WLUNAState.Sender)
                    {
                        graphBatchData.sNodes[day].Add(matrix);
                    }
                    else
                    {
                        graphBatchData.rNodes[day].Add(matrix);
                    }
                }
                
            }
        }

        // edge data
        foreach (WLUNAEdge edge in edges.Values)
        {
            Vector3 pos = edge.midpoint;
            Quaternion rot = edge.rotation;
            for (int day = 0; day < 16; day++)
            {
                float scale = edge.scales[day];
                float length = edge.lengths[day];
                if (scale > 0f)
                {
                    graphBatchData.edges[day].Add(Matrix4x4.TRS(pos, rot, new Vector3(scale, length /* /2f */, scale))); // divide length by 2 if using cylinder mesh
                }
            }
        }
    }


    DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        return dateTimeOffset.DateTime;
    }

    Vector3 GetRandomPosition()
    {
        return new Vector3(UnityEngine.Random.Range(-10f, -2f), UnityEngine.Random.Range(-1f, 3f), UnityEngine.Random.Range(0.5f, 10f));
    }





    void RenderGraph()
    {

        Graphics.DrawMeshInstanced(edgeMesh, 0, defaultMat, graphBatchData.edges[currentDay]);
        Graphics.DrawMeshInstanced(nodeMesh, 0, defaultMat, graphBatchData.dNodes[currentDay]);
        Graphics.DrawMeshInstanced(nodeMesh, 0, greenMat, graphBatchData.rNodes[currentDay]);
        Graphics.DrawMeshInstanced(nodeMesh, 0, redMat, graphBatchData.sNodes[currentDay]);
    }
}

#nullable enable
public class UnorderedPair<T> : IEquatable<UnorderedPair<T>>
{
    private static IEqualityComparer<T> comparer = EqualityComparer<T>.Default;

    public T X { get; }
    public T Y { get; }

    public UnorderedPair(T x, T y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(UnorderedPair<T>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // For equality simply include the swapped check
        return
                comparer.Equals(X, other.X) && comparer.Equals(Y, other.Y)
            ||
                comparer.Equals(X, other.Y) && comparer.Equals(Y, other.X);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as UnorderedPair<T>);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return
                    (X is null ? 0 : comparer.GetHashCode(X))
                +
                    (Y is null ? 0 : comparer.GetHashCode(Y));
        }
    }

    public static bool operator ==(UnorderedPair<T>? left, UnorderedPair<T>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(UnorderedPair<T>? left, UnorderedPair<T>? right)
    {
        return !Equals(left, right);
    }
}
#nullable disable

