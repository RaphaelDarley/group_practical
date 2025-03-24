using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class VRControls : MonoBehaviour
{
    public InputActionReference rightThumbstick;
    public InputActionReference rightThumbstickButton;
    public Transform player;

    [Header("Settings")]
    public float turnSpeed = 1.0f;
    public bool forceUniaxialTurning = false;

    // Start is called before the first frame update
    void Start()
    {
        rightThumbstick.action.performed += RotateCamera;
        rightThumbstickButton.action.started += ResetCamera;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 r = player.eulerAngles;
        player.eulerAngles = new Vector3(r.x, r.y, 0f);
    }

    void ResetCamera(InputAction.CallbackContext context)
    {
        player.eulerAngles = Vector3.zero;
    }

    void RotateCamera(InputAction.CallbackContext context)
    {
        Vector2 vector = context.ReadValue<Vector2>();

        if (forceUniaxialTurning)
        {
            // change yaw
            if (Mathf.Abs(vector.x) >= Mathf.Abs(vector.y))
            {
                player.eulerAngles += new Vector3(0f, vector.x * turnSpeed);
            }
            // change pitch
            else
            {
                player.eulerAngles += new Vector3(-vector.y * turnSpeed, 0f);
            }
        }
        else
        {
            player.eulerAngles += new Vector3(0f, vector.x * turnSpeed);
            player.eulerAngles += new Vector3(-vector.y * turnSpeed, 0f);
        }
    }
}
