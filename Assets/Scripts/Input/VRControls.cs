using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class VRControls : MonoBehaviour
{
    [Header("Controls")]
    public InputActionReference rightThumbstick;
    public InputActionReference rightThumbstickButton;
    public InputActionReference menuButton;

    [Header("Objects")]
    public Transform player;
    public GameObject vrMenu;
    public Transform vrMenuOffsetPoint;

    [Header("Settings")]
    public float turnSpeed = 1.0f;
    public bool forceUniaxialTurning = false;

    // Start is called before the first frame update
    void Start()
    {
        rightThumbstick.action.performed += RotateCamera;
        rightThumbstickButton.action.started += ResetCamera;
        menuButton.action.started += ToggleMenu;
    }

    // Update is called once per frame
    void Update()
    {
        //Vector3 r = player.localEulerAngles;
        //player.localEulerAngles = new Vector3(r.x, r.y, 0f);
    }

    void ResetCamera(InputAction.CallbackContext context)
    {
        player.localEulerAngles = Vector3.zero;
    }

    void RotateCamera(InputAction.CallbackContext context)
    {
        Vector2 vector = context.ReadValue<Vector2>();

        if (forceUniaxialTurning)
        {
            // change yaw
            if (Mathf.Abs(vector.x) >= Mathf.Abs(vector.y))
            {
                player.localEulerAngles += new Vector3(0f, vector.x * turnSpeed);
            }
            // change pitch
            else
            {
                player.localEulerAngles += new Vector3(-vector.y * turnSpeed, 0f);
            }
        }
        else
        {
            player.localEulerAngles += new Vector3(0f, vector.x * turnSpeed);
            player.localEulerAngles += new Vector3(-vector.y * turnSpeed, 0f);
        }
    }

    private bool menuEnabled = true;

    void ToggleMenu(InputAction.CallbackContext context)
    {
        menuEnabled = !menuEnabled;
        vrMenu.SetActive(menuEnabled);
        vrMenuOffsetPoint.position = player.position;
        vrMenuOffsetPoint.rotation = player.rotation;
    }
}
