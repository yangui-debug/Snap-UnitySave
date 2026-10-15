using Exoa.Events;
using Lean.Common;

public class FocusOnClick : LeanSelectableBehaviour
{
    public bool follow;
    public bool focusOnFollow;

    protected override void OnSelected(LeanSelect @select)
    {
        base.OnSelected(@select);
        print("OnSelect " + gameObject.name);
        if (follow)
            CameraEvents.OnRequestObjectFollow?.Invoke(gameObject, focusOnFollow);
        else
            CameraEvents.OnRequestObjectFocus?.Invoke(gameObject, false);
    }

    protected override void OnDeselected(LeanSelect @select)
    {
        base.OnDeselected(@select);
        print("OnDeselect " + gameObject.name);
        if (follow)
            CameraEvents.OnRequestObjectFollow?.Invoke(null, focusOnFollow);
    }
}
