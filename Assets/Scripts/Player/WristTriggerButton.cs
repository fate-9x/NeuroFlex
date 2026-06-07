using UnityEngine;

// Sphere trigger on the wrist that fires ConfirmInput when a specific hand touches it.
// requiredHandTag must match the tag set by ActiveHandTagger ("LeftHand" or "RightHand").
public class WristTriggerButton : MonoBehaviour
{
    [SerializeField] private bool isSkip = false;
    [SerializeField] private string requiredHandTag = "LeftHand";

    private bool _triggered = false;
    private int _activeCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.IsChildOf(transform.parent)) return;
        if (!HasAncestorWithTag(other.transform, requiredHandTag)) return;

        _activeCount++;
        if (_triggered) return;

        _triggered = true;
        if (isSkip) ConfirmInput.RaiseSkipSubtitle();
        else        ConfirmInput.RaiseConfirm();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.IsChildOf(transform.parent)) return;
        if (!HasAncestorWithTag(other.transform, requiredHandTag)) return;

        _activeCount--;
        if (_activeCount <= 0)
        {
            _activeCount = 0;
            _triggered = false;
        }
    }

    private static bool HasAncestorWithTag(Transform t, string tag)
    {
        while (t != null)
        {
            if (t.CompareTag(tag)) return true;
            t = t.parent;
        }
        return false;
    }
}
