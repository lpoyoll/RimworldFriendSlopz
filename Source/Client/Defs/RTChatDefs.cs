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
        public static Texture2D ChatOn = ContentFinder<Texture2D>.Get("UI/ChatOn");

        public static Texture2D ChatOff = ContentFinder<Texture2D>.Get("UI/ChatOff");
    }

    [DefOf]
    public static class RTChatDefSounds
    {
        public static SoundDef ChatSend;

        public static SoundDef ChatReceive;
    }
}
