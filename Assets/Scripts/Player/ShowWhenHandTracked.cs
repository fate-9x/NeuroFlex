using UnityEngine;

// Hides this GameObject when controllers are connected (hand tracking not active).
// No inspector references needed — reads OVRInput controller state at runtime.
public class ShowWhenHandTracked : MonoBehaviour
{
    void Update()
    {
        bool hasControllers =
            OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) ||
            OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
        gameObject.SetActive(!hasControllers);
    }
}
