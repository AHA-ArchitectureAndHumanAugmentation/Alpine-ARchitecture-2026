using System;
using System.Collections.Generic;

namespace Compas.Events
{
    /// <summary>
    /// Keeps track of every <see cref="EventBus{T}"/> that has been used, so all of them can be
    /// emptied in one call.
    ///
    /// <para>This exists for hosts that reuse a process across runs. Unity with
    /// <c>Enter Play Mode Options</c> enabled and <c>Reload Domain</c> disabled is the case that
    /// matters here: statics are not reset between play sessions, so listeners registered by
    /// objects from the previous session stay subscribed and are invoked against destroyed
    /// objects. Call <see cref="ClearAll"/> once at startup to get a clean bus.</para>
    /// </summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> clearActions = new List<Action>();

        /// <summary>
        /// Registers a callback that empties one bus. Called once per closed generic from the
        /// static constructor of <see cref="EventBus{T}"/>; there is no need to call it directly.
        /// </summary>
        public static void Register(Action clear)
        {
            if (clear == null)
            {
                throw new ArgumentNullException(nameof(clear));
            }

            clearActions.Add(clear);
        }

        /// <summary>
        /// Removes every listener from every bus that has been used so far.
        /// </summary>
        public static void ClearAll()
        {
            for (int i = 0; i < clearActions.Count; i++)
            {
                clearActions[i]();
            }
        }
    }
}
