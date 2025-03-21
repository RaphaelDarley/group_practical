using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class CustomInputScript : MonoBehaviour
{
    public InputActionReference cubeButton;
    public InputActionReference cylinderButton;
    public InputActionReference sphereStick;
    public InputActionReference playerStick;

    public MeshRenderer cube;
    public Transform cylinder;
    public Transform sphere;

    public Transform player;


    // Start is called before the first frame update
    void Start()
    {
        cubeButton.action.started += HideCube;
        cubeButton.action.canceled += ShowCube;

        cylinderButton.action.canceled += ResetCylinder;

        //sphereStick.action.performed += MoveSphere;
        playerStick.action.performed += RotatePlayer;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ScaleCylinder()
    {
        cylinder.localScale *= 1.05f;
    }

    void ResetCylinder(InputAction.CallbackContext context)
    {
        cylinder.localScale = Vector3.one;
    }

    void HideCube(InputAction.CallbackContext context)
    {
        cube.enabled = false;
    }


    void ShowCube(InputAction.CallbackContext context)
    {
        cube.enabled = true;
    }


    void MoveSphere(InputAction.CallbackContext context)
    {
        Vector2 vector = context.ReadValue<Vector2>();
        Vector3 movement = new Vector3(vector.x, 0f, vector.y);
        movement *= 0.2f;
        sphere.transform.position += movement;
    }

    void RotatePlayer(InputAction.CallbackContext context)
    {
        Vector2 vector = context.ReadValue<Vector2>();
        float yaw = vector.x;
        float pitch = -vector.y;

        float rotationPitch = player.localEulerAngles.x;

        float rotationYaw = player.localEulerAngles.y + yaw;

        rotationPitch += pitch;

        player.localEulerAngles = new Vector3(rotationPitch, rotationYaw, 0.0f);
    }

}
