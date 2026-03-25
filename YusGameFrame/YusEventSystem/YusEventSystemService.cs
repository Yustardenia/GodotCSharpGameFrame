using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

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

    public void RegisterEvent(string eventKey)
    {
        if (string.IsNullOrWhiteSpace(eventKey))
        {
            GD.PushError("YusEventSystemService 收到了空的事件键。");
            return;
        }

        _registeredEvents.Add(eventKey);
        _listeners.TryAdd(eventKey, []);
    }

    public void AddListener(string eventKey, Node owner, Action listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    public void AddListener<T1>(string eventKey, Node owner, Action<T1> listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    public void AddListener<T1, T2>(string eventKey, Node owner, Action<T1, T2> listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    public void AddListener<T1, T2, T3>(string eventKey, Node owner, Action<T1, T2, T3> listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    public void AddListener(string eventKey, Node owner, Action<Variant[]> listener)
    {
        AddListenerInternal(eventKey, owner, listener);
    }

    public void RemoveListener(string eventKey, Action listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    public void RemoveListener<T1>(string eventKey, Action<T1> listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    public void RemoveListener<T1, T2>(string eventKey, Action<T1, T2> listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    public void RemoveListener<T1, T2, T3>(string eventKey, Action<T1, T2, T3> listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    public void RemoveListener(string eventKey, Action<Variant[]> listener)
    {
        RemoveListenerInternal(eventKey, listener);
    }

    public void Broadcast(string eventKey)
    {
        BroadcastInternal(eventKey, []);
    }

    public void Broadcast<T1>(string eventKey, T1 arg1)
    {
        BroadcastInternal(eventKey, [Variant.From(arg1)]);
    }

    public void Broadcast<T1, T2>(string eventKey, T1 arg1, T2 arg2)
    {
        BroadcastInternal(eventKey, [Variant.From(arg1), Variant.From(arg2)]);
    }

    public void Broadcast<T1, T2, T3>(string eventKey, T1 arg1, T2 arg2, T3 arg3)
    {
        BroadcastInternal(eventKey, [Variant.From(arg1), Variant.From(arg2), Variant.From(arg3)]);
    }

    private void BroadcastInternal(string eventKey, Variant[] arguments)
    {
        if (!TryGetInvocationSnapshot(eventKey, out var listeners))
        {
            return;
        }

        foreach (var listener in listeners)
        {
            try
            {
                InvokeListener(listener.Callback, arguments);
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

    private static void InvokeListener(Delegate callback, Variant[] arguments)
    {
        switch (callback)
        {
            case Action listener when arguments.Length == 0:
                listener.Invoke();
                return;
            case Action<Variant[]> rawListener:
                rawListener.Invoke(arguments);
                return;
        }

        callback.DynamicInvoke(ConvertArguments(arguments, callback.Method.GetParameters()));
    }

    private static object?[] ConvertArguments(IReadOnlyList<Variant> arguments, ParameterInfo[] parameters)
    {
        var converted = new object?[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            if (index >= arguments.Count)
            {
                converted[index] = null;
                continue;
            }

            converted[index] = ConvertVariant(arguments[index], parameters[index].ParameterType);
        }

        return converted;
    }

    private static object? ConvertVariant(Variant variant, Type targetType)
    {
        if (targetType == typeof(Variant))
        {
            return variant;
        }

        if (targetType == typeof(object))
        {
            return variant;
        }

        var asMethod = typeof(Variant).GetMethod(nameof(Variant.As), BindingFlags.Public | BindingFlags.Instance);
        if (asMethod == null)
        {
            return variant;
        }

        var genericMethod = asMethod.MakeGenericMethod(targetType);
        return genericMethod.Invoke(variant, null);
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
