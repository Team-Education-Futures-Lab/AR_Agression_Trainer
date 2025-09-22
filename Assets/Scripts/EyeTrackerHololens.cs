using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Input;
using TMPro;

public class EyeTrackerHololens : MonoBehaviour
{
    // InputAction references for OpenXR eye tracking
    private InputAction eyeGazePositionAction;
    private InputAction eyeGazeRotationAction;
    public TextMeshProUGUI emotionTMP;

    void OnEnable()
    {
        // Create InputActions bound to the OpenXR "eye gaze interaction" profile
        eyeGazePositionAction = new InputAction(
            name: "Eye Gaze Position",
            type: InputActionType.Value,
            binding: "<XRHMD>/centerEyePosition");

        eyeGazeRotationAction = new InputAction(
            name: "Eye Gaze Rotation",
            type: InputActionType.Value,
            binding: "<XRHMD>/centerEyeRotation");

        eyeGazePositionAction.Enable();
        eyeGazeRotationAction.Enable();
    }

    void Update()
    {
        if (eyeGazePositionAction != null && eyeGazeRotationAction != null)
        {
            // Get gaze origin
            Vector3 origin = eyeGazePositionAction.ReadValue<Vector3>();

            // Get gaze direction from rotation
            Quaternion rotation = eyeGazeRotationAction.ReadValue<Quaternion>();
            Vector3 direction = rotation * Vector3.forward;

            // Debug visualization
            Debug.DrawRay(origin, direction * 10, Color.green);
            Debug.Log($"👁 Origin: {origin}, Direction: {direction}");
            emotionTMP.text = $"👁 Origin: {origin}, Direction: {direction}";
        }
    }

    void OnDisable()
    {
        eyeGazePositionAction?.Disable();
        eyeGazeRotationAction?.Disable();
    }
}
