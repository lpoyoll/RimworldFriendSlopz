using Shared.Details.Planet;
using System;
using System.IO;

namespace Shared.Files.Configs;

public class PlanetConfigFile : BaseFile
{
    public static string SavePath { get; set; } = string.Empty;

    public int PersistentRandomValue { get; set; } = -1;

    public string SeedString { get; set; } = string.Empty;

    public float PlanetCoverage { get; set; } = -1f;

    public int Rainfall { get; set; } = -1;

    public int Temperature { get; set; } = -1;

    public int Population { get; set; } = -1;

    public int LandmarkDensity { get; set; } = -1;

    public float Pollution { get; set; } = -1f;

    public FeatureDetail[] Features { get; set; } = null;

    public RoadDetail[] Roads { get; set; } = null;

    public PollutionDetail[] PollutedTiles { get; set; } = null;

    public NPCFactionDetail[] NPCFactions { get; set; } = null;

    public NPCSettlementDetail[] NPCSettlements { get; set; } = null;

    public override void Save()
    {
        try { Serializer.ObjectBytesToFile(SavePath, this); }
        catch (Exception e) { throw new Exception(e.ToString()); }
    }

    public static object Load<T>()
    {
        if (File.Exists(SavePath)) return Serializer.FileBytesToObject<T>(SavePath);
        else return null;
    }
}