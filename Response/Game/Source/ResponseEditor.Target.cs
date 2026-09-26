using UnrealBuildTool;

public class ResponseEditorTarget : TargetRules
{
	public ResponseEditorTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Editor;
		DefaultBuildSettings = BuildSettingsVersion.Latest;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
		ExtraModuleNames.AddRange(new[] { "Response", "ResponseDispatch" });
	}
}
