using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KartPlayerInputSource : MonoBehaviour, IKartInputSource
{
    private KartInputFrame currentInput;

    public KartInputFrame CurrentInput => currentInput;

    private void Update()
    {
        float throttle = 0f;
        float steer = 0f;
        bool brake = false;
        bool drift = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) throttle += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) throttle -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steer += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steer -= 1f;
            brake = keyboard.spaceKey.isPressed;
            drift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            throttle += stick.y;
            steer += stick.x;
            brake |= gamepad.buttonSouth.isPressed;
            drift |= gamepad.rightShoulder.isPressed || gamepad.leftShoulder.isPressed;
        }

        currentInput = new KartInputFrame(throttle, steer, brake, drift).Sanitized();
    }
}
