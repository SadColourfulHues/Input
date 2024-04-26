using SadChromaLib.Types;
using SadChromaLib.Utils.Timing;

using System;
using System.Runtime.CompilerServices;

namespace SadChromaLib.Input;

/// <summary>
/// [WIP] A class that can call multiple actions depending on the chain of input patterns it receives
/// </summary>
public sealed class InputChainController
{
    public delegate void CallbackMethod(float holdDuration);
    readonly public int InputBufferSize;

    readonly VArray<ActionItem> _actions;
    readonly InputHistoryItem[] _inputHistory;

    TimeSinceObserver _inputTimer;
    TimeSinceObserver _holdTimer;

    readonly float _bufferResetThresh;
    int _inputIdx;

    /// <summary>
    /// Creates a new input chain controller
    /// </summary>
    /// <param name="maxActions">The maximum number of registrable actions</param>
    /// <param name="maxInputBufferSize">For the best results, set this to the length of your longest input sequence.</param>
    /// <param name="bufferResetThreshold">The amount of time elapsed (in seconds) before the buffer is reset.</param>
    public InputChainController(int maxActions = 24, int maxInputBufferSize = 4, float bufferResetThreshold = 0.75f)
    {
        _inputIdx = 0;

        _actions = new(maxActions);
        _inputHistory = new InputHistoryItem[maxInputBufferSize];

        _bufferResetThresh = bufferResetThreshold;
        _inputTimer = new();
    }

    #region Main Functions

    public void MarkHoldStart() {
        _holdTimer.Reset();
    }

    /// <summary>
    /// Fully resets the state of the input chain controller. Actions must be re-added for it to work again.
    /// </summary>
    public void Clear()
    {
        _actions.Clear();
        ClearBuffer();
    }

    // <summary>
    /// Registers an action to the controller
    /// </summary>
    /// <param name="callback">The action callback</param>
    /// <param name="sequence">
    /// The sequence of actions to check for when evaluating this action
    /// Format: (string actionId, float maxDelay)
    /// </param>
    public void AddAction(CallbackMethod callback, params ActionSequenceItem[] sequence) {
        _actions.Add(new(sequence, callback));
    }

    /// <summary>
    /// Evaluates the next input
    /// </summary>
    /// <param name="actionId"></param>
    public void Evaluate(string actionId)
    {
        float timeSinceLastInput = (float) _inputTimer.TimeSinceSecs();
        float holdDuration = (float) _holdTimer.TimeSinceSecs();

        // Auto clear buffer on input timeout
        if (timeSinceLastInput >= _bufferResetThresh) {
            ClearBuffer();
        }

        EnqueueInput(new(actionId, timeSinceLastInput, holdDuration));
        _inputTimer.Reset();

        bool isFailedMatch;
        int bufferIdx;

        // Find the first matching action
        for (int i = 0; i < _actions.Count; ++i) {
            isFailedMatch = false;
            bufferIdx = 0;

            for (int j = 0; j < _actions[i].Value.Sequence.Length; ++j) {
                if (bufferIdx >= _inputHistory.Length)
                    break;

                if (!Match(_actions[i].Value.Sequence[j], _inputHistory[j], j == 0)) {
                    isFailedMatch = true;
                    break;
                }

                bufferIdx ++;
            }

            if (isFailedMatch)
                continue;

            _actions[i].Value.Callback.Invoke(holdDuration);
            ClearBuffer();

            return;
        }

        // Trim buffer to the start of the first valid index
        int bestStartIdx = _inputHistory.Length;

        for (int i = 0; i < _actions.Count; ++i) {
            for (int j = 0; j < _inputHistory.Length; ++j) {
                if (!Match(_actions[i].Value.Sequence[0], _inputHistory[j], true))
                    continue;

                bestStartIdx = Math.Min(bestStartIdx, j);
                break;
            }
        }
        if (bestStartIdx == 0 || bestStartIdx == _inputHistory.Length)
            return;

        int count = _inputIdx - bestStartIdx;

        Array.Copy(_inputHistory, bestStartIdx, _inputHistory, 0, count);
        _inputIdx = count;
    }

    #endregion

    #region Utils

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Match(ActionSequenceItem a, InputHistoryItem b, bool isFirst)
    {
        return a.ActionId == b.ActionId &&
                (isFirst || (!isFirst && b.Delay < a.MaxDelay)) &&
                b.HoldDuration >= a.MinHoldDuration;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnqueueInput(InputHistoryItem input)
    {
        if (_inputIdx >= _inputHistory.Length) {
            ClearBuffer();
        }

        _inputHistory[_inputIdx] = input;
        _inputIdx++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearBuffer()
    {
        Array.Clear(_inputHistory);
        _inputIdx = 0;
    }

    #endregion

    #region Data Types

    public readonly struct ActionSequenceItem
    {
        public readonly string ActionId;
        public readonly float MaxDelay;
        public readonly float MinHoldDuration;

        public ActionSequenceItem(string id, float delay = 0.0125f, float minHold = 0.0f)
        {
            ActionId = id;
            MaxDelay = delay + minHold;
            MinHoldDuration = minHold;
        }
    }

    /// <summary>
    /// A struct representing an action
    /// </summary>
    readonly struct ActionItem
    {
        public readonly ActionSequenceItem[] Sequence;
        public readonly CallbackMethod Callback;

        public ActionItem(ActionSequenceItem[] sequence, CallbackMethod callback)
        {
            Sequence = sequence;
            Callback = callback;
        }
    }

    /// <summary>
    /// A struct representing a partial entry to an input sequence
    /// </summary>
    readonly struct InputHistoryItem
    {
        public readonly string ActionId;
        public readonly float Delay;
        public readonly float HoldDuration;

        public bool IsHeld {
            get => HoldDuration > 0.0f;
        }

        public InputHistoryItem(string id, float delay, float holdDuration)
        {
            ActionId = id;
            Delay = delay;
            HoldDuration = holdDuration;
        }
    }

    #endregion
}