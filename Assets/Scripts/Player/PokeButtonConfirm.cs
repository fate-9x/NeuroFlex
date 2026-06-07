using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;

[RequireComponent(typeof(PokeInteractable))]
[RequireComponent(typeof(CircleSurface))]
[RequireComponent(typeof(PlaneSurface))]
public class PokeButtonConfirm : MonoBehaviour
{
    [SerializeField] private bool isSkip = false;

    private PokeInteractable _interactable;
    private InteractableState _prevState;

    private void Awake()
    {
        var plane = GetComponent<PlaneSurface>();
        var circle = GetComponent<CircleSurface>();
        _interactable = GetComponent<PokeInteractable>();

        circle.InjectPlaneSurface(plane);
        _interactable.InjectSurfacePatch(circle);

        _prevState = _interactable.State;
    }

    private void OnEnable()
    {
        if (_interactable != null)
            _prevState = _interactable.State;
    }

    private void Update()
    {
        var state = _interactable.State;
        if (state == InteractableState.Select && _prevState != InteractableState.Select)
        {
            if (isSkip) ConfirmInput.RaiseSkipSubtitle();
            else ConfirmInput.RaiseConfirm();
        }
        _prevState = state;
    }
}
