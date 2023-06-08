
using System;
using System.Collections.Generic;
using UnityEngine;

public class EventDispatcher : IDisposable
{
    //public EventDispatcher(GameService _service) => gameService = _service;

    #region Variables

    //private GameService gameService;

    /// <summary>
    /// Controlls if the E.D. is busy performing an action like adding / removing listeners
    /// </summary>
    private bool _processing = false;

    private readonly Dictionary<Type, List<Delegate>> _listeners = new();

    /// <summary>
    /// If the Event Dispatcher is busy, the events are stored to be added once it finished
    /// </summary>
    readonly private List<Listener> _listenersToAdd = new List<Listener>();

    /// <summary>
    /// If the Event Dispatcher is busy, the events are stored to be deleted once the event is finished
    /// </summary>
    readonly private List<Listener> _listenersToRemove = new List<Listener>();

    #endregion

    #region EventService Methods

    public void AddListener<T>(Action<T> listener) where T : class
    {
        Listener eventListener = new Listener { EventType = typeof(T), EventDelegate = listener };
        if (_processing)
        {
            _listenersToAdd.Add(eventListener);
        }
        else
        {
            AddListenerInternal(eventListener);
        }
    }

    public void RemoveListener<T>(Action<T> listener) where T : class
    {
        var evListener = new Listener { EventType = typeof(T), EventDelegate = listener };
        if (_processing)
        {
            _listenersToRemove.Add(evListener);
        }
        else
        {
            RemoveListenerInternal(evListener);
        }
    }

    public void TriggerEvent<T>() where T : class, new()
    {
        TriggerEvent(new T());
    }

    public void Dispose()
    {
        _listeners.Clear();
        _listenersToAdd.Clear();
        _listenersToRemove.Clear();
    }

    public void TriggerEvent<T>(T e) where T : class
    {
#if DEBUG
        if (_processing)
        {
            Debug.LogWarning("Triggered an event while processing a previous event");
        }
#endif

        Debug.Assert(e != null, "Raised a null event");
        Type type = e.GetType();
        if (!_listeners.TryGetValue(type, out List<Delegate> listeners))
        {
#if DEBUG
            Debug.Log("Raised event with no listeners");
#endif
            return;
        }

        _processing = true;
        listeners.RemoveAll(e => e == null);
        foreach (Delegate listener in listeners)
        {
            if (listener is Action<T> castedDelegate)
            {
                castedDelegate(e);
            }
        }
        _processing = false;

        //Add pendant listeners
        foreach (var listenerToAdd in _listenersToAdd)
        {
            AddListenerInternal(listenerToAdd);
        }
        _listenersToAdd.Clear();

        //Remove pendant listeners
        foreach (var listenerToRemove in _listenersToRemove)
        {
            RemoveListenerInternal(listenerToRemove);
        }
        _listenersToRemove.Clear();
    }

    #endregion

    #region Internals Methods

    private void AddListenerInternal(Listener listener)
    {
        Debug.Assert(listener != null, "Added a null listener.");
        if (!_listeners.TryGetValue(listener.EventType, out List<Delegate> delegateList))
        {
            delegateList = new List<Delegate>();
            _listeners[listener.EventType] = delegateList;
        }
        Debug.Assert(delegateList.Find(e => e == listener.EventDelegate) == null, "Added duplicated event listener to the event dispatcher.");
        delegateList.Add(listener.EventDelegate);
    }

    private void RemoveListenerInternal(Listener listener)
    {
        if (listener != null && _listeners.TryGetValue(listener.EventType, out List<Delegate> listeners))
        {
            listeners.RemoveAll(e => e == listener.EventDelegate);
        }
    }

    #endregion

    private class Listener
    {
        public Type EventType;
        public Delegate EventDelegate;
    }
}