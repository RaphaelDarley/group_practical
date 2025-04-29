using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDropdown : MonoBehaviour
{

    public GameObject cylinder;
    public GameObject sphere;
    public GameObject capsule;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void ToggleObject(int obj)
    {
        switch(obj)
        {
            case 0: // cylinder
                cylinder.SetActive(true);
                sphere.SetActive(false);
                capsule.SetActive(false);
                break;
            case 1: // sphere
                cylinder.SetActive(false);
                sphere.SetActive(true);
                capsule.SetActive(false);
                break;
            case 2: // capsule
                cylinder.SetActive(false);
                sphere.SetActive(false);
                capsule.SetActive(true);
                break;

        }
    }
}
