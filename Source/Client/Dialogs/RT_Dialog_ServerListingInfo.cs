using System.Reflection;
using HarmonyLib;
using TCPNetwork.Packets.ServerBrowser;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs;

public class RT_Dialog_ServerListingInfo(ServerInfo info) : Window
{
    public override Vector2 InitialSize => new Vector2(600f, 250f);

    private readonly ServerInfo ServerInfo = info;

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Vector2 titleSize = Text.CalcSize(ServerInfo._name);
        float centeredx = inRect.width / 2;
        Rect titleRect = new Rect(centeredx - titleSize.x / 2, inRect.y, titleSize.x, titleSize.y);
        Widgets.Label(titleRect, ServerInfo._name);

        Widgets.DrawLineHorizontal(0, titleSize.y + 3f, inRect.width);

        Text.Font = GameFont.Small;
        Rect descriptionRect = new Rect(inRect.x, titleSize.y + 6f, inRect.width / 3 * 2, inRect.height - 55f);
        Widgets.Label(descriptionRect, ServerInfo._description);

        Rect connectRect = new Rect(inRect.width - 135f, inRect.height - 55f, 125f, 45f);
        Rect playerCountRect = new Rect(connectRect.x, connectRect.y - 30f, 125f, 45f);

        Widgets.Label(playerCountRect, $"Population: {ServerInfo._currentPlayerCount}/{ServerInfo._maximumPlayerCount}");

        if (Widgets.ButtonText(connectRect, "Connect")) ConnectToServer();
    }

    private void ConnectToServer() 
    {
        ClientNetwork.Ip = ServerInfo._ip;
        ClientNetwork.Port = ServerInfo._port.ToString();
        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
        ClientNetwork _ = new ClientNetwork();
        Close();
    }
}