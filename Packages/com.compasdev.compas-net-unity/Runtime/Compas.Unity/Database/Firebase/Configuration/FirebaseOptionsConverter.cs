
using System;
using Firebase;

namespace Compas.Unity.Database.Firebase.Configuration
{
    /// <summary>
    /// Provides conversion utilities to transform <see cref="FirebaseConfig"/> objects into Firebase <see cref="AppOptions"/> instances.
    /// </summary>
    public static class FirebaseOptionsConverter
    {
        /// <summary>
        /// Creates a new <see cref="AppOptions"/> instance from a <see cref="FirebaseConfig"/> object.
        /// </summary>
        /// <param name="config">The Firebase configuration to convert.</param>
        /// <returns>A new <see cref="AppOptions"/> instance configured from the provided config.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided config is invalid.</exception>
        public static AppOptions Create(FirebaseConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            if (!config.IsValid(out string reason))
                throw new ArgumentException($"Invalid FirebaseConfig: {reason}", nameof(config));

            return new AppOptions
            {
                AppId = config.AppId,
                ApiKey = config.ApiKey,
                DatabaseUrl = new Uri(config.DatabaseUrl),
                StorageBucket = config.StorageBucket,
                ProjectId = config.ProjectId
            };
        }

        /// <summary>
        /// Attempts to create a new <see cref="AppOptions"/> instance from a <see cref="FirebaseConfig"/> object.
        /// </summary>
        /// <param name="config">The Firebase configuration to convert.</param>
        /// <param name="options">When this method returns true, contains the newly created <see cref="AppOptions"/> instance; otherwise, null.</param>
        /// <param name="reason">When this method returns false, contains a description of the error; otherwise, null.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryCreate(
            FirebaseConfig config,
            out AppOptions options,
            out string reason)
        {
            options = null;
            reason = null;

            if (config == null)
            {
                reason = "FirebaseConfig is null.";
                return false;
            }

            if (!config.IsValid(out reason))
                return false;

            options = new AppOptions
            {
                AppId = config.AppId,
                ApiKey = config.ApiKey,
                DatabaseUrl = new Uri(config.DatabaseUrl),
                StorageBucket = config.StorageBucket,
                ProjectId = config.ProjectId
            };

            return true;
        }
    }
}