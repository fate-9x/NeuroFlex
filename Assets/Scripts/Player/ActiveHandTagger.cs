using UnityEngine;

// Routes the "LeftHand"/"RightHand" tag to whichever visual is active
// so ScoreAdder.GetClosestHandPosition() always finds the correct position.
// The Meta XR SDK manages hand/controller visibility internally — this script
// only moves the tag; it does NOT call SetActive on any hand visual.
public class ActiveHandTagger : MonoBehaviour
{
    [SerializeField] private OVRHand hand;
    [SerializeField] private GameObject controllerVisual;
    [SerializeField] private GameObject handVisual;
    [SerializeField] private string side; // "LeftHand" or "RightHand"

    void Start()
    {
        if (hand != null) return;
        // Auto-find: prefer OVRHand on the Building Block (not legacy LeftOVRHand)
        foreach (Transform child in transform)
        {
            if (child.name.Contains("BuildingBlock") || child.name.Contains("Hand Tracking"))
            {
                var h = child.GetComponent<OVRHand>();
                if (h != null) { hand = h; break; }
            }
        }
        if (hand == null)
            hand = GetComponentInChildren<OVRHand>(true);
    }

    void LateUpdate()
    {
        bool handsOn = hand != null && hand.IsTracked;
        // SDK manages visual visibility — only route the tag here
        GameObject active = handsOn ? handVisual : controllerVisual;
        if (active != null && active.tag != side)
            active.tag = side;
    }
}
