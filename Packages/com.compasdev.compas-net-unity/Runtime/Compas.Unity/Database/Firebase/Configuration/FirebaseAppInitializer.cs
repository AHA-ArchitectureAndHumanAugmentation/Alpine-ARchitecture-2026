using System;
using System.Threading.Tasks;
using Firebase;

namespace Compas.Unity.Database.Firebase.Configuration
{
    /// <summary>
    /// Provides initialization utilities for Firebase applications.
    /// Handles dependency checking, validation, and graceful reuse of existing Firebase instances.
    /// </summary>
    /// <remarks>
    /// This static class abstracts the complexity of setting up Firebase in the CompasXR system.
    /// It ensures all necessary dependencies are available before app creation and prevents duplicate initialization.
    /// </remarks>
    public static class FirebaseAppInitializer
    {
        /// <summary>
        /// Asynchronously initializes a Firebase application instance, checking dependencies and reusing existing instances when available.
        /// </summary>
        /// <param name="config">The Firebase configuration containing app settings.</param>
        /// <param name="appName">Optional name for the Firebase app instance. If null or empty, the default instance is used.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the initialized <see cref="FirebaseApp"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided config is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when Firebase dependencies are not available or app creation fails unexpectedly.</exception>
        /// <remarks>
        /// This method will reuse an existing Firebase app instance if one with the specified name already exists.
        /// If no name is provided, the default Firebase instance will be returned if it exists.
        /// TODO: Exception strategy may be expanded in future versions.
        /// </remarks>
        public static async Task<FirebaseApp> InitializeAppAsync(
            FirebaseConfig config,
            string appName = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.IsValid(out string reason))
                throw new ArgumentException($"Invalid FirebaseConfig: {reason}", nameof(config));

            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new InvalidOperationException($"Firebase dependencies are not available: {status}");

            // Reuse existing app if present
            if (!string.IsNullOrEmpty(appName))
            {
                try
                {
                    var existing = FirebaseApp.GetInstance(appName);
                    if (existing != null) return existing;
                }
                catch (ArgumentException)
                {
                    // App doesn't exist yet -> create it below.
                }
            }
            else if (FirebaseApp.DefaultInstance != null)
            {
                return FirebaseApp.DefaultInstance;
            }

            // Create new app
            var options = FirebaseOptionsConverter.Create(config);
            FirebaseApp created = string.IsNullOrEmpty(appName)
                ? FirebaseApp.Create(options)
                : FirebaseApp.Create(options, appName);

            if (created == null)
            {
                throw new InvalidOperationException(
                    "FirebaseApp.Create returned null. App creation failed unexpectedly.");
            }

            return created;
        }
    }
}