using Godot;
using System;
using System.Collections.Generic;

namespace YusGameFrame.YusEventSystem;

public partial class YusEventSystemService : Node
{
    public static YusEventSystemService Instance { get; private set; } = null!;

    private readonly Dictionary<string, List<ListenerEntry>> _listeners = new(StringComparer.Ordinal);
    private readonly HashSet<string> _registeredEvents = new(StringComparer.Ordinal);

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GD.PushError("YusEventSystemService 已存在一个有效实例。");
            QueueFree();
            return;
        }

        Instance = this;
    }

    public override void _Ready()
    {
        YusEventSignals.RegisterAll(this);
    }

    public override void _ExitTree()
    {
        if (Instance != this)
        {
            return;
        }

        _listeners.Clear();
        _registeredEvents.Clear();
        Instance = null!;
    }

    public static YusEventSystemService RequireInstance()
    {
        if (Instance == null)
        {
            throw new InvalidOperationException("YusEventSystemService 当前不可用，请确认已正确配置 Autoload。");
        }

        return Instance;
    }

    internal void RegisterEvent(string eventKey)
    {
        if (string.IsNullOrWhiteSpace(eventKey))
        {
            GD.PushError("YusEventSystemService 收到了空的事件键。");
            return;
        }

        _registeredEvents.Add(eventKey);
        _listeners.TryAdd(eventKey, []);
    }

    internal void AddListener(string eventKey, Node owner, Action listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    internal void AddListener<T1>(string eventKey, Node owner, Action<T1> listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    internal void AddListener<T1, T2>(string eventKey, Node owner, Action<T1, T2> listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    internal void AddListener<T1, T2, T3>(string eventKey, Node owner, Action<T1, T2, T3> listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    internal void RemoveListener(string eventKey, Action listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    internal void RemoveListener<T1>(string eventKey, Action<T1> listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    internal void RemoveListener<T1, T2>(string eventKey, Action<T1, T2> listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    internal void RemoveListener<T1, T2, T3>(string eventKey, Action<T1, T2, T3> listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    internal void Broadcast(string eventKey)
    {
        if (!TryGetInvocationSnapshot(eventKey, out var listeners))
        {
            return;
        }

        foreach (var listener in listeners)
        {
            try
            {
                ((Action)listener.Callback).Invoke();
            }
            catch (Exception exception)
            {
                GD.PushError($"YusEventSystem 事件 '{eventKey}' 的监听执行失败：{exception}");
            }
        }
    }

    internal void Broadcast<T1>(string eventKey, T1 arg1)
    {
        if (!TryGetInvocationSnapshot(eventKey, out var listeners))
        {
            return;
        }

        foreach (var listener in listeners)
        {
            try
            {
                ((Action<T1>)listener.Callback).Invoke(arg1);
            }
            catch (Exception exception)
            {
                GD.PushError($"YusEventSystem 事件 '{eventKey}' 的监听执行失败：{exception}");
            }
        }
    }

    internal void Broadcast<T1, T2>(string eventKey, T1 arg1, T2 arg2)
    {
        if (!TryGetInvocationSnapshot(eventKey, out var listeners))
        {
            return;
        }

        foreach (var listener in listeners)
        {
            try
            {
                ((Action<T1, T2>)listener.Callback).Invoke(arg1, arg2);
            }
            catch (Exception exception)
            {
                GD.PushError($"YusEventSystem 事件 '{eventKey}' 的监听执行失败：{exception}");
            }
        }
    }

    internal void Broadcast<T1, T2, T3>(string eventKey, T1 arg1, T2 arg2, T3 arg3)
    {
        if (!TryGetInvocationSnapshot(eventKey, out var listeners))
        {
            return;
        }

        foreach (var listener in listeners)
        {
            try
            {
                ((Action<T1, T2, T3>)listener.Callback).Invoke(arg1, arg2, arg3);
            }
            catch (Exception exception)
            {
                GD.PushError($"YusEventSystem 事件 '{eventKey}' 的监听执行失败：{exception}");
            }
        }
    }

    private void AddListenerInternal(string eventKey, Node owner, Delegate listener)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(listener);

        if (!EnsureEventRegistered(eventKey))
        {
            return;
        }

        _listeners[eventKey].Add(new ListenerEntry(owner, listener));
    }

    private void RemoveListenerInternal(string eventKey, Delegate listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        if (!_listeners.TryGetValue(eventKey, out var listeners))
        {
            return;
        }

        listeners.RemoveAll(entry => entry.Callback == listener);
    }

    private bool TryGetInvocationSnapshot(string eventKey, out ListenerEntry[] listeners)
    {
        listeners = Array.Empty<ListenerEntry>();

        if (!EnsureEventRegistered(eventKey))
        {
            return false;
        }

        if (!_listeners.TryGetValue(eventKey, out var eventListeners))
        {
            return false;
        }

        eventListeners.RemoveAll(static entry => !entry.IsAlive);
        if (eventListeners.Count == 0)
        {
            return false;
        }

        listeners = eventListeners.ToArray();
        return true;
    }

    private bool EnsureEventRegistered(string eventKey)
    {
        if (_registeredEvents.Contains(eventKey))
        {
            return true;
        }

        GD.PushError($"YusEventSystem 事件 '{eventKey}' 尚未注册，如有需要请重新生成 YusEventSignals.g.cs。");
        return false;
    }

    private sealed class ListenerEntry
    {
        public ListenerEntry(Node owner, Delegate callback)
        {
            Owner = owner;
            Callback = callback;
        }

        public Node Owner { get; }

        public Delegate Callback { get; }

        public bool IsAlive => GodotObject.IsInstanceValid(Owner) && !Owner.IsQueuedForDeletion();
    }
}
