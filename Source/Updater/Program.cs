using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Rimjob.Updater
{
    internal static class Program
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/lpoyoll/RimworldFriendSlopz/releases/latest";

        private static int Main(string[] args)
        {
            Console.Title = "Rimjob Updater";
            try
            {
                if (args.Length >= 2 && string.Equals(args[0], "--apply", StringComparison.OrdinalIgnoreCase))
                    return ApplyUpdate(Path.GetFullPath(args[1]));

                string target = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
                if (!LooksLikeRimjobFolder(target))
                {
                    Console.Error.WriteLine("Update.exe must be run from the root of the Rimjob mod folder.");
                    Console.Error.WriteLine("Expected to find About\\About.xml and 1.6\\Assemblies.");
                    Wait();
                    return 2;
                }

                string tempFolder = Path.Combine(Path.GetTempPath(), "RimjobUpdater", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempFolder);
                string tempUpdater = Path.Combine(tempFolder, "Update.exe");
                File.Copy(Process.GetCurrentProcess().MainModule.FileName, tempUpdater, true);

                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = tempUpdater,
                    Arguments = "--apply \"" + target + "\"",
                    WorkingDirectory = tempFolder,
                    UseShellExecute = true
                };

                if (!CanWriteTo(target)) start.Verb = "runas";
                Process.Start(start);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Rimjob updater could not start:");
                Console.Error.WriteLine(ex);
                Wait();
                return 1;
            }
        }

        private static int ApplyUpdate(string target)
        {
            Console.WriteLine("Rimjob Client Updater");
            Console.WriteLine("Target: " + target);
            Console.WriteLine();

            if (!LooksLikeRimjobFolder(target))
                throw new InvalidOperationException("The selected target no longer looks like a Rimjob client installation.");

            Process rimworld = Process.GetProcesses().FirstOrDefault(p =>
                string.Equals(p.ProcessName, "RimWorldWin64", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.ProcessName, "RimWorld", StringComparison.OrdinalIgnoreCase));
            if (rimworld != null)
            {
                Console.Error.WriteLine("RimWorld is currently running. Close RimWorld completely, then run Update.exe again.");
                Wait();
                return 3;
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string work = Path.Combine(Path.GetTempPath(), "RimjobUpdater", "work-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);

            try
            {
                Console.WriteLine("Checking GitHub for the latest Rimjob release...");
                GitHubRelease release = GetLatestRelease();
                if (release == null || string.IsNullOrWhiteSpace(release.TagName))
                    throw new InvalidOperationException("GitHub did not return a valid Rimjob release.");

                GitHubAsset asset = release.Assets == null ? null : release.Assets.FirstOrDefault(a =>
                    a != null &&
                    !string.IsNullOrWhiteSpace(a.Name) &&
                    a.Name.StartsWith("Rimjob-v", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                if (asset == null || string.IsNullOrWhiteSpace(asset.DownloadUrl))
                    throw new InvalidOperationException("The latest release does not contain a Rimjob client ZIP asset.");

                string currentVersion = ReadCurrentVersion(target);
                Console.WriteLine("Installed: " + (string.IsNullOrWhiteSpace(currentVersion) ? "unknown" : currentVersion));
                Console.WriteLine("Latest:    " + release.TagName);
                if (!string.IsNullOrWhiteSpace(currentVersion) &&
                    string.Equals(NormalizeVersion(currentVersion), NormalizeVersion(release.TagName), StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine();
                    Console.WriteLine("Rimjob is already up to date.");
                    Wait();
                    return 0;
                }

                string zipPath = Path.Combine(work, asset.Name);
                Console.WriteLine("Downloading " + asset.Name + "...");
                using (WebClient web = CreateWebClient()) web.DownloadFile(asset.DownloadUrl, zipPath);

                if (!string.IsNullOrWhiteSpace(asset.Digest) && asset.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                {
                    string expected = asset.Digest.Substring("sha256:".Length).Trim();
                    string actual = ComputeSha256(zipPath);
                    if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Downloaded release failed its GitHub SHA256 check.");
                    Console.WriteLine("SHA256 verified.");
                }

                string extract = Path.Combine(work, "extract");
                Directory.CreateDirectory(extract);
                SafeExtract(zipPath, extract);

                string source = Path.Combine(extract, "Rimjob");
                if (!LooksLikeRimjobFolder(source))
                    throw new InvalidDataException("The downloaded release ZIP does not contain a valid top-level Rimjob client folder.");
                if (!File.Exists(Path.Combine(source, "1.6", "Assemblies", "RTClient.dll")))
                    throw new InvalidDataException("The downloaded release is missing RTClient.dll.");

                string parent = Directory.GetParent(target).FullName;
                string staging = Path.Combine(parent, Path.GetFileName(target) + ".update-new");
                string backup = Path.Combine(parent, Path.GetFileName(target) + ".update-backup");
                DeleteDirectoryIfExists(staging);
                DeleteDirectoryIfExists(backup);

                Console.WriteLine("Staging new client files...");
                CopyDirectory(source, staging);

                bool oldMoved = false;
                try
                {
                    Directory.Move(target, backup);
                    oldMoved = true;
                    Directory.Move(staging, target);
                    DeleteDirectoryIfExists(backup);
                }
                catch
                {
                    if (Directory.Exists(target)) DeleteDirectoryIfExists(target);
                    if (oldMoved && Directory.Exists(backup)) Directory.Move(backup, target);
                    throw;
                }

                Console.WriteLine();
                Console.WriteLine("Rimjob client updated successfully to " + release.TagName + ".");
                Console.WriteLine("All client-side mod files were replaced from the published release ZIP.");
                Wait();
                return 0;
            }
            catch (WebException ex)
            {
                Console.Error.WriteLine("Update download failed: " + ex.Message);
                Console.Error.WriteLine("Check your internet connection and the GitHub Releases page, then retry.");
                Wait();
                return 4;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Update failed:");
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Your previous Rimjob folder is restored automatically if replacement had already begun.");
                Wait();
                return 5;
            }
            finally
            {
                try { DeleteDirectoryIfExists(work); } catch { }
            }
        }

        private static GitHubRelease GetLatestRelease()
        {
            using (WebClient web = CreateWebClient())
            {
                byte[] bytes = web.DownloadData(LatestReleaseApi);
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                    return serializer.ReadObject(stream) as GitHubRelease;
                }
            }
        }

        private static WebClient CreateWebClient()
        {
            WebClient web = new WebClient();
            web.Headers[HttpRequestHeader.UserAgent] = "Rimjob-Updater/1.0";
            web.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
            return web;
        }

        private static bool LooksLikeRimjobFolder(string path)
        {
            return Directory.Exists(path) &&
                   File.Exists(Path.Combine(path, "About", "About.xml")) &&
                   Directory.Exists(Path.Combine(path, "1.6", "Assemblies"));
        }

        private static bool CanWriteTo(string directory)
        {
            string probe = Path.Combine(directory, ".rimjob-update-write-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllText(probe, "test");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        private static string ReadCurrentVersion(string target)
        {
            string path = Path.Combine(target, "VERSION.txt");
            try { return File.Exists(path) ? File.ReadAllText(path).Trim() : null; }
            catch { return null; }
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return string.Empty;
            version = version.Trim();
            return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version.Substring(1) : version;
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static void SafeExtract(string zipPath, string destination)
        {
            string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string output = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                    if (!output.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Unsafe path found in downloaded ZIP: " + entry.FullName);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(output);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    entry.ExtractToFile(output, true);
                }
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relative = directory.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string output = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.Copy(file, output, true);
            }
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        private static void Wait()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to close...");
            try { Console.ReadKey(true); } catch { }
        }
    }

    [DataContract]
    internal sealed class GitHubRelease
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        [DataMember(Name = "assets")]
        public List<GitHubAsset> Assets { get; set; }
    }

    [DataContract]
    internal sealed class GitHubAsset
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string DownloadUrl { get; set; }

        [DataMember(Name = "digest")]
        public string Digest { get; set; }
    }
}
