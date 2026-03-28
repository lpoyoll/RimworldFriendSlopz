using UnityEngine;
using Verse;

namespace GameClient.Defs
{
    [StaticConstructorOnStartup]
    public static class RTTextureDefs
    {
        public static Texture2D ChatOn = ContentFinder<Texture2D>.Get("UI/ChatON");

        public static Texture2D ChatOff = ContentFinder<Texture2D>.Get("UI/ChatOFF");

        public static Texture2D OptionsOn = ContentFinder<Texture2D>.Get("UI/OptionsON");

        public static Texture2D OptionsOff = ContentFinder<Texture2D>.Get("UI/OptionsOFF");

        public static Texture2D AdminOn = ContentFinder<Texture2D>.Get("UI/AdminON");

        public static Texture2D AdminOff = ContentFinder<Texture2D>.Get("UI/AdminOFF");

        public static Texture2D PinOn = ContentFinder<Texture2D>.Get("UI/PinON");

        public static Texture2D PinOff = ContentFinder<Texture2D>.Get("UI/PinOFF");

        public static Texture2D SoundOn = ContentFinder<Texture2D>.Get("UI/SoundON");

        public static Texture2D SoundOff = ContentFinder<Texture2D>.Get("UI/SoundOFF");
    }
}
