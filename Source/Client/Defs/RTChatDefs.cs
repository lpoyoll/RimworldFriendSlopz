using RimWorld;
using UnityEngine;
using Verse;
// ReSharper disable UnassignedField.Global

namespace GameClient.Defs;

[StaticConstructorOnStartup]
public static class RTChatDefs
{
    public static readonly Texture2D ChatOn = ContentFinder<Texture2D>.Get("UI/ChatOn");

    public static readonly Texture2D ChatOff = ContentFinder<Texture2D>.Get("UI/ChatOff");
}

[DefOf]
public static class RTChatDefSounds
{
    public static SoundDef ChatSend;

    public static SoundDef ChatReceive;
}