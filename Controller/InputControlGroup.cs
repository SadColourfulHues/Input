using Godot;

namespace SadChromaLib.Input;

/// <summary>
/// A UI control that can show variations depending on the current input scheme
/// </summary>
[GlobalClass]
[Tool]
public sealed partial class InputControlGroup: Control
{
    [Export]
    GamepadStateObserver _gamepadState;

    #region Events

    public override void _Ready()
    {
        #if TOOLS
        if (Engine.IsEditorHint())
        {
            AddIfMissing("MouseAndKeyboard");
            AddIfMissing("Controller", false);

            return;
        }
        #endif

        _gamepadState.StartObserving();
        _gamepadState.Bind(OnGamepadStateChanged);
    }

    public override void _ExitTree()
    {
        #if TOOLS
        if (Engine.IsEditorHint())
            return;
        #endif

        _gamepadState.Unbind(OnGamepadStateChanged);
    }

    private void OnGamepadStateChanged(bool connected)
    {
        GetNode<Control>("MouseAndKeyboard").Visible = !connected;
        GetNode<Control>("Controller").Visible = connected;
    }

    #endregion

    #region Tool Utils
    #if TOOLS
    private void AddIfMissing(string name, bool visible = true)
    {
        if (HasNode(name))
            return;

        Control group = new() { Name = name, Visible = visible };
        AddChild(group);

        group.Owner = GetTree().EditedSceneRoot;
    }
    #endif
    #endregion
}