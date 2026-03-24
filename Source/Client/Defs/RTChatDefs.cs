using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace GameClient.Defs
{
    [StaticConstructorOnStartup]
    public static class RTChatDefs
    {
        public static Texture2D Chat = ContentFinder<Texture2D>.Get("UI/Chat");

        public static Texture2D Options = ContentFinder<Texture2D>.Get("UI/Options");
    }

    [DefOf]
    public static class RTChatDefSounds
    {
        public static SoundDef ChatSend;

        public static SoundDef ChatReceive;
    }
}
