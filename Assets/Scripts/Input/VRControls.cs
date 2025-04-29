using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class VRControls : MonoBehaviour
{

    private InputHandler inputHandler;


    [Header("User Objects")]
    public Transform xrOrigin;
    public Transform vrCamera;


    [Header("Objects")]
    public GameObject vrMenu;
    public Transform vrMenuOffsetPoint;

    [Header("Settings")]
    public float moveSpeed = 2.0f;
    public float turnSpeed = 1.0f;
    public bool forceUniaxialTurning = false;
    public bool allowStrafe = true;
    public float zoomDistance = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        inputHandler = GetComponent<InputHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        MovePlayer();
        RotateCamera();
        UpdateMenu();
        UpdateZoom();

        //DebugFunc();
    }

    // move player relative to camera view: forward/strafe directions relative to CAMERA, not xr rig
    void MovePlayer()
    {
        inputHandler.leftController.TryReadAxis2DValue(InputHelpers.Axis2D.PrimaryAxis2D, out Vector2 vector);

        xrOrigin.position += moveSpeed * vector.y * 0.05f * vrCamera.forward;

        if (allowStrafe)
        {
            xrOrigin.position += moveSpeed * vector.x * 0.05f * vrCamera.right;
        }
    }


    // keep track of player pitch for pitch clamping
    private float pitch = 0.0f;
    public float maxPitch = 20.0f;

    // rotate xr rig globally (not with respect to the camera to prevent rolling)
    void RotateCamera()
    {
        // potentially reset camera
        if (inputHandler.IsStarted(InputHandler.ControllerButton.RightClick))
        {
            xrOrigin.transform.rotation = Quaternion.identity;
            pitch = 0.0f;
        }

        Vector2 vector = inputHandler.GetRightAxis();
        
        if (forceUniaxialTurning)
        {
            // change yaw globally
            if (Mathf.Abs(vector.x) >= Mathf.Abs(vector.y))
            {
                float deltaYaw = vector.x * turnSpeed;
                xrOrigin.transform.Rotate(0f, deltaYaw, 0f, Space.World);
            }
            // change pitch locally
            else
            {
                float deltaPitch = -vector.y * turnSpeed;
                deltaPitch = Mathf.Clamp(deltaPitch, -pitch - maxPitch, -pitch + maxPitch);
                pitch += deltaPitch;
                xrOrigin.transform.Rotate(deltaPitch, 0f, 0f, Space.Self);
            }
        }
        else
        {
            float deltaYaw = vector.x * turnSpeed;
            float deltaPitch = -vector.y * turnSpeed;
            deltaPitch = Mathf.Clamp(deltaPitch, -pitch - 45f, -pitch + 45f);
            pitch += deltaPitch;

            // change yaw globally
            xrOrigin.transform.Rotate(0f, deltaYaw, 0f, Space.World);

            // change pitch locally
            xrOrigin.transform.Rotate(deltaPitch, 0f, 0f, Space.Self);

        }
    }

    // keep track of whether the menu is showing
    private bool menuShowing = true;
    
    // move menu to player position and toggle on menu button press
    void UpdateMenu()
    {
        // move menu to player position
        vrMenuOffsetPoint.SetPositionAndRotation(xrOrigin.position, xrOrigin.rotation);

        if (inputHandler.IsStarted(InputHandler.ControllerButton.Menu))
        {
            menuShowing = !menuShowing;

            vrMenu.GetComponent<CanvasGroup>().alpha = menuShowing ? 1f : 0f;
            vrMenu.GetComponent<CanvasGroup>().interactable = menuShowing;
            vrMenu.GetComponent<CanvasGroup>().blocksRaycasts = menuShowing;
        }
    }

    private Vector3 originalCameraPosition;
    void UpdateZoom()
    {
        if (inputHandler.IsPressed(InputHandler.ControllerButton.LeftTrigger))
        {
            Vector3 forward = vrCamera.forward;
            Vector3 zoomPosition = originalCameraPosition + forward * zoomDistance;
            vrCamera.position = zoomPosition;
        }
        else
        {
            vrCamera.position = originalCameraPosition;
        }
    }


    void DebugFunc()
    {
        inputHandler.rightController.TryReadSingleValue(InputHelpers.Button.Primary2DAxisClick, out float val);
        if (val > 0f)
        {
            Debug.Log(val);
        }
    }



    public void ChangeMovementSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void ChangeTurnSpeed(float speed)
    {
        turnSpeed = speed;
    }

    public void ToggleUniaxialTurning(bool turning)
    {
        forceUniaxialTurning = !forceUniaxialTurning;
    }

    public void ToggleStrafe(bool strafe)
    {
        allowStrafe = !allowStrafe;
    }
}
