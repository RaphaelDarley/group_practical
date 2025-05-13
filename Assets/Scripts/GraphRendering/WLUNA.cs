using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static OVRPlugin;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Runtime.CompilerServices;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using UnityEngine.UI;


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
    public List<float>[] edgeScales;
    public List<float>[] dNodes;
    public List<float>[] sNodes;
    public List<float>[] rNodes;

    public List<Vector3> nodePos;
    public List<Quaternion> nodeRot;
    public List<Vector3> edgePos;
    public List<float> edgeLengths;
    public List<Quaternion> edgeRot;

    public WLUNABatchData()
    {
        edgeScales = new List<float>[16];
        dNodes = new List<float>[16];
        rNodes = new List<float>[16];
        sNodes = new List<float>[16];
        nodePos = new List<Vector3>();
        nodeRot = new List<Quaternion>();
        edgePos = new List<Vector3>();
        edgeLengths = new List<float>();
        edgeRot = new List<Quaternion>();


        for (int i = 0; i < 16; i++)
        {
            edgeScales[i] = new();
            dNodes[i] = new();
            sNodes[i] = new();
            rNodes[i] = new();
        }
    }

}

public class WLUNABatchMatrices
{
    public Matrix4x4[] dNodeMats;
    public Matrix4x4[] sNodeMats;
    public Matrix4x4[] rNodeMats;
    public Matrix4x4[] edgeMats;


    public int N;
    public int E;

    public WLUNABatchMatrices(int N, int E)
    {
        dNodeMats = new Matrix4x4[N];
        sNodeMats = new Matrix4x4[N];
        rNodeMats = new Matrix4x4[N];
        edgeMats = new Matrix4x4[E];

        this.N = N;
        this.E = E;
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
    public float length;

    public WLUNAEdge(Vector3 midpoint, Vector3 up)
    {
        this.midpoint = midpoint;
        this.rotation = Quaternion.FromToRotation(Vector3.up, up);
        this.scales = new float[16];
    }

    public void SetScale(int day, float scale, float length)
    {
        scales[day] = scale;
        this.length = length;
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
    public UnityEngine.Mesh dNodeMesh;
    public UnityEngine.Mesh sNodeMesh;
    public UnityEngine.Mesh rNodeMesh;
    public UnityEngine.Mesh edgeMesh;

    [Header("GameObjects")]
    public TextMeshProUGUI timeLabel;
    public Transform graphParent;
    public GameObject timeSlider;

    [Header("Settings")]
    public float nodeSize;
    public float edgeThickness;


    [Header("Utilities")]
    private List<Transaction> transactions = new List<Transaction>();
    private Dictionary<string, WLUNANode> nodes = new Dictionary<string, WLUNANode>();
    private Dictionary<UnorderedPair<WLUNANode>, WLUNAEdge> edges = new();
    private List<DateTime> sortedDates;

    private WLUNABatchData graphBatchData = new WLUNABatchData();
    private WLUNABatchMatrices batchMatrices;

    // Most to be filled in at runtime
    [Header("Time Variables")]
    private float graphTime = 0.5f;
    public int currentDay = 0;
    public float timeSpeed;
    private float timeLength;
    public bool autoTime;
    public float spawnRadius;



    // Start is called before the first frame update
    void Start()
    {
        timeLength = 1f / timeSpeed;

        if (greenMat == null | redMat == null | defaultMat == null)
        {
            Debug.Log("One or more materials is null");
        }
        //if (nodeMesh == null | edgeMesh == null)
        //{
        //    Debug.Log("One or more meshes is null");
        //}

        InitialiseWLUNA();
        UpdateLabel();
    }

    // Update is called once per frame
    void Update()
    {
        if (autoTime)
        {
            IncrementTime();
        }

        RenderGraph();
    }




    private void InitialiseWLUNA()
    {
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

        this.sortedDates = sortedDates;
        InitialiseBatchData(sortedDates, groupedByTime);
    }



    private void InitialiseBatchData(List<DateTime> sortedDates, Dictionary<DateTime, List<Transaction>> groupedByTime)
    {
        // node data
        foreach (WLUNANode node in nodes.Values)
        {
            Vector3 pos = node.position;
            Quaternion rot = UnityEngine.Random.rotation;
            graphBatchData.nodePos.Add(pos);
            graphBatchData.nodeRot.Add(rot);
            for (int day = 0; day < 16; day++)
            {
                if (node.states[day] == WLUNAState.Default)
                {
                    //Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, 0.05f * Vector3.one);
                    graphBatchData.dNodes[day].Add(0.05f);
                    graphBatchData.sNodes[day].Add(0f);
                    graphBatchData.rNodes[day].Add(0f);
                }

                //Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, 0.07f * Vector3.one);
                else if (node.states[day] == WLUNAState.Sender)
                {
                    graphBatchData.sNodes[day].Add(0.07f);
                    graphBatchData.dNodes[day].Add(0f);
                    graphBatchData.rNodes[day].Add(0f);
                }
                else
                {
                    graphBatchData.rNodes[day].Add(0.07f);
                    graphBatchData.sNodes[day].Add(0f);
                    graphBatchData.dNodes[day].Add(0f);
                }   
                
            }
        }

        // edge data
        foreach (WLUNAEdge edge in edges.Values)
        {
            Vector3 pos = edge.midpoint;
            Quaternion rot = edge.rotation;

            graphBatchData.edgePos.Add(pos);
            graphBatchData.edgeLengths.Add(edge.length);
            graphBatchData.edgeRot.Add(rot);

            for (int day = 0; day < 16; day++)
            {
                float scale = edge.scales[day];
                graphBatchData.edgeScales[day].Add(scale);
            }
        }

        int N = graphBatchData.nodePos.Count;
        int E = graphBatchData.edgePos.Count;

        batchMatrices = new WLUNABatchMatrices(N, E);
    }


    DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        return dateTimeOffset.DateTime;
    }

    Vector3 GetRandomPosition()
    {
        //return new Vector3(UnityEngine.Random.Range(-10f, -2f), UnityEngine.Random.Range(-1f, 3f), UnityEngine.Random.Range(0.5f, 10f));
        Vector3 pos = UnityEngine.Random.insideUnitSphere;
        return pos;// + 6f * Vector3.left;
    }


    void RenderGraph()
    {
        // tween data
        float t = graphTime / timeLength;
        int nextDay = (currentDay + 1) % 16;

        int N = batchMatrices.N;
        int E = batchMatrices.E;

        // nodes
        for (int i = 0; i < N; i++)
        {
            Vector3 pos = graphBatchData.nodePos[i];
            Quaternion rot = graphBatchData.nodeRot[i];

            pos *= spawnRadius;

            float dScale = (1 - t) * graphBatchData.dNodes[currentDay][i] + t * graphBatchData.dNodes[nextDay][i];
            float sScale = (1 - t) * graphBatchData.sNodes[currentDay][i] + t * graphBatchData.sNodes[nextDay][i];
            float rScale = (1 - t) * graphBatchData.rNodes[currentDay][i] + t * graphBatchData.rNodes[nextDay][i];

            dScale *= nodeSize;
            rScale *= nodeSize;
            sScale *= nodeSize;
            
            
            batchMatrices.dNodeMats[i] = Matrix4x4.TRS(pos, rot, new Vector3(dScale, dScale, dScale));
            batchMatrices.sNodeMats[i] = Matrix4x4.TRS(pos, rot, new Vector3(sScale, sScale, sScale));
            batchMatrices.rNodeMats[i] = Matrix4x4.TRS(pos, rot, new Vector3(rScale, rScale, rScale));
        }

        // edges
        for (int i = 0; i < E; i++)
        {
            Vector3 pos = graphBatchData.edgePos[i];
            Quaternion rot = graphBatchData.edgeRot[i];
            float length = graphBatchData.edgeLengths[i];

            pos *= spawnRadius;
            length *= spawnRadius;

            float scale = (1 - t) * graphBatchData.edgeScales[currentDay][i] + t * graphBatchData.edgeScales[nextDay][i];

            scale *= edgeThickness;

            batchMatrices.edgeMats[i] = Matrix4x4.TRS(pos, rot, new Vector3(scale, length, scale));
        }

        Graphics.DrawMeshInstanced(edgeMesh, 0, defaultMat, batchMatrices.edgeMats, E, null, ShadowCastingMode.Off, true);
        Graphics.DrawMeshInstanced(dNodeMesh, 0, defaultMat, batchMatrices.dNodeMats, N, null, ShadowCastingMode.Off, true);
        Graphics.DrawMeshInstanced(rNodeMesh, 0, greenMat, batchMatrices.rNodeMats, N,null, ShadowCastingMode.Off, true);
        Graphics.DrawMeshInstanced(sNodeMesh, 0, redMat, batchMatrices.sNodeMats, N, null, ShadowCastingMode.Off, true);
        //Graphics.DrawMeshInstanced(nodeMesh, 0, redMat, graphBatchData.sNodes[currentDay]);
    }


    public void IncrementTime()
    {
        graphTime += Time.deltaTime;

        if (graphTime > timeLength)
        {
            graphTime = 0f;
            currentDay = (currentDay + 1) % 16;
            UpdateLabel();
        }

        timeSlider.GetComponent<UnityEngine.UI.Slider>().value = graphTime;

    }

    public void DecrementTime()
    {
        graphTime -= Time.deltaTime;

        if (graphTime < 0f)
        {
            graphTime = timeLength - Time.deltaTime;
            currentDay--;
            if (currentDay < 0)
            {
                currentDay = 15;
            }
            UpdateLabel();
        }

        timeSlider.GetComponent<UnityEngine.UI.Slider>().value = graphTime;
    }

    private void UpdateLabel()
    {
        string time = sortedDates[currentDay].ToString("dd-MM-yyyy");
        string text = "Showing Transactions for: " + time;
        timeLabel.text = text;
    }

    public void ChangeNodeSize(float val)
    {
        this.nodeSize = val;
    }

    public void ChangeEdgeSize(float val)
    {
        this.edgeThickness = val;
    }

    public void ChangeSpawnRadius(float val)
    {
        this.spawnRadius = val;
    }

    public void ChangeTimeSpeed(float val)
    {
        this.timeSpeed = val;
        this.timeLength = 1f / timeSpeed;

        timeSlider.GetComponent<UnityEngine.UI.Slider>().maxValue = timeLength;

        graphTime = 0.0f;
    }

    public void ResetWLUNA(bool v)
    {
        graphTime = 0f;
        currentDay = 0;

        nodeSize = 1f;
        edgeThickness = 1f;

        ChangeTimeSpeed(0.4f);

        spawnRadius = 10f;

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
