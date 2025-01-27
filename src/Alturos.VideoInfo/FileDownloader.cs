﻿using Alturos.VideoInfo.Model;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Alturos.VideoInfo
{
    public class FileDownloader
    {
        public string GetFfmpegPackageUrl()
        {
            var win64 = "https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.1.3-win64-static.zip";
            var win32 = "https://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-4.1.3-win32-static.zip";

#if NETSTANDARD2_0
            var macos64 = "https://ffmpeg.zeranoe.com/builds/macos64/static/ffmpeg-4.1.3-macos64-static.zip";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return macos64;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    return win64;
                }

                return win32;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                throw new NotSupportedException("Linux binary download is not supported");
            }
#endif

#if NET461
            if (Environment.Is64BitOperatingSystem)
            {
                return win64;
            }

            return win32;
#endif

            throw new NotSupportedException("Binary download is not supported");
        }

        public async Task<FfmpegDownloadResult> DownloadAsync(string url, string destinationPath)
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            var filePath = Path.Combine(destinationPath, fileName);

            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMinutes(10);

                using (var httpResponseMessage = await httpClient.GetAsync(url).ConfigureAwait(false))
                {
                    if (!httpResponseMessage.IsSuccessStatusCode)
                    {
                        return new FfmpegDownloadResult
                        {
                            Successful = false
                        };
                    }

                    var fileContentStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    using (var sourceStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        fileContentStream.Seek(0, SeekOrigin.Begin);
                        await fileContentStream.CopyToAsync(sourceStream);
                    }
                }
            }

            var folderName = Path.GetFileNameWithoutExtension(fileName);
            var folderPath = Path.Combine(destinationPath, folderName);

            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }

            ZipFile.ExtractToDirectory(filePath, folderPath);
            File.Delete(filePath);

            var basePath = Path.Combine(destinationPath, folderName, folderName, "bin");
            return new FfmpegDownloadResult
            {
                Successful = true,
                FfmpegPath = Path.Combine(basePath, "ffmpeg.exe"),
                FfprobePath = Path.Combine(basePath, "ffprobe.exe"),
                FfplayPath = Path.Combine(basePath, "ffplay.exe")
            };
        }
    }
}
