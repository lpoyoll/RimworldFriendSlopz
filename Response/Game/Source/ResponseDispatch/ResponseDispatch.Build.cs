using UnrealBuildTool;

public class ResponseDispatch : ModuleRules
{
	public ResponseDispatch(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		PublicDependencyModuleNames.AddRange(new[] { "Core", "CoreUObject", "Engine", "GameplayTags", "DeveloperSettings", "ResponseCore" });
		PrivateDependencyModuleNames.AddRange(new[] { "Json", "JsonUtilities" });
	}
}
