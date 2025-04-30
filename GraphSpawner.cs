
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GraphSpawner : MonoBehaviour
{
    // Make prefab object available
    public GameObject prefab;

    // Constants for adjusting display
    public int Starttime = 0;

    // Datapoint class with timestamp and value attributes
    public class Datapoint
    {
        public int timestamp;
        public float open;
        public Transform t;

        public Datapoint(int ts, float o)
        {
            timestamp = ts;
            open = o;
            Draw();
        }

        public void Draw()
        {
            GameObject g = Instantiate(
                prefab,
                new Vector3(timestamp, open, 0),
                Quaternion.Identity,
                transform
            );
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        public Datapoint myObj = new Datapoint(3, 4);
        Debug.Log(myObj.timestamp);
    }

}
