using System;
using System.Collections.Generic;
using System.IO;

namespace Shared.Files.Configs.Mods;

public class ModsConfigFile : BaseFile
{
    public static string SavePath { get; set; } = string.Empty;

    public enum ModType { Required, Optional, Forbidden };

    public bool IsEnforced { get; set; } = false;

    public List<ModConfig> ModConfigs { get; set; } = new List<ModConfig>();

    public override void Save()
    {
        try { Serializer.SerializeToFile(SavePath, this); }
        catch (Exception e) { throw new Exception(e.ToString()); }
    }

    public static object Load<T>()
    {
        if (File.Exists(SavePath)) return Serializer.SerializeFromFile<T>(SavePath);
        else
        {
            ModsConfigFile file = new ModsConfigFile();
            Serializer.SerializeToFile(SavePath, file);
            return file;
        }
    }
}