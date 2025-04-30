using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphSpawnerCopy : MonoBehaviour
{
    public GameObject prefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject datapoint = Instantiate(
            prefab,
            new Vector3(1,1,1),
            Quaternion.identity,
            transform
        );
    }
}
