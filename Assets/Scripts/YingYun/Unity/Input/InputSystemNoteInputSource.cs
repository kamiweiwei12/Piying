using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Timing;

namespace YingYun.Rhythm.Input
{
    /// <summary>把 Input System action 事件轉為保留原始事件時間戳的 HitInput 佇列。</summary>
    public sealed class InputSystemNoteInputSource : IDisposable
    {
        private readonly InputActionMap _map;
        private readonly ClockBridge _bridge;
        private readonly DspSongClock _clock;
        private readonly Queue<HitInput> _queue = new Queue<HitInput>();
        private double _inputOffsetSeconds;

        public double InputOffsetSeconds
        {
            get => _inputOffsetSeconds;
            set => _inputOffsetSeconds = value;
        }

        public InputSystemNoteInputSource(
            InputActionAsset actions,
            ClockBridge bridge,
            DspSongClock clock,
            double inputOffsetSeconds)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _inputOffsetSeconds = inputOffsetSeconds;
            _map = actions.FindActionMap("Rhythm", true);
            _map.actionTriggered += OnActionTriggered;
            _map.Enable();
        }

        public bool TryDequeue(out HitInput input)
        {
            if (_queue.Count == 0)
            {
                input = default;
                return false;
            }

            input = _queue.Dequeue();
            return true;
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public void Dispose()
        {
            _map.actionTriggered -= OnActionTriggered;
            _map.Disable();
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (!_bridge.HasSample)
            {
                return;
            }

            InputKind kind;
            if (context.phase == InputActionPhase.Performed)
            {
                kind = InputKind.Press;
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                kind = InputKind.Release;
            }
            else
            {
                return;
            }

            int lane = ActionNameToLane(context.action.name);
            if (lane < 0)
            {
                return;
            }

            double songTime = _bridge.InputTimeToSong(context.time, _clock, _inputOffsetSeconds);
            _queue.Enqueue(new HitInput(songTime, lane, kind, context.control.device.deviceId));
        }

        private static int ActionNameToLane(string actionName)
        {
            switch (actionName)
            {
                case "Lane1": return 0;
                case "Lane2": return 1;
                case "Lane3": return 2;
                case "Lane4": return 3;
                case "Lane5": return 4;
                case "Lane6": return 5;
                default: return -1;
            }
        }
    }
}
