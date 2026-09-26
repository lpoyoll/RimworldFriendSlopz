#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "DispatchSettings.generated.h"

/** Project settings > Game > Response Dispatch. Realism toggles live here. */
UCLASS(Config = Game, DefaultConfig, meta = (DisplayName = "Response Dispatch"))
class RESPONSEDISPATCH_API UDispatchSettings : public UDeveloperSettings
{
	GENERATED_BODY()

public:
	/** Attendance target for G1 in urban areas (minutes). */
	UPROPERTY(Config, EditAnywhere, Category = "Grading") float G1UrbanTargetMinutes = 15.f;
	/** Attendance target for G1 in rural areas (minutes). */
	UPROPERTY(Config, EditAnywhere, Category = "Grading") float G1RuralTargetMinutes = 20.f;
	UPROPERTY(Config, EditAnywhere, Category = "Grading") float G2TargetMinutes = 60.f;

	/** If true, the AI dispatcher assigns units without player input. Off when a human plays dispatcher. */
	UPROPERTY(Config, EditAnywhere, Category = "AI Dispatcher") bool bAutoDispatch = true;
	/** Player is offered G1/G2 jobs within this distance before AI units are sent (metres). */
	UPROPERTY(Config, EditAnywhere, Category = "AI Dispatcher") float PlayerOfferRadiusMetres = 3000.f;
	/** Seconds the player has to accept an offered job before it goes to an AI unit. */
	UPROPERTY(Config, EditAnywhere, Category = "AI Dispatcher") float PlayerOfferSeconds = 20.f;

	/** Relative path under the project folder to call type JSON files. */
	UPROPERTY(Config, EditAnywhere, Category = "Data") FString CallTypesDirectory = TEXT("Data/CallTypes");
};
