using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TimingShow
{
    public static class LogAnalyzerBridge
    {
        private const string AnalyzerUrl = "https://sleepingcui.github.io/adofai_offset_analyzer/";
        private static readonly object Sync = new object();
        private static TcpListener _listener;

        public const int MinTimeoutSeconds = 5;
        public const int MaxTimeoutSeconds = 3600;
        public const int DefaultTimeoutSeconds = 60;

        public static string CreateUrl(string filePath, int requestedPort = 0, int timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("Log file not found", filePath);
            if (requestedPort < 0 || requestedPort > 65535)
                throw new ArgumentOutOfRangeException("requestedPort", "Port must be between 0 and 65535");
            if (timeoutSeconds < MinTimeoutSeconds || timeoutSeconds > MaxTimeoutSeconds)
                throw new ArgumentOutOfRangeException("timeoutSeconds", "Timeout must be between " + MinTimeoutSeconds + " and " + MaxTimeoutSeconds + " seconds");
            Stop();

            string token = Guid.NewGuid().ToString("N");
            TcpListener listener = new TcpListener(IPAddress.Loopback, requestedPort);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            lock (Sync) _listener = listener;
            var worker = new Thread(() => Server(listener, filePath, token))
            {
                IsBackground = true,
                Name = "TimingShow.LogAnalyzerBridge"
            };
            var expiry = new Thread(() => Expire(listener, timeoutSeconds))
            {
                IsBackground = true,
                Name = "TimingShow.LogAnalyzerExpiry"
            };
            worker.Start();
            expiry.Start();

            string source = "http://127.0.0.1:" + port + "/log?token=" + token;
            string analyzerUrl = AnalyzerUrl + "?source=" + Uri.EscapeDataString(source) + "&name=" + Uri.EscapeDataString(Path.GetFileName(filePath));
            ModContext.Logger.Log($"server started at 127.0.0.1:{port}");
            ModContext.Logger.Log($"token={token}");
            return analyzerUrl;
        }

        private static void Server(TcpListener listener, string filePath, string token)
        {
            TcpClient client = null;
            try
            {
                client = listener.AcceptTcpClient();
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    stream.ReadTimeout = 10000;
                    string request = ReadHeaders(stream);
                    string requestLine = request.Split(new[] { "\r\n" }, StringSplitOptions.None)[0];
                    string expectedPath = "/log?token=" + token;
                    bool isOptions = requestLine.StartsWith("OPTIONS ", StringComparison.OrdinalIgnoreCase);
                    bool isGet = requestLine.StartsWith("GET " + expectedPath + " ", StringComparison.Ordinal);
                    if (isOptions)
                    {
                        WriteResponse(stream, 204, "", new byte[0], "text/plain");
                    }
                    else if (isGet)
                    {
                        byte[] data = File.ReadAllBytes(filePath);
                        WriteResponse(stream, 200, "OK", data, ContentType(filePath), Path.GetFileName(filePath));
                        ModContext.Logger.Log("Sent " + data.Length + " bytes for " + token);
                    }
                    else
                    {
                        WriteResponse(stream, 404, "Not Found", Encoding.UTF8.GetBytes("Not Found"), "text/plain");
                    }
                }
            }
            catch (Exception e)
            {
                ModContext.Logger.Log(e.Message);
            }
            finally
            {
                Stop(listener);
                if (client != null) client.Close();
            }
        }

        private static void Expire(TcpListener listener, int timeoutSeconds)
        {
            Thread.Sleep(TimeSpan.FromSeconds(timeoutSeconds));
            Stop(listener);
        }

        private static string ReadHeaders(NetworkStream stream)
        {
            var bytes = new MemoryStream();
            int previous = -1;
            int current;
            while (bytes.Length < 16384 && (current = stream.ReadByte()) >= 0)
            {
                bytes.WriteByte((byte)current);
                if (previous == '\r' && current == '\n' && EndsWithHeaderTerminator(bytes)) break;
                previous = current;
            }
            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private static bool EndsWithHeaderTerminator(MemoryStream stream)
        {
            byte[] bytes = stream.GetBuffer();
            int n = (int)stream.Length;
            return n >= 4 && bytes[n - 4] == '\r' && bytes[n - 3] == '\n' && bytes[n - 2] == '\r' && bytes[n - 1] == '\n';
        }

        private static void WriteResponse(NetworkStream stream, int status, string reason, byte[] data, string contentType, string fileName = null)
        {
            string disposition = string.IsNullOrEmpty(fileName) ? "" : "\r\nContent-Disposition: attachment; filename=\"" + fileName.Replace("\"", "") + "\"";
            string header = "HTTP/1.1 " + status + " " + reason + "\r\n" +
                            "Access-Control-Allow-Origin: https://sleepingcui.github.io\r\n" +
                            "Access-Control-Allow-Methods: GET, OPTIONS\r\n" +
                            "Access-Control-Allow-Headers: *\r\n" +
                            "Access-Control-Allow-Private-Network: true\r\n" +
                            "Cache-Control: no-store\r\n" +
                            "Content-Type: " + contentType + "\r\n" +
                            "Content-Length: " + data.Length + disposition + "\r\nConnection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (data.Length > 0) stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        private static string ContentType(string filePath)
        {
            string name = filePath.ToLowerInvariant();
            if (name.EndsWith(".json")) return "application/json";
            if (name.EndsWith(".tlog.gz")) return "application/gzip";
            return "application/octet-stream";
        }

        public static void Stop()
        {
            TcpListener listener;
            lock (Sync) listener = _listener;
            Stop(listener);
        }

        private static void Stop(TcpListener listener)
        {
            if (listener == null) return;
            try { listener.Stop(); } catch { }
            lock (Sync)
            {
                if (ReferenceEquals(_listener, listener)) _listener = null;
            }
        }
    }
}
