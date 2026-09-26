using UnrealBuildTool;

public class ResponseTarget : TargetRules
{
	public ResponseTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Game;
		DefaultBuildSettings = BuildSettingsVersion.Latest;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
		ExtraModuleNames.AddRange(new[] { "Response", "ResponseDispatch" });
	}
}
