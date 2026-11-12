#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;

namespace Compas.Unity.Database.Firebase.RealtimeDatabase
{
    /// <summary>
    /// Provides real-time database operations for a specific Firebase application instance.
    /// Encapsulates all read, write, and event subscription functionality for Firebase Realtime Database.
    /// </summary>
    /// <remarks>
    /// This client is lightweight and delegates all Firebase operations to the underlying FirebaseDatabase instance.
    /// Responsibilities:
    /// - Provide minimal read/write/delete operations with consistent error handling
    /// - Expose RTDB event subscriptions with explicit lifetimes via IDisposable
    /// - Normalize and validate database paths
    ///
    /// Non-responsibilities (handled externally):
    /// - Firebase initialization or dependency resolution
    /// - Scene lifecycle management
    /// - Application-specific paths or data schemas
    /// </remarks>
    public sealed class RealtimeDatabaseClient
    {
        /// <summary>
        /// Firebase application instance this client is bound to.
        /// </summary>
        public FirebaseApp App { get; }

        /// <summary>
        /// Firebase Realtime Database instance derived from <see cref="App"/>.
        /// </summary>
        public FirebaseDatabase Database { get; }

        /// <summary>
        /// Create a RealtimeDatabaseClient bound to the provided FirebaseApp.
        /// </summary>
        public RealtimeDatabaseClient(FirebaseApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            Database = FirebaseDatabase.GetInstance(app)
                ?? throw new InvalidOperationException(
                    "FirebaseDatabase.GetInstance(app) returned null.");
        }

        // ---------------------------------------------------------------------
        // Reference helpers //TODO: Pull from DatabasereReference too?
        // ---------------------------------------------------------------------

        /// <summary>
        /// Get a DatabaseReference at the given path
        /// (e.g. "project/assembly/graph/node").
        /// </summary>
        public DatabaseReference ConstructReference(string dataPath)
        {
            return Database.GetReference(NormalizePath(dataPath));
        }

        // ---------------------------------------------------------------------
        // Read operations
        // ---------------------------------------------------------------------

        /// <summary>
        /// Fetch a single value snapshot asynchronously from the given database path.
        /// </summary>
        public async Task<DataSnapshot> GetAsync(
            string dataPath,
            CancellationToken cancellationToken = default)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            cancellationToken.ThrowIfCancellationRequested();
            return await ConstructReference(dataPath).GetValueAsync();
        }

        // ---------------------------------------------------------------------
        // Write operations
        // ---------------------------------------------------------------------

        /// <summary>
        /// Set raw JSON string data at the given database path.
        /// </summary>
        public Task SetRawJsonAsync(
            string dataPath,
            string json,
            CancellationToken cancellationToken = default)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            if (json == null) throw new ArgumentNullException(nameof(json));
            cancellationToken.ThrowIfCancellationRequested();
            return ConstructReference(dataPath).SetRawJsonValueAsync(json);
        }

        /// <summary>
        /// Set an object value at the given database path, replacing any existing data.
        /// </summary>
        public Task SetValueAsync(
            string dataPath,
            object value,
            CancellationToken cancellationToken = default)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (!IsFirebaseSupportedValue(value))
                throw new ArgumentException(
                    "Value type is not supported by Firebase Realtime Database.",
                    nameof(value));

            cancellationToken.ThrowIfCancellationRequested();
            return ConstructReference(dataPath).SetValueAsync(value);
        }

        /// <summary>
        /// Delete (remove) data at the given database path.
        /// </summary>
        public Task DeleteAsync(
            string dataPath,
            CancellationToken cancellationToken = default)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            cancellationToken.ThrowIfCancellationRequested();
            return ConstructReference(dataPath).RemoveValueAsync();
        }

        /// <summary>
        /// Generate a unique child key under the given path.
        /// 
        /// NOTE: This does NOT write data automatically.
        /// </summary>
        public (string key, DatabaseReference reference) Push(string dataPath)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            var reference = ConstructReference(dataPath).Push();
            var key = reference.Key;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException(
                    "Push() returned a reference with a null or empty key.");

            return (key, reference);
        }

        // ---------------------------------------------------------------------
        // Event subscriptions
        // ---------------------------------------------------------------------

        //TODO: Check if these work as expected. Particularly in Unity.
        /// <summary>
        /// Subscribe to value changes at the given database path. Dispose the returned subscription to unsubscribe.
        /// </summary>
        public IDisposable ListenValueChanged(
            string dataPath,
            EventHandler<ValueChangedEventArgs> handler)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var reference = ConstructReference(dataPath);
            reference.ValueChanged += handler;

            return new Subscription(() =>
                reference.ValueChanged -= handler);
        }

        /// <summary>
        /// Subscribe to child additions at the given database path. Dispose the returned subscription to unsubscribe.
        /// </summary>
        public IDisposable ListenChildAdded(
            string dataPath,
            EventHandler<ChildChangedEventArgs> handler)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var reference = ConstructReference(dataPath);
            reference.ChildAdded += handler;

            return new Subscription(() =>
                reference.ChildAdded -= handler);
        }

        /// <summary>
        /// Subscribe to child modifications at the given database path. Dispose the returned subscription to unsubscribe.
        /// </summary>
        public IDisposable ListenChildChanged(
            string dataPath,
            EventHandler<ChildChangedEventArgs> handler)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var reference = ConstructReference(dataPath);
            reference.ChildChanged += handler;

            return new Subscription(() =>
                reference.ChildChanged -= handler);
        }

        /// <summary>
        /// Subscribe to child removals at the given database path. Dispose the returned subscription to unsubscribe.
        /// </summary>
        public IDisposable ListenChildRemoved(
            string dataPath,
            EventHandler<ChildChangedEventArgs> handler)
        {
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var reference = ConstructReference(dataPath);
            reference.ChildRemoved += handler;

            return new Subscription(() =>
                reference.ChildRemoved -= handler);
        }

        // ---------------------------------------------------------------------
        // Internals
        // ---------------------------------------------------------------------

        /// <summary>
        /// Normalize and validate a database path (removes leading/trailing slashes).
        /// </summary>
        private static string NormalizePath(string dataPath)
        {
            if (string.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(dataPath));

            var p = dataPath.Trim();

            while (p.StartsWith("/")) p = p.Substring(1);
            while (p.EndsWith("/")) p = p.Substring(0, p.Length - 1);

            if (p.Length == 0)
                throw new ArgumentException("Path cannot be empty after normalization.", nameof(dataPath));

            return p;
        }

        /// <summary>
        /// Manages event subscription lifetime and cleanup.
        /// </summary>
        /// <remarks>
        /// When disposed, automatically unsubscribes from the Firebase event to prevent memory leaks.
        /// </remarks>
        private sealed class Subscription : IDisposable
        {
            private Action? _unsubscribe;

            /// <summary>
            /// Initializes a new instance of the <see cref=\"Subscription\"/> class.
            /// </summary>
            /// <param name=\"unsubscribe\">The action to invoke when this subscription is disposed.</param>
            public Subscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            /// <summary>
            /// Disposes the subscription and unsubscribes from the Firebase event.
            /// </summary>
            public void Dispose()
            {
                _unsubscribe?.Invoke();
                _unsubscribe = null;
            }
        }

        /// <summary>
        /// Determines whether a value type is supported by Firebase Realtime Database.
        /// </summary>
        /// <param name=\"value\">The value to validate.</param>
        /// <returns>True if the value type can be stored in Firebase; otherwise, false.</returns>
        /// <remarks>
        /// Supported types include primitives (string, int, float, double, bool, etc.),\n        /// and collections (IDictionary, IList). Complex objects must be converted to supported types first.\n        /// </remarks>\n        
        private static bool IsFirebaseSupportedValue(object value)
        {
            if (value is string || value is bool) return true;

            // Numeric primitives
            if (value is int || value is long || value is float || value is double
                || value is short || value is byte || value is uint || value is ulong
                || value is decimal) return true; // decimal will serialize oddly sometimes; consider converting to double

            // Collections
            if (value is System.Collections.IDictionary) return true;
            if (value is System.Collections.IList) return true;

            return false;
        }

    }
}