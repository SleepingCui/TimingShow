using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace TimingShow.Update
{
    internal static class UpdateClient
    {
        private const int RequestTimeoutMs = 20000;
        private const int DownloadTimeoutMs = 60000;

        private static readonly string[] TrustedHosts = { "github.com", "githubusercontent.com" };

        static UpdateClient()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch (NotSupportedException)
            {
            }
        }

        public static string FetchRepository(string url)
        {
            HttpWebRequest request = CreateRequest(AppendCacheBuster(url), RequestTimeoutMs);
            request.Accept = "application/json";
            using (WebResponse response = request.GetResponse())
            {
                ValidateResponse(response);
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static UpdateRelease FindRelease(string json, string id, Version currentVersion)
        {
            UpdateManifest manifest = JsonConvert.DeserializeObject<UpdateManifest>(json);
            if (manifest == null || manifest.Releases == null)
                throw new InvalidDataException("Repository.json does not contain a Releases list.");

            foreach (UpdateRelease release in manifest.Releases)
            {
                if (release == null || !string.Equals(release.Id, id, StringComparison.OrdinalIgnoreCase)) continue;

                Version candidate;
                if (!TryParseVersion(release.Version, out candidate) || candidate <= currentVersion) continue;

                if (string.IsNullOrEmpty(release.DownloadUrl))
                    throw new InvalidDataException("Release " + release.Version + " has no DownloadUrl.");
                if (string.IsNullOrEmpty(release.Sha256))
                    throw new InvalidDataException("Release " + release.Version + " has no Sha256 checksum.");
                if (release.Size <= 0)
                    throw new InvalidDataException("Release " + release.Version + " has no Size.");
                return release;
            }
            return null;
        }

        public static void Download(UpdateRelease release, string destination, Action<float> onProgress, Func<bool> isCancelled)
        {
            Uri uri = ValidateUrl(release.DownloadUrl);
            HttpWebRequest request = CreateRequest(uri, DownloadTimeoutMs);
            request.Accept = "application/octet-stream";
            using (WebResponse response = request.GetResponse())
            {
                ValidateResponse(response);
                long expected = release.Size > 0 ? release.Size : response.ContentLength;
                byte[] package = ReadAll(response, release.Size, expected, onProgress, isCancelled);
                using (SHA256 sha = SHA256.Create())
                {
                    string actual = ToHex(sha.ComputeHash(package));
                    if (!string.Equals(actual, release.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("SHA-256 mismatch: expected " + release.Sha256 + ", got " + actual + ".");
                }
                File.WriteAllBytes(destination, package);
            }
        }

        private static byte[] ReadAll(WebResponse response, long expectedSize, long progressTotal, Action<float> onProgress, Func<bool> isCancelled)
        {
            using (Stream input = response.GetResponseStream())
            using (MemoryStream buffer = new MemoryStream())
            {
                byte[] chunk = new byte[81920];
                long received = 0;
                int read;
                while ((read = input.Read(chunk, 0, chunk.Length)) > 0)
                {
                    if (isCancelled != null && isCancelled()) throw new OperationCanceledException();
                    buffer.Write(chunk, 0, read);
                    received += read;
                    if (expectedSize > 0 && received > expectedSize)
                        throw new InvalidDataException("Downloaded package is larger than the repository entry states.");
                    if (onProgress != null && progressTotal > 0)
                        onProgress(Math.Min(1f, (float)((double)received / progressTotal)));
                }

                if (expectedSize > 0 && received != expectedSize)
                    throw new InvalidDataException("Downloaded package size " + received + " does not match the repository entry " + expectedSize + ".");
                return buffer.ToArray();
            }
        }

        public static Uri ValidateUrl(string url)
        {
            Uri uri;
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri))
                throw new InvalidDataException("Invalid update URL: " + url);
            if (uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("Update URL must use HTTPS: " + url);
            if (!IsTrustedHost(uri.Host))
                throw new InvalidDataException("Untrusted update host: " + uri.Host);
            return uri;
        }

        public static bool TryParseVersion(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(text)) return false;
            return Version.TryParse(text.Trim().TrimStart('v', 'V'), out version);
        }

        public static string ToHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("X2"));
            return builder.ToString();
        }

        private static bool IsTrustedHost(string host)
        {
            foreach (string trusted in TrustedHosts)
            {
                if (string.Equals(host, trusted, StringComparison.OrdinalIgnoreCase)) return true;
                if (host.EndsWith("." + trusted, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static void ValidateResponse(WebResponse response)
        {
            if (response.ResponseUri != null && !IsTrustedHost(response.ResponseUri.Host))
                throw new InvalidDataException("Request was redirected to an untrusted host: " + response.ResponseUri.Host);
        }

        private static string AppendCacheBuster(string url)
        {
            string separator = url.IndexOf('?') >= 0 ? "&" : "?";
            return url + separator + "t=" + DateTime.UtcNow.Ticks;
        }

        private static HttpWebRequest CreateRequest(string url, int timeoutMs)
        {
            return CreateRequest(ValidateUrl(url), timeoutMs);
        }

        private static HttpWebRequest CreateRequest(Uri uri, int timeoutMs)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;
            request.UserAgent = "TimingShow-Updater";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }
    }
}
