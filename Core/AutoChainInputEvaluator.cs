using Godot;

namespace SadChromaLib.Input;

/// <summary>
/// A helper struct for automatically evaluating a specified set of input actions
/// </summary>
public readonly struct AutoChainInputEvaluator
{
    readonly StringName[] _autoEvaluateActions;

    public AutoChainInputEvaluator(params string[] actionIds)
    {
        _autoEvaluateActions = new StringName[actionIds.Length];

        for (int i = 0; i < actionIds.Length; ++i) {
            _autoEvaluateActions[i] = actionIds[i];
        }
    }

    public readonly void Evaluate(InputEvent @event, InputChainController controller)
    {
        for (int i = 0; i < _autoEvaluateActions.Length; ++i) {
            if (@event.IsActionPressed(_autoEvaluateActions[i])) {
                controller.MarkHoldStart();
            }
            else if (@event.IsActionReleased(_autoEvaluateActions[i])) {
                controller.Evaluate(_autoEvaluateActions[i]);
            }
        }
    }
}