using GameClient.Core.Preferences;
using GameClient.Dialogs;
using Shared;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{
    //Class that handles loging responses from the server

    public static class LoginManager
    {
        //Parses the received packet into an order

        [HandlesPacket(PacketHeader.LoginManager)]
        private static void ParsePacket(byte[] bytes)
        {
            LoginData loginData = Serializer.ConvertBytesToObject<LoginData>(bytes);

            switch (loginData._tryResponse)
            {
                case LoginResponse.InvalidLogin:
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Login details are invalid! Please try again!" }));
                    break;

                case LoginResponse.BannedLogin:
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You are banned from this server!" }));
                    break;

                case LoginResponse.RegisterError:
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "There was an error registering! Please try again!" }));
                    break;

                case LoginResponse.ExtraLogin:
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You connected from another place!" }));
                    break;

                case LoginResponse.WrongMods:
                    ModManagerH.GetConflictingMods(bytes);
                    break;

                case LoginResponse.ServerFull:
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Server is full!" }));
                    break;

                case LoginResponse.Whitelist:
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Server is whitelisted!" }));
                    break;

                case LoginResponse.WrongVersion:
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { $"Mod version mismatch! Expected version '{loginData._extraDetails[0]}'" }));
                    break;

                case LoginResponse.NoWorld:
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { $"Server is currently being set up! Join again later!" }));
                    break;
            }
        }
    }
}
