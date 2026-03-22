using Shared;
using Shared.Files.Synchronous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPNetwork.Packets
{
    public class PKT_Synchronous : PKT_Base
    {
        public enum Type { Visit, Raid }

        public enum StepMode { Ask, Accept, Reject, Start, Action }

        public StepMode CurrentStepMode { get; set; } = StepMode.Ask;

        public Type CurrentType { get; set; } = Type.Visit;

        public ActionType CurrentActionType { get; set; } = ActionType.SPlayerDraft;

        public int FromTile { get; set; } = -1;

        public int ToTile { get; set; } = -1;

        public string Username { get; set; } = string.Empty;

        public byte[] Contents { get; set; } = null;

        public PartyFile Party { get; set; } = null;

        public enum ActionType
        {
            SPlayerDraft,
            SPlayerWeather,
            SPlayerMentalState,
            SPlayerGameSpeed,
            SPlayerJob,
            SPlayerHediff,
            SPlayerDestroy
        }

        public T GetPacketData<T>(object obj = null) { return Serializer.ConvertBytesToObject<T>(Contents); }
    }
}
