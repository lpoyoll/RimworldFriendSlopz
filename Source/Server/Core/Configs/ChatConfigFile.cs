using Shared;

namespace GameServer.Core.Configs;

[Serializable]
public class ChatConfigFile
{
    public bool EnableMoTD = false;

    public string MessageOfTheDay = "Remember to drink water";

    public bool LoginNotifications = false;

    public bool DisconnectNotifications = false;

#if SERVER
    private static string FilePath => Path.Combine(Master.ConfigsPath, "ChatConfig.json");

    public static ChatConfigFile Load()
    {
        if (File.Exists(FilePath))
        {
            return Serializer.SerializeFromFile<ChatConfigFile>(FilePath);
        }
        else 
        {
            var obj = new ChatConfigFile();
            Serializer.SerializeToFile(FilePath, obj);
            return obj;
        }
    }

    public static void Save()
    {
        Serializer.SerializeToFile(FilePath, Master.ChatConfig);
    }
#endif

}