using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

public class OVRControls : MonoBehaviour
{

    [Header("User Objects")]
    public Transform vrUser;
    public Transform vrCamera;

    [Header("Objects")]
    public GameObject vrMenu;
    public Transform vrMenuPosition;

    [Header("Settings")]
    public float moveSpeed;
    public float turnSpeed;
    public bool forceUniaxialTurning;
    public bool disablePitch;
    public bool allowStrafe;
    public float zoomDistance;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isZooming)
        {
            MoveUser();
        }

        RotateCamera();
        UpdateZoom();

        UpdateMenu();
    }

    // ============================== MOVEMENT ====================================================


    // Move user relative to the plane spanned by the camera view
    void MoveUser()
    {

        // potentially reset camera and position
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
        {
            vrUser.position = Vector3.zero;
            vrUser.rotation = Quaternion.identity;
            pitch = 0.0f;
        }

        Vector2 vector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        Vector3 move = moveSpeed * vector.y * 0.05f * vrCamera.forward;

        if (allowStrafe)
        {
            move += moveSpeed * vector.x * 0.05f * vrCamera.right;
        }

        move += UpdateElevation() * 0.05f;

        vrUser.position += move;

    }


    // Elevate user relative to the global view
    Vector3 UpdateElevation()
    {
        if (OVRInput.Get(OVRInput.Button.One))
        {
            return -moveSpeed * Vector3.up;
        }
        else if (OVRInput.Get(OVRInput.Button.Two))
        {
            return moveSpeed * Vector3.up;
        }
        return Vector3.zero;
    }




    // keep track of player pitch for pitch clamping
    private float pitch = 0.0f;
    public float maxPitch = 20.0f;

    // rotate xr rig globally (not with respect to the camera to prevent rolling)
    void RotateCamera()
    {

        Vector2 vector = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (forceUniaxialTurning)
        {
            // change yaw globally
            if (Mathf.Abs(vector.x) >= Mathf.Abs(vector.y))
            {
                float deltaYaw = vector.x * turnSpeed;
                vrUser.Rotate(0f, deltaYaw, 0f, Space.World);
            }
            // change pitch locally
            else if (!disablePitch)
            {
                float deltaPitch = -vector.y * turnSpeed;
                deltaPitch = Mathf.Clamp(deltaPitch, -pitch - maxPitch, -pitch + maxPitch);
                pitch += deltaPitch;
                vrUser.Rotate(deltaPitch, 0f, 0f, Space.Self);
            }
        }
        else
        {
            float deltaYaw = vector.x * turnSpeed;
            float deltaPitch = -vector.y * turnSpeed;
            deltaPitch = Mathf.Clamp(deltaPitch, -pitch - 45f, -pitch + 45f);
            pitch += deltaPitch;

            // change yaw globally
            vrUser.Rotate(0f, deltaYaw, 0f, Space.World);

            if (!disablePitch)
            {
                // change pitch locally
                vrUser.Rotate(deltaPitch, 0f, 0f, Space.Self);
            }

        }
    }



    private Vector3 originalCameraPosition = Vector3.zero;
    private Vector3 zoomDirection = Vector3.zero;
    private bool isZooming = false;
    private float zoomCoef = 0f;
    private float zoomSpeed = 0.06f;
    // project camera forwards (a crude zoom effect)
    void UpdateZoom()
    {
        if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.5f)
        {
            if (!isZooming)
            {
                zoomDirection = vrCamera.forward;
                originalCameraPosition = vrUser.position;
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
                vrUser.position = originalCameraPosition;
                isZooming = false;
            }
            if (isZooming)
            {
                UpdateCameraZoom(false);
            }
        }


    }

    // calculate new zoom target
    private void UpdateCameraZoom(bool zoomingForward)
    {
        // update position
        Vector3 forward = zoomDirection;
        Vector3 zoomTargetPosition = originalCameraPosition + forward * zoomDistance;

        if (zoomingForward)
        {
            Vector3 zoomPosition = Vector3.Slerp(originalCameraPosition, zoomTargetPosition, EaseOutExpo(zoomCoef));
            vrUser.position = zoomPosition;
        }
        else
        {
            Vector3 zoomPosition = Vector3.Slerp(zoomTargetPosition, originalCameraPosition, EaseOutExpo(1 - zoomCoef));
            vrUser.position = zoomPosition;
        }
    }

    // helper function to smooth zooming
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


    // ====================================== UI INTERACTIONS =======================================================


    // keep track of whether the menu is showing
    private bool menuShowing = true;

    // move menu to player position and toggle on menu button press
    void UpdateMenu()
    {
        // move menu to player position
        vrMenuPosition.SetPositionAndRotation(vrUser.position, vrUser.rotation);

        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            menuShowing = !menuShowing;

            vrMenu.SetActive(menuShowing);
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
}