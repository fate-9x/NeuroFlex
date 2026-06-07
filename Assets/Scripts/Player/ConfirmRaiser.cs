using UnityEngine;

public class ConfirmRaiser : MonoBehaviour
{
    [SerializeField] private bool isSkip = false;

    public void Raise()
    {
        if (isSkip) ConfirmInput.RaiseSkipSubtitle();
        else        ConfirmInput.RaiseConfirm();
    }
}
