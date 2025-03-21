using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class CustomTurnScript : MonoBehaviour
{
    public InputActionReference rightJoystick;
    public InputActionReference rightJoystickButton;
    public Transform player;


    // Start is called before the first frame update
    void Start()
    {
        rightJoystick.action.performed += RotateCamera;
        rightJoystickButton.action.started += ResetCamera;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ResetCamera(InputAction.CallbackContext context)
    {
        player.localEulerAngles = Vector3.zero;
    }

    void RotateCamera(InputAction.CallbackContext context)
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
