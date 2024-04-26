using Godot;
using System.Runtime.CompilerServices;

namespace SadChromaLib.Input;

/// <summary>
/// A resource that can be used to track changes to gamepad states.
/// </summary>
[GlobalClass]
#if TOOLS
[Tool]
#endif
public sealed partial class GamepadStateObserver: Resource
{
    public delegate void ControllerStateChangedCallback(bool hasController);
    public event ControllerStateChangedCallback OnControllerStateChanged;

    bool _isObserving;

    #region Main Functions

    /// <summary>
    /// (Can be called multiple times; it will only bind to the signal whenever it's needed)
    /// Binds to the Input::JoyConnectionChanged signal, allowing its subscribers to listen to gamepad connection states.
    /// </summary>
    public void StartObserving()
    {
        if (_isObserving)
            return;

        _isObserving = true;

        Godot.Input.JoyConnectionChanged += OnJoyConnectionChangedInternal;
        OnJoyConnectionChangedInternal(default, default);
    }

    /// <summary>
    /// Stops listening to gamepad state changes
    /// </summary>
    public void StopObserving()
    {
        _isObserving = false;
        Godot.Input.JoyConnectionChanged -= OnJoyConnectionChangedInternal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind(ControllerStateChangedCallback callback) {
        OnControllerStateChanged += callback;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unbind(ControllerStateChangedCallback callback) {
        OnControllerStateChanged -= callback;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindAll() {
        OnControllerStateChanged = null;
    }

    #endregion

    #region Event Handlers

    public override void _Notification(int what)
    {
        if (what != GodotObject.NotificationPredelete)
            return;

        OnControllerStateChanged = null;
        StopObserving();
    }

    private void OnJoyConnectionChangedInternal(long idx, bool connected) {
        OnControllerStateChanged?.Invoke(Godot.Input.GetConnectedJoypads().Count > 0);
    }

    #endregion
}