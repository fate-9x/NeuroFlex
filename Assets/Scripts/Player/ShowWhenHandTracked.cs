using UnityEngine;

// Shows target GameObject only when hand tracking is active (no controllers connected).
// Place on an always-active parent. Assign Target = the child to show/hide.
// If Target is left empty, operates on this GameObject itself — but then it cannot
// re-enable itself after being deactivated; use the parent pattern instead.
public class ShowWhenHandTracked : MonoBehaviour
{
    [SerializeField] private GameObject target;

    void Update()
    {
        bool hasControllers =
            OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) ||
            OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);

        var obj = target != null ? target : gameObject;
        obj.SetActive(!hasControllers);
    }
}
