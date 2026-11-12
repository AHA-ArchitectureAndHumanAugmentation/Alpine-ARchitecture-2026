#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Storage;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;


namespace Compas.Unity.Database.Firebase.Storage
{
    /// <summary>
    /// Provides file storage operations for a specific Firebase application instance.
    /// Supports uploading, downloading, and listing files in Firebase Cloud Storage.
    /// </summary>
    /// <remarks>
    /// This client uses both the native Firebase Storage SDK (for uploads and deletes)
    /// and HTTP REST API (for listings and metadata), as the Unity Firebase SDK v11.8.1
    /// does not include native list/listAll methods.
    ///
    /// All async operations support cancellation tokens for cleanup and timeout management.
    /// </remarks>
    public sealed class StorageClient
    {
        public FirebaseApp App { get; }
        public FirebaseStorage Storage { get; }
        /// <summary>
        /// Timeout applied to HTTP REST requests used for list and metadata operations.
        /// Defaults to 20 seconds. Must be greater than zero.
        /// </summary>
        public TimeSpan HttpTimeout
        {
            get => _httpTimeout;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), "HTTP timeout must be greater than zero.");
                _httpTimeout = value;
            }
        }
        private TimeSpan _httpTimeout = TimeSpan.FromSeconds(20);

        // Shared HttpClient for all HTTP listing calls.
        // This avoids creating/disposal per request and enables connection pooling.
        // Per-request timeouts are handled via HttpTimeout and linked cancellation tokens.
        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        public StorageClient(FirebaseApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            Storage = FirebaseStorage.GetInstance(app)
                ?? throw new InvalidOperationException(
                    "FirebaseStorage.GetInstance(app) returned null.");
        }

        // ---------------------------------------------------------------------
        // Reference helpers
        // ---------------------------------------------------------------------

        /// <summary>
        /// Get a StorageReference at the given path (e.g. "obj_storage/folder/file.obj").
        /// Leading/trailing slashes are normalized.
        /// </summary>
        public StorageReference ConstructReference(string cloudPath)
        {
            if (string.IsNullOrWhiteSpace(cloudPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(cloudPath));
            return Storage.GetReference(NormalizePath(cloudPath));
        }

        // ---------------------------------------------------------------------
        // Core API Features
        // ---------------------------------------------------------------------
        
        /// <summary>
        /// Upload byte data to the specified cloud path.
        /// </summary>
        public Task UploadBytesAsync(string path, byte[] data, CancellationToken ct = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            ct.ThrowIfCancellationRequested();
            return ConstructReference(path).PutBytesAsync(data);
        }
        
        /// <summary>
        /// Delete a file at the specified cloud path.
        /// </summary>
        public Task DeleteAsync(string path, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return ConstructReference(path).DeleteAsync();
        }

        // ---------------------------------------------------------------------
        // File operations (HTTP REST API)
        // ---------------------------------------------------------------------
        // Note: Firebase Storage Unity SDK v11.8.1 does not include list/listAll methods.
        // Use the HTTP-based methods below instead.

        /// <summary>
        /// Get metadata and download URLs for all files in a cloud folder.
        /// Uses HttpTimeout for the underlying HTTP request.
        /// </summary>
        public async Task<IReadOnlyList<FileMetadata>> GetAllFilesInFolderAsUrlAsync(
            string cloudFolderPath,
            CancellationToken ct = default)
        {
            if (cloudFolderPath == null)
                throw new ArgumentNullException(nameof(cloudFolderPath));

            ct.ThrowIfCancellationRequested();

            var bucket = GetBucketOrThrow();
            var normalizedPrefix = NormalizePrefix(cloudFolderPath);
            var url = BuildListUrl(bucket, normalizedPrefix);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(HttpTimeout);
            using var resp = await SharedHttpClient.SendAsync(req, timeoutCts.Token).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Storage HTTP list failed ({(int)resp.StatusCode} {resp.ReasonPhrase}). " +
                    $"Body: {TrimForError(body)}");
            }

            var parsed = JsonConvert.DeserializeObject<ListFilesResponse>(body);
            var items = parsed?.items?.ToArray() ?? Array.Empty<FileMetadata>();

            // Populate download URIs for all files
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.uri))
                {
                    var encodedPath = Uri.EscapeDataString(item.name);
                    item.uri = $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{encodedPath}?alt=media";
                }
            }

            return items;
        }

        /// <summary>
        /// Get metadata and download URL for a specific file.
        /// Uses HttpTimeout for the underlying HTTP request.
        /// </summary>
        public async Task<FileMetadata> GetFileUriAsync(
            string cloudFilePath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cloudFilePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(cloudFilePath));

            ct.ThrowIfCancellationRequested();

            var bucket = GetBucketOrThrow();
            var normalizedPath = NormalizePath(cloudFilePath);
            var encodedPath = Uri.EscapeDataString(normalizedPath);
            var url = $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{encodedPath}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(HttpTimeout);
            using var resp = await SharedHttpClient.SendAsync(req, timeoutCts.Token).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Storage HTTP get file failed ({(int)resp.StatusCode} {resp.ReasonPhrase}). " +
                    $"Body: {TrimForError(body)}");
            }

            var fileMetadata = JsonConvert.DeserializeObject<FileMetadata>(body);
            if (fileMetadata == null)
                throw new InvalidOperationException("Failed to deserialize file metadata.");

            // Build download URL manually since the metadata endpoint doesn't include it
            fileMetadata.uri = $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{encodedPath}?alt=media";

            return fileMetadata;
        }

        // ---------------------------------------------------------------------
        // Download operations
        // ---------------------------------------------------------------------

        /// <summary>
        /// Download a file from cloud storage to a local path.
        /// If localPath is a directory, the file will be saved there with its cloud name.
        /// </summary>
        public async Task DownloadFileFromCloudAsync(
            string cloudFilePath,
            string localPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cloudFilePath))
                throw new ArgumentException("Cloud file path cannot be null or empty.", nameof(cloudFilePath));
            if (string.IsNullOrWhiteSpace(localPath))
                throw new ArgumentException("Local path cannot be null or empty.", nameof(localPath));

            ct.ThrowIfCancellationRequested();

            var fileMetadata = await GetFileUriAsync(cloudFilePath, ct);
            if (string.IsNullOrEmpty(fileMetadata.uri))
                throw new InvalidOperationException(
                    $"File metadata for {fileMetadata.name} does not contain a download URI.");

            var targetPath = ResolveLocalFilePath(localPath, fileMetadata.name);
            await DownloadFileAsyncUnityWebRequest(fileMetadata.uri, targetPath, ct);
        }

        /// <summary>
        /// Download all files from a cloud folder to a local directory.
        /// </summary>
        public async Task DownloadFolderFromCloudAsync(
            string cloudFolderPath,
            string localDirectoryPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cloudFolderPath))
                throw new ArgumentException("Cloud folder path cannot be null or empty.", nameof(cloudFolderPath));
            if (string.IsNullOrWhiteSpace(localDirectoryPath))
                throw new ArgumentException("Local directory path cannot be null or empty.", nameof(localDirectoryPath));

            ct.ThrowIfCancellationRequested();

            var files = await GetAllFilesInFolderAsUrlAsync(cloudFolderPath, ct);
            if (files.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No files found at path '{cloudFolderPath}'. It may not exist or may be empty.");
            }

            await DownloadAllFilesInFolderAsyncUnityWebRequest(files, localDirectoryPath, ct);
        }

        /// <summary>
        /// Asynchronously downloads a single file from a URL to a local path using UnityWebRequest.
        /// Creates any necessary parent directories automatically.
        /// </summary>
        /// <param name="downloadUrl">The full download URL for the file.</param>
        /// <param name="localFilePath">The local file path where the file will be saved.</param>
        /// <param name="ct">Cancellation token for aborting the download.</param>
        /// <remarks>
        /// This method uses UnityWebRequest instead of HttpClient to ensure compatibility with Unity's threading model.
        /// </remarks>
        private async Task DownloadFileAsyncUnityWebRequest(
            string downloadUrl,
            string localFilePath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new ArgumentException("Download URL cannot be null or empty.", nameof(downloadUrl));
            if (string.IsNullOrWhiteSpace(localFilePath))
                throw new ArgumentException("Local file path cannot be null or empty.", nameof(localFilePath));

            ct.ThrowIfCancellationRequested();

            var directory = Path.GetDirectoryName(localFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var webRequest = UnityWebRequest.Get(downloadUrl))
            {
                webRequest.downloadHandler = new DownloadHandlerFile(localFilePath);
                var asyncOp = webRequest.SendWebRequest();

                while (!asyncOp.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new HttpRequestException(
                        $"Download failed for {downloadUrl}: {webRequest.error}");
                }
            }
        }

        /// <summary>
        /// Asynchronously downloads multiple files from a list of file metadata to a local directory.
        /// </summary>
        /// <param name="filesMetadata">The collection of file metadata containing download URIs.</param>
        /// <param name="localDirectoryPath">The local directory path where files will be saved.</param>
        /// <param name="ct">Cancellation token for aborting the download operations.</param>
        private async Task DownloadAllFilesInFolderAsyncUnityWebRequest(
            IEnumerable<FileMetadata> filesMetadata,
            string localDirectoryPath,
            CancellationToken ct = default)
        {
            if (filesMetadata == null)
                throw new ArgumentNullException(nameof(filesMetadata));
            if (string.IsNullOrWhiteSpace(localDirectoryPath))
                throw new ArgumentException("Local directory path cannot be null or empty.", nameof(localDirectoryPath));

            ct.ThrowIfCancellationRequested();

            Directory.CreateDirectory(localDirectoryPath);

            var downloadTasks = new List<Task>();
            foreach (var fileMetadata in filesMetadata)
            {
                if (string.IsNullOrEmpty(fileMetadata.uri))
                {
                    throw new InvalidOperationException(
                        $"File metadata for {fileMetadata.name} does not contain a download URI.");
                }

                string localFilePath = Path.Combine(localDirectoryPath, Path.GetFileName(fileMetadata.name));
                downloadTasks.Add(DownloadFileAsyncUnityWebRequest(fileMetadata.uri, localFilePath, ct));
            }

            await Task.WhenAll(downloadTasks);
        }

        /// <summary>
        /// File metadata including name, bucket, and download URL.
        /// </summary>
        public sealed class FileMetadata
        {
            public string name { get; set; } = string.Empty;
            public string bucket { get; set; } = string.Empty;
            public string? uri { get; set; }
        }

        private sealed class ListFilesResponse
        {
            public List<FileMetadata>? items { get; set; }
            public List<string>? prefixes { get; set; }
        }

        // ---------------------------------------------------------------------
        // Internals
        // ---------------------------------------------------------------------
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            var p = path.Trim();

            while (p.StartsWith("/")) p = p.Substring(1);
            while (p.EndsWith("/")) p = p.Substring(0, p.Length - 1);

            if (p.Length == 0)
                throw new ArgumentException("Path cannot be empty after normalization.", nameof(path));

            // Validate no control characters or null bytes
            if (p.Any(c => char.IsControl(c) || c == '\0'))
                throw new ArgumentException("Path contains invalid control characters.", nameof(path));

            return p;
        }
        /// <summary>
        /// Normalizes a path to use as a prefix for list operations, ensuring it behaves like a folder path.
        /// </summary>
        /// <param name="prefix">The prefix path to normalize.</param>
        /// <returns>The normalized prefix path with trailing slash for folder semantics.</returns>
        private static string NormalizePrefix(string prefix)
        {
            var p = prefix.Trim();

            while (p.StartsWith("/")) p = p.Substring(1);

            // Folder semantics: ensure trailing slash so prefix behaves like a "folder"
            if (p.Length > 0 && !p.EndsWith("/", StringComparison.Ordinal))
                p += "/";

            return p;
        }
        /// <summary>
        /// Constructs the HTTP request URL for listing objects in a Firebase Storage path.
        /// </summary>
        /// <param name="bucket">The storage bucket name.</param>
        /// <param name="prefix">The normalized prefix path to list objects from.</param>
        /// <returns>The complete URL for the list request.</returns>
        private static string BuildListUrl(string bucket, string prefix)
        {
            var encodedPrefix = Uri.EscapeDataString(prefix);
            return $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o?prefix={encodedPrefix}&delimiter=/";
        }
        /// <summary>
        /// Gets the storage bucket name from the Firebase app options, throwing if not configured.
        /// </summary>
        /// <returns>The storage bucket name.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the bucket is not configured on the Firebase app.</exception>
        private string GetBucketOrThrow()
        {
            var bucket = App?.Options?.StorageBucket;
            if (string.IsNullOrWhiteSpace(bucket))
                throw new InvalidOperationException(
                    "Firebase Storage bucket is not configured on the FirebaseApp options.");
            return bucket;
        }
        /// <summary>
        /// Trims an error message string to a maximum length for logging, appending an ellipsis if truncated.
        /// </summary>
        /// <param name="s">The string to trim.</param>
        /// <param name="max">The maximum length allowed (default: 600 characters).</param>
        /// <returns>The trimmed string.</returns>
        private static string TrimForError(string s, int max = 600)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
        /// <summary>
        /// Resolves the final local file path, handling both file and directory path inputs.
        /// </summary>
        /// <param name="localPath">The target local path (file or directory).</param>
        /// <param name="remoteName">The remote file name to extract the filename from.</param>
        /// <returns>The resolved local file path.</returns>
        private static string ResolveLocalFilePath(string localPath, string remoteName)
        {
            var fileName = Path.GetFileName(remoteName);
            if (string.IsNullOrEmpty(fileName))
                throw new InvalidOperationException("Remote file name is empty.");

            if (LooksLikeDirectoryPath(localPath))
            {
                Directory.CreateDirectory(localPath);
                return Path.Combine(localPath, fileName);
            }

            return localPath;
        }
        /// <summary>
        /// Determines whether a path looks like a directory path (exists as directory or ends with separator).
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path appears to be a directory; otherwise, false.</returns>
        private static bool LooksLikeDirectoryPath(string path)
        {
            if (Directory.Exists(path)) return true;
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

    }

}