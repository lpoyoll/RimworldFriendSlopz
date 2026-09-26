using UnrealBuildTool;

public class ResponseCore : ModuleRules
{
	public ResponseCore(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		PublicDependencyModuleNames.AddRange(new[] { "Core", "CoreUObject", "Engine", "GameplayTags", "DeveloperSettings", "Json", "JsonUtilities" });
	}
}
