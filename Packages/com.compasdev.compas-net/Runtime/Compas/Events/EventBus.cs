using System;

namespace Compas.Events
{
    public interface IEvent { }

    /// <summary>
    /// The event bus handles events for communicating data between classes without coupling
    /// them.
    ///
    /// <para> How to use: Create a data struct with the info that needs to be transmitted over.</para>
    /// <para>EventBus&lt;ConnectionEstablished&gt;.Raise(new ConnectionEstablished(brokerAddress))</para>
    ///
    /// <para>Listeners are held in a static event, so pair every <see cref="AddListener"/> with a
    /// <see cref="RemoveListener"/> - in Unity, from OnEnable/OnDisable. See
    /// <see cref="EventBusRegistry"/> for clearing every bus at once when the host reuses a
    /// process across runs.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class EventBus<T> where T : struct, IEvent
    {
        private static event Action<T> OnEvent;

        // Runs once per closed generic, the first time this bus is touched, which is exactly
        // often enough for ClearAll() to know about every bus that could hold a listener.
        static EventBus() => EventBusRegistry.Register(Clear);

        public static void AddListener(Action<T> listener)
        {
            OnEvent += listener;
        }

        public static void RemoveListener(Action<T> listener)
        {
            OnEvent -= listener;
        }

        public static void Raise(T eventData)
        {
            EventBus.DebugLog?.Invoke($"Raise {typeof(T)}");
            OnEvent?.Invoke(eventData);
        }

        /// <summary>Removes every listener from this bus.</summary>
        public static void Clear() => OnEvent = null;
    }

    public static class EventBus
    {
        public static Action<string> DebugLog { get; set; }
    }
}
