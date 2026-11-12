using System;

namespace Compas.Unity.Database.Firebase.Configuration
{
    /// <summary>
    /// Plain data container for Firebase configuration settings.
    /// This class contains no Firebase, Unity, or platform-specific logic.
    /// </summary>
    public sealed class FirebaseConfig
    {
        /// <summary>
        /// Gets the unique identifier for the Firebase app.
        /// </summary>
        public string AppId { get; }

        /// <summary>
        /// Gets the API key for authenticating with Firebase services.
        /// </summary>
        public string ApiKey { get; }

        /// <summary>
        /// Gets the Firebase Realtime Database URL.
        /// </summary>
        public string DatabaseUrl { get; }

        /// <summary>
        /// Gets the Firebase Cloud Storage bucket identifier.
        /// </summary>
        public string StorageBucket { get; }

        /// <summary>
        /// Gets the Firebase project identifier.
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirebaseConfig"/> class.
        /// </summary>
        /// <param name="appId">The unique identifier for the Firebase app.</param>
        /// <param name="apiKey">The API key for authenticating with Firebase services.</param>
        /// <param name="databaseUrl">The Firebase Realtime Database URL.</param>
        /// <param name="storageBucket">The Firebase Cloud Storage bucket identifier.</param>
        /// <param name="projectId">The Firebase project identifier.</param>
        public FirebaseConfig(
            string appId,
            string apiKey,
            string databaseUrl,
            string storageBucket,
            string projectId)
        {
            AppId = appId;
            ApiKey = apiKey;
            DatabaseUrl = databaseUrl;
            StorageBucket = storageBucket;
            ProjectId = projectId;
        }

        /// <summary>
        /// Validates the configuration by checking that all required fields are populated and valid.
        /// </summary>
        /// <param name="reason">When this method returns false, contains a description of the validation failure; otherwise, null.</param>
        /// <returns><c>true</c> if the configuration is valid; otherwise, <c>false</c>.</returns>
        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(AppId))
            {
                reason = "AppId is null or empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                reason = "ApiKey is null or empty.";
                return false;
            }

            if (!Uri.TryCreate(DatabaseUrl, UriKind.Absolute, out _))
            {
                reason = "DatabaseUrl is not a valid absolute URI.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(StorageBucket))
            {
                reason = "StorageBucket is null or empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ProjectId))
            {
                reason = "ProjectId is null or empty.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}