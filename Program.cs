using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PlaylistBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1 || !Directory.Exists(args[0]))
            {
                Console.WriteLine("Please provide a drive/folder for processing.");
                return;
            }

            string startFolderPath = args[0];
            if (!startFolderPath.EndsWith("\\"))
            {
                startFolderPath += "\\";
            }

            IEnumerable<string> existingPlaylists = Directory.EnumerateFiles(startFolderPath, "*.wpl", SearchOption.TopDirectoryOnly);
            foreach (string existingPlaylistPath in existingPlaylists)
            {
                File.Delete(existingPlaylistPath);
            }

            IEnumerable<string> topLevelDirectories = Directory.EnumerateDirectories(startFolderPath, "*", SearchOption.TopDirectoryOnly);
            foreach (string topLevelDirectory in topLevelDirectories)
            {
                ProcessFolder(topLevelDirectory);
            }
        }

        static void ProcessFolder(string topLevelFolder)
        {
            topLevelFolder = topLevelFolder.TrimEnd(new char[] { '\\' });
            string folderName = Path.GetFileName(topLevelFolder);
            string parentPath = Path.GetDirectoryName(topLevelFolder);

            List<string> songs = new List<string>();
            AddFolder(topLevelFolder, songs);

            const int maxCount = 2000;
            int count = 0;

            int fileIndex = 1;
            StreamWriter fileWriter = null;

            while (songs.Count > 0)
            {
                if (count == 0)
                {
                    if (fileWriter != null)
                    {
                        fileWriter.WriteLine("        </seq>");
                        fileWriter.WriteLine("    </body>");
                        fileWriter.WriteLine("</smil>");
                        fileWriter.Close();
                    }

                    int itemCount = Math.Min(maxCount, songs.Count);
                    string playlistName = string.Format("{0}_{1}", folderName, fileIndex++);
                    fileWriter = new StreamWriter(string.Format("{0}.wpl", Path.Combine(parentPath, playlistName)));

                    fileWriter.WriteLine("<?wpl version=\"1.0\"?>");
                    fileWriter.WriteLine("<smil>");
                    fileWriter.WriteLine("    <head>");
                    fileWriter.WriteLine("        <meta name=\"Generator\" content=\"Microsoft Windows Media Player -- 12.0.10586.162\"/>");
                    fileWriter.WriteLine(string.Format("        <meta name=\"ItemCount\" content=\"{0}\"/>", itemCount));
                    fileWriter.WriteLine(string.Format("        <title>{0}</title>", playlistName));
                    fileWriter.WriteLine("    </head>");
                    fileWriter.WriteLine("    <body>");
                    fileWriter.WriteLine("        <seq>");
                }

                string songPath = songs[0];
                songs.RemoveAt(0);

                Uri pathUri = new Uri(songPath);
                Uri folderUri = new Uri(topLevelFolder);
                string relativeSongPath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
                relativeSongPath = HttpUtility.HtmlEncode(relativeSongPath);

                if (fileWriter != null)
                {
                    fileWriter.WriteLine(string.Format("            <media src=\"{0}\"/>", relativeSongPath));
                }

                count++;

                if (count == maxCount)
                {
                    count = 0;
                }
            }

            if (fileWriter != null)
            {
                fileWriter.WriteLine("        </seq>");
                fileWriter.WriteLine("    </body>");
                fileWriter.WriteLine("</smil>");
                fileWriter.Close();
            }
        }

        static void AddFolder(string folder, List<string> fullList)
        {
            List<Entry> entries = new List<Entry>();

            IEnumerable<string> files = Directory.EnumerateFiles(
                folder,
                "*.mp3",
                SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                entries.Add(new Entry(file, false));
            }

            IEnumerable<string> directories = Directory.EnumerateDirectories(
                folder,
                "*",
                SearchOption.TopDirectoryOnly);

            foreach (string directory in directories)
            {
                entries.Add(new Entry(directory, true));
            }

            entries.Sort(new EntryComparer());

            foreach (Entry entry in entries)
            {
                if (entry.IsDirectory)
                {
                    AddFolder(entry.Path, fullList);
                }
                else
                {
                    fullList.Add(entry.Path);
                }
            }
        }

        private class Entry
        {
            public DateTime CreationTime;
            public string Path;
            public bool IsDirectory;

            public Entry(string path, bool isDirectory)
            {
                this.Path = path;
                this.IsDirectory = isDirectory;
                this.CreationTime = isDirectory ? Directory.GetCreationTimeUtc(path) : File.GetCreationTimeUtc(path);
            }
        }

        private class EntryComparer : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y)
            {
                // If both are null, or both are same instance, return true.
                if (object.ReferenceEquals(x, y))
                {
                    return 0;
                }

                // -1 means x is higher, 1 means y is higher
                // this will ensure chronological order
                if (x == null)
                {
                    return 1;
                }
                else if (y == null)
                {
                    return -1;
                }

                if (x.CreationTime == y.CreationTime)
                {
                    if (x.IsDirectory)
                    {
                        if (y.IsDirectory)
                        {
                            string xName = Path.GetFileName(x.Path);
                            string yName = Path.GetFileName(y.Path);
                            return string.Compare(xName, yName, StringComparison.InvariantCultureIgnoreCase);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        if (y.IsDirectory)
                        {
                            return 1;
                        }
                        else
                        {
                            string xName = Path.GetFileName(x.Path);
                            string yName = Path.GetFileName(y.Path);
                            return string.Compare(xName, yName, StringComparison.InvariantCultureIgnoreCase);
                        }
                    }
                }
                else if (x.CreationTime > y.CreationTime)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}
