using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class InputHandler : MonoBehaviour
{
    public UnityEngine.XR.InputDevice leftController;
    public UnityEngine.XR.InputDevice rightController;

    public enum ButtonState
    {
        Started,
        Held,
        Released
    }

    public enum ControllerButton
    {
        LeftClick,
        RightClick,
        LeftTrigger,
        RightTrigger,
        LeftGrip,
        RightGrip,
        A,
        B,
        X,
        Y,
        Menu,
    }

    private Dictionary<ControllerButton, ButtonState> buttonStates = new();

    // Start is called before the first frame update
    void Start()
    {
        InitializeInputDevices();
        foreach (ControllerButton button in Enum.GetValues(typeof(ControllerButton)))
        {
            buttonStates.Add(button, ButtonState.Released);
        }
    }

    public void InitializeInputDevices()
    {
        if (!leftController.isValid)
        {
            InitializeInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, ref leftController);
        }
        if (!rightController.isValid)
        {
            InitializeInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, ref rightController);
        }
    }

    private void InitializeInputDevice(InputDeviceCharacteristics inputCharacteristics, ref UnityEngine.XR.InputDevice inputDevice)
    {
        List<UnityEngine.XR.InputDevice> devices = new();
        //Call InputDevices to see if it can find any devices with the characteristics we're looking for
        InputDevices.GetDevicesWithCharacteristics(inputCharacteristics, devices);

        //Our hands might not be active and so they will not be generated from the search.
        //We check if any devices are found here to avoid errors.
        if (devices.Count > 0)
        {
            inputDevice = devices[0];
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!leftController.isValid || !rightController.isValid)
        {
            InitializeInputDevices();
        }
    }

    public Vector2 GetLeftAxis()
    {
        Vector2 vec = Vector2.zero;
        leftController.TryReadAxis2DValue(InputHelpers.Axis2D.PrimaryAxis2D, out vec);
        Debug.Log(vec);
        return vec;
    }

    public Vector2 GetRightAxis()
    {
        Vector2 vec = Vector2.zero;
        rightController.TryReadAxis2DValue(InputHelpers.Axis2D.PrimaryAxis2D, out vec);
        return vec;
    }

    /// <summary>
    /// Returns whether the provided button is in a down state
    /// </summary>
    public bool IsPressed(ControllerButton button)
    {
        float val = 0f;
        switch(button)
        {
            case ControllerButton.LeftClick:
                leftController.TryReadSingleValue(InputHelpers.Button.Primary2DAxisClick, out val); break;
            case ControllerButton.LeftTrigger:
                leftController.TryReadSingleValue(InputHelpers.Button.TriggerButton, out val); break;
            case ControllerButton.LeftGrip:
                leftController.TryReadSingleValue(InputHelpers.Button.GripButton, out val); break;
            case ControllerButton.X:
                leftController.TryReadSingleValue(InputHelpers.Button.PrimaryButton, out val); break;
            case ControllerButton.Y:
                leftController.TryReadSingleValue(InputHelpers.Button.SecondaryButton, out val); break;
            case ControllerButton.Menu:
                leftController.TryReadSingleValue(InputHelpers.Button.MenuButton, out val); break;
            case ControllerButton.RightClick:
                rightController.TryReadSingleValue(InputHelpers.Button.Primary2DAxisClick, out val); break;
            case ControllerButton.RightTrigger:
                rightController.TryReadSingleValue(InputHelpers.Button.TriggerButton, out val); break;
            case ControllerButton.RightGrip:
                rightController.TryReadSingleValue(InputHelpers.Button.GripButton, out val); break;
            case ControllerButton.A:
                rightController.TryReadSingleValue(InputHelpers.Button.PrimaryButton, out val); break;
            case ControllerButton.B:
                rightController.TryReadSingleValue(InputHelpers.Button.SecondaryButton, out val); break;
            default:
                break;
        }
        return (val > 0f);
    }

    /// <summary>
    /// Returns the state of the provided button (Released, Started (pressed on this frame but not the last), or Held (pressed on this frame and the last))
    /// </summary>
    public ButtonState GetState(ControllerButton button)
    {
        if(IsPressed(button))
        {
            if (buttonStates[button] == ButtonState.Released)
            {
                buttonStates[button] = ButtonState.Started;
                return ButtonState.Started;
            }
            else
            {
                buttonStates[button] = ButtonState.Held;
                return ButtonState.Held;
            }
        }
        else
        {
            buttonStates[button] = ButtonState.Released;
            return ButtonState.Released;
        }
    }

    /// <summary>
    /// Returns whether the button has just been pressed (ie on this frame but not the last)
    /// </summary>
    public bool IsStarted(ControllerButton button)
    {
        return (GetState(button) == ButtonState.Started);
    }
}
