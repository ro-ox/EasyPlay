using System.IO;
using System.Net.Http;
using System.Diagnostics;
using EasyPlay.Models;

namespace EasyPlay.Services
{
    public class DownloadService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromHours(2) };

        public async Task<bool> DownloadFileAsync(
            string url,
            string savePath,
            DownloadTask downloadTask,
            CancellationToken cancellationToken)
        {
            try
            {
                downloadTask.Status = "در حال اتصال...";

                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                downloadTask.TotalBytes = totalBytes;

                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                using var stream = await response.Content.ReadAsStreamAsync();

                var buffer = new byte[8192];
                long totalRead = 0;
                var stopwatch = Stopwatch.StartNew();
                var lastUpdate = DateTime.Now;
                var lastBytes = 0L;

                downloadTask.Status = "در حال دانلود...";

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (read == 0) break;

                    await fileStream.WriteAsync(buffer, 0, read, cancellationToken);
                    totalRead += read;
                    downloadTask.DownloadedBytes = totalRead;

                    if (totalBytes > 0)
                    {
                        downloadTask.Progress = (double)totalRead / totalBytes * 100;
                    }

                    // Update speed every 500ms
                    var now = DateTime.Now;
                    if ((now - lastUpdate).TotalMilliseconds >= 500)
                    {
                        var bytesPerSecond = (totalRead - lastBytes) / (now - lastUpdate).TotalSeconds;
                        downloadTask.DownloadSpeed = bytesPerSecond;
                        lastUpdate = now;
                        lastBytes = totalRead;
                    }
                }

                stopwatch.Stop();
                downloadTask.Progress = 100;
                downloadTask.Status = "دانلود تکمیل شد";
                downloadTask.IsCompleted = true;
                downloadTask.DownloadSpeed = 0;

                return true;
            }
            catch (OperationCanceledException)
            {
                downloadTask.Status = "لغو شد";
                downloadTask.IsCancelled = true;

                // Delete partial file
                try
                {
                    if (File.Exists(savePath))
                        File.Delete(savePath);
                }
                catch { }

                return false;
            }
            catch (Exception ex)
            {
                downloadTask.Status = "خطا در دانلود";
                downloadTask.ErrorMessage = ex.Message;

                // Delete partial file
                try
                {
                    if (File.Exists(savePath))
                        File.Delete(savePath);
                }
                catch { }

                return false;
            }
        }

        public static async Task<long> GetFileSizeAsync(string url)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await _httpClient.SendAsync(request);
                return response.Content.Headers.ContentLength ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public static string GenerateFileName(string url, string? customName = null)
        {
            if (!string.IsNullOrEmpty(customName))
                return customName;

            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);

                if (string.IsNullOrEmpty(fileName) || fileName == "/")
                    fileName = $"video_{DateTime.Now:yyyyMMddHHmmss}.mp4";

                return fileName;
            }
            catch
            {
                return $"video_{DateTime.Now:yyyyMMddHHmmss}.mp4";
            }
        }
    }
}
