using UnrealBuildTool;

public class ResponseDispatch : ModuleRules
{
	public ResponseDispatch(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		PublicDependencyModuleNames.AddRange(new[] { "Core", "CoreUObject", "Engine", "GameplayTags", "DeveloperSettings" });
		PrivateDependencyModuleNames.AddRange(new[] { "Json", "JsonUtilities" });
	}
}
