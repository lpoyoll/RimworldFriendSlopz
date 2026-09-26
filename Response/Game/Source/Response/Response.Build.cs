using UnrealBuildTool;

public class Response : ModuleRules
{
	public Response(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		PublicDependencyModuleNames.AddRange(new[] { "Core", "CoreUObject", "Engine", "InputCore", "EnhancedInput", "ResponseCore", "ResponseDispatch" });
	}
}
