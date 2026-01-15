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

    [SerializeField]
    private TextMeshProUGUI emotionTMP;

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
            Vector3 gazeOrigin = eyeGazePositionAction.ReadValue<Vector3>();

            // Get gaze direction from rotation
            Quaternion gazeRotation = eyeGazeRotationAction.ReadValue<Quaternion>();
            Vector3 gazeDirection = gazeRotation * Vector3.forward;

            // Debug visualization
            Debug.DrawRay(gazeOrigin, gazeDirection * 10, Color.green);
            Debug.Log($"Gaze origin: {gazeOrigin}, direction: {gazeDirection}");
            emotionTMP.text = $"Origin: {gazeOrigin}, Direction: {gazeDirection}";
        }
    }

    void OnDisable()
    {
        eyeGazePositionAction?.Disable();
        eyeGazeRotationAction?.Disable();
    }
}
