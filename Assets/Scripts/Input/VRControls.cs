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
    public CharacterController sphereCollider;


    [Header("Objects")]
    public GameObject vrMenu;
    public Transform vrMenuOffsetPoint;

    [Header("Settings")]
    public float moveSpeed;
    public float turnSpeed;
    public bool forceUniaxialTurning;
    public bool allowStrafe;
    public float zoomDistance;

    // Start is called before the first frame update
    void Start()
    {
        inputHandler = GetComponent<InputHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isZooming)
        {
            MovePlayer();
        }   
        RotateCamera();
        UpdateZoom();
        UpdateMenu();

        //DebugFunc();
    }

    // move player relative to camera view: forward/strafe directions relative to CAMERA, not xr rig
    void MovePlayer()
    {
        inputHandler.leftController.TryReadAxis2DValue(InputHelpers.Axis2D.PrimaryAxis2D, out Vector2 vector);

        Vector3 move = moveSpeed * vector.y * 0.05f * vrCamera.forward;

        if (allowStrafe)
        {
            move += moveSpeed * vector.x * 0.05f * vrCamera.right;
        }

        sphereCollider.Move(move);
        xrOrigin.position = sphereCollider.transform.position;
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

    private Vector3 originalCameraPosition = Vector3.zero;
    private bool isZooming = false;
    private float zoomCoef = 0f;
    private float zoomSpeed = 0.06f;
    void UpdateZoom()
    {
        if (inputHandler.IsPressed(InputHandler.ControllerButton.LeftTrigger))
        {
            if (!isZooming)
            {
                originalCameraPosition = xrOrigin.transform.position;
                isZooming = true;
            }
            zoomCoef = Mathf.Clamp01(zoomCoef + zoomSpeed);
            UpdateCameraZoom(true);

        }
        else
        {
            zoomCoef = Mathf.Clamp01(zoomCoef - zoomSpeed);
            if (zoomCoef <= 0f && isZooming)
            {
                xrOrigin.transform.position = originalCameraPosition;
                isZooming = false;
            }
            if (isZooming)
            {
                UpdateCameraZoom(false);
            }
        }


    }

    private void UpdateCameraZoom(bool zoomingForward)
    {
        // update position
        Vector3 forward = vrCamera.forward;
        Vector3 zoomTargetPosition = originalCameraPosition + forward * zoomDistance;

        if (zoomingForward)
        {
            Vector3 zoomPosition = Vector3.Slerp(originalCameraPosition, zoomTargetPosition, EaseOutExpo(zoomCoef));
            xrOrigin.transform.position = zoomPosition;
        }
        else
        {
            Vector3 zoomPosition = Vector3.Slerp(zoomTargetPosition, originalCameraPosition, EaseOutExpo(1 - zoomCoef));
            xrOrigin.transform.position = zoomPosition;
        }
    }

    private float EaseOutExpo(float f)
    {
        if (f >= 1f)
        {
            return 1;
        }
        else
        {
            return 1 - Mathf.Pow(2, -10 * f);
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

    public void ChangeZoomDistance(float distance)
    {
        zoomDistance = distance;
    }

    public void ChangeCollisionRadius(float radius)
    {
        this.GetComponent<CharacterController>().radius = radius;
    }
}
