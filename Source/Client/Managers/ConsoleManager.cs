using GameClient.Dialogs;
using GameClient.Misc;
using TCPNetwork.Packets;
using RimWorld;
using Shared;
using static Shared.CommonEnumerators;

namespace GameClient.Managers;

//Class that handles how the client will answer to incoming server commands
public static class ConsoleManager
{
    [HandlesPacket(PacketHeader.ConsoleManager)]
    private static void ParsePacket(byte[] bytes)
    {
        CommandData data = Serializer.ConvertBytesToObject<CommandData>(bytes);

        switch (data._commandMode)
        {
            case CommandMode.Op:
                OnOpCommand();
                break;

            case CommandMode.Deop:
                OnDeopCommand();
                break;

            case CommandMode.Broadcast:
                OnBroadcastCommand(data);
                break;

            case CommandMode.ForceSave:
                OnForceSaveCommand();
                break;
            
            default:
                Printer.Error($"Received invalid step mode {data._commandMode}");
                return;
        }
    }

    private static void OnOpCommand()
    {
        SessionHandler.IsAdmin = true;
        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", ["You are now an admin!"]));
    }

    private static void OnDeopCommand()
    {
        SessionHandler.IsAdmin = false;
        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", ["You are no longer an admin!"]));
    }

    private static void OnBroadcastCommand(CommandData commandData)
    {
        RimworldManager.GenerateLetter("Server Broadcast", ChatManagerH.ParseMessage(commandData._details, true), LetterDefOf.PositiveEvent);
    }

    private static void OnForceSaveCommand() { SaveManager.ForceSave(); }
}