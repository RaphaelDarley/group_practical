using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResizeCubeSlider : MonoBehaviour
{
    public Transform cube;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void ResizeCube(float size)
    {
        cube.localScale = new Vector3(size, size, size);
    }
}
