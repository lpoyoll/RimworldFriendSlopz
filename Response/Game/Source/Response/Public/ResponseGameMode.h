#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "ResponseGameMode.generated.h"

/** Shift game mode. Owns shift start/end; the systems themselves live in subsystems. */
UCLASS()
class RESPONSE_API AResponseGameMode : public AGameModeBase
{
	GENERATED_BODY()
};
