using System.Linq;
using GameClient.Dialogs;

namespace GameClient.Managers;

public static class ConnectionManager
{
    public static void ShowWelcomeDialogs() 
    {
        RT_Dialog_Base.PushNewDialog(new RT_Dialog_YesNo("Choose a login method:",
            delegate { RT_Dialog_Base.PushNewDialog(new RT_Dialog_ServerListing()); },
            ShowConnectDialogs,
            "Server Browser",
            "Login"
        ));
    }

    public static void ShowConnectDialogs()
    {
        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Inputs("Connection Details", ["IP", "Port"], [false, false],
            ParseConnectionDetails));
    }

    public static void ParseConnectionDetails()
    {
        bool isInvalid = false;

        if (string.IsNullOrWhiteSpace(RT_Dialog_Inputs.DialogInputResults[0])) isInvalid = true;
        if (string.IsNullOrWhiteSpace(RT_Dialog_Inputs.DialogInputResults[1])) isInvalid = true;
        if (RT_Dialog_Inputs.DialogInputResults[1].Count() > 5) isInvalid = true;
        if (!RT_Dialog_Inputs.DialogInputResults[1].All(char.IsDigit)) isInvalid = true;

        if (isInvalid) RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR",
            ["Server details are invalid! Please try again!"]));
        else
        {
            ClientNetwork.Ip = RT_Dialog_Inputs.DialogInputResults[0];
            ClientNetwork.Port = RT_Dialog_Inputs.DialogInputResults[1];

            RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
            ClientNetwork _ = new ClientNetwork();
        }
    }
}