using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    // === UI ELEMENT FOR TIMESTAMP DISPLAY ===
    public Text TimestampText;

    // === STABLECOIN GRAPH SETTINGS ===
    public float MinLineThickness = 0.05f;
    public float GlobalHeightScale = 1;
    public float[] RelativeHeightScale = { 1 };
    public float HeightZero = 0;
    public float CoinGap = 5;
    public float TimeEntryWidth = 0.2f;

    // === TRANSACTION GRAPH SETTINGS ===
    public float nodeSize = 0.005f;
    public float edgeThickness = 0.003f;

    // === TRANSACTION GRAPH UTILITIES ===
    private Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();
    private List<Transaction> transactions = new List<Transaction>();
    private List<GameObject> currentEdges = new List<GameObject>();

    private Coroutine timeStepCoroutine;

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

        StartCoroutine(LoadAndRenderGraph(transactionFilepath));
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

    // ============================ TRANSACTION GRAPH ============================
    IEnumerator LoadAndRenderGraph(string filepath)
    {
        TextAsset file = Resources.Load(filepath) as TextAsset;
        if (file == null)
        {
            Debug.LogError($"Could not load file at Resources/{filepath}.txt or .csv");
            yield break;
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
            yield break;
        }

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
            GameObject node = Instantiate(NodePrefab, GetRandomPosition(), Quaternion.identity, transform);
            node.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            node.name = address;
            nodes[address] = node;
        }

        Dictionary<DateTime, List<Transaction>> groupedByTime = GroupTransactionsByTime();

        Debug.Log($"Grouped into {groupedByTime.Count} timestamps with total {transactions.Count} transactions.");

        // Begin coroutine to step through timestamps
        timeStepCoroutine = StartCoroutine(StepThroughTime(groupedByTime));
    }

    IEnumerator StepThroughTime(Dictionary<DateTime, List<Transaction>> groupedByTime)
    {
        List<DateTime> sortedDates = new List<DateTime>(groupedByTime.Keys);
        sortedDates.Sort();

        // Loop through each day, showing all edges for that day
        while(isRunning) {
            foreach (var day in sortedDates)
            {
                if (!isRunning) yield break;

                // --- UI Update for Day ---
                if (TimestampText != null)
                    TimestampText.text = day.ToString("yyyy-MM-dd");

                ClearCurrentEdges();

                // Reset all nodes to default appearance
                foreach (var key in new List<string>(nodes.Keys))
                {
                    ChangeNodeColor(nodes[key], NodePrefab);
                }


                DisplayTransactions(groupedByTime[day]);

                yield return new WaitForSeconds(2f); // Wait 2 seconds before showing the next day
            }
        }
    }

    // Helper function to display all edges for transactions on a specific day
    void DisplayTransactions(List<Transaction> transactionsForDay)
    {   

        // Display the actual transactions with the edges ands the colours of the nodes
        foreach (var tx in transactionsForDay)
        {
            Debug.Log("Entered the loop for the edges");
            if (!nodes.ContainsKey(tx.sender) || !nodes.ContainsKey(tx.receiver)) continue;

            GameObject senderNode = nodes[tx.sender];
            GameObject receiverNode = nodes[tx.receiver];
            
            ChangeNodeColor(senderNode, PosPrefab);
            ChangeNodeColor(receiverNode, NegPrefab);

            GameObject edge = Instantiate(EdgePrefab, transform);
            Vector3 senderPos = senderNode.transform.position;
            Vector3 receiverPos = receiverNode.transform.position;
            Vector3 direction = receiverPos - senderPos;
            float distance = direction.magnitude;

            float scaleFactor = Mathf.Clamp(tx.amount / 1500f, 0.01f, 0.03f);

            edge.transform.position = (senderPos + receiverPos) / 2f;
            edge.transform.up = direction.normalized;
            edge.transform.localScale = new Vector3(scaleFactor, distance / 2f, scaleFactor);

            currentEdges.Add(edge);
        }
    }


    void ClearCurrentEdges()
    {
        foreach (GameObject edge in currentEdges)
        {
            Destroy(edge);
        }
        currentEdges.Clear();
    }

    void ChangeNodeColor(GameObject node, GameObject prefab)
    {
        if (node == null) return;

        // Get the Renderer component
        Renderer renderer = node.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a new instance of the material
            Material newMaterial = new Material(renderer.sharedMaterial);

            // Change the color of the new material instance
            newMaterial.color = prefab.GetComponent<Renderer>().sharedMaterial.color;

            // Apply the new material to the node
            renderer.material = newMaterial;

            if(prefab != NodePrefab)
            {
                node.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            }
            else 
            {
                node.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }
        }

    }




    public void ToggleRunning()
    {
        isRunning = !isRunning;

        if (isRunning && timeStepCoroutine == null)
        {
            Debug.Log("Restarting graph playback");
            StartCoroutine(StepThroughTime(GroupTransactionsByTime()));
        }
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


    DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        return dateTimeOffset.DateTime;
    }

    Vector3 GetRandomPosition()
    {
        return new Vector3(UnityEngine.Random.Range(-10f,-2f), UnityEngine.Random.Range(-1f, 3f), UnityEngine.Random.Range(0.5f, 10f));
    }
}

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
