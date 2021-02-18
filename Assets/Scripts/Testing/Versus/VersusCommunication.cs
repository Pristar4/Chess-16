using System;
using System.IO;
using UnityEngine;

namespace Chess.Testing
{
    public class VersusCommunication : MonoBehaviour
    {
        private const string folderName = "Communication";
        private const string playerFileExtention = ".player";
        private const string managerFileName = "Manager";
        private const string managerExtension = ".json";

        private static string testData;
        private bool communicationAlert;
        private FileSystemEventArgs communicationArgs;

        private FileSystemWatcher communicationWatcher;

        private static string CommunicationPath => Path.Combine(".", folderName);

        private static string ManagerFilePath => Path.Combine(CommunicationPath, managerFileName + managerExtension);

        private void Awake()
        {
            Directory.CreateDirectory(CommunicationPath);
            communicationWatcher = new FileSystemWatcher(Path.GetFullPath(CommunicationPath));
            communicationWatcher.NotifyFilter = NotifyFilters.LastWrite;
            communicationWatcher.Created += CommunicationFileChanged;
            communicationWatcher.Changed += CommunicationFileChanged;
            communicationWatcher.Filter = "*.*";
            communicationWatcher.EnableRaisingEvents = true;
        }

        private void Update()
        {
            if (communicationAlert)
            {
                communicationAlert = false;

                if (Path.GetFileNameWithoutExtension(communicationArgs.Name) ==
                    Path.GetFileNameWithoutExtension(ManagerFilePath)) onManagerUpdated?.Invoke(ReadManagerFile());
                if (Path.GetExtension(communicationArgs.FullPath) == playerFileExtention)
                {
                    var playerInfo = GetPlayerInfo(communicationArgs.FullPath);
                    onPlayerUpdated?.Invoke(playerInfo);
                }
            }
        }

        public event Action<PlayerInfo> onPlayerUpdated;
        public event Action<VersusInfo> onManagerUpdated;

        private void CommunicationFileChanged(object sender, FileSystemEventArgs e)
        {
            // Note that this is called from different thread than Unity main thread, so need to set a flag
            // to pick it up on main thread
            communicationArgs = e;
            communicationAlert = true;
        }

        public static void WriteManagerFile(VersusInfo info)
        {
            Write(JsonUtility.ToJson(info), ManagerFilePath);
        }

        public static VersusInfo ReadManagerFile()
        {
            var s = Read(ManagerFilePath);
            return JsonUtility.FromJson<VersusInfo>(s);
        }

        public static void CreateManagerFile()
        {
            Write("", ManagerFilePath);
        }

        public static bool ManagerFileExists()
        {
            return File.Exists(ManagerFilePath);
        }

        public static string[] GetPlayerFiles()
        {
            return Directory.GetFiles(CommunicationPath, "*" + playerFileExtention);
        }

        public static PlayerInfo GetPlayerInfo(string path)
        {
            var data = Read(path);
            if (string.IsNullOrEmpty(data) || string.IsNullOrWhiteSpace(data)
                ) // Sometimes the result is empty, and reading it again fixes that.
                // Don't have energy to figure out why right now...
                data = Read(path);
            return JsonUtility.FromJson<PlayerInfo>(data);
        }

        public static void WritePlayerInfo(PlayerInfo playerInfo)
        {
            var path = Path.Combine(CommunicationPath,
                "Player" + "_" + playerInfo.playerName + "_" + playerInfo.id + playerFileExtention);
            var data = JsonUtility.ToJson(playerInfo);
            Write(data, path);
        }

        private static void Write(string data, string path)
        {
            var writer = new StreamWriter(path);
            writer.Write(data);
            writer.Close();
        }

        private static string Read(string path)
        {
            var reader = new StreamReader(path);
            var data = reader.ReadToEnd();
            reader.Close();
            return data;
        }
    }

    public struct VersusMatch
    {
        public string whitePlayerName;
        public string blackPlayerName;
    }
}