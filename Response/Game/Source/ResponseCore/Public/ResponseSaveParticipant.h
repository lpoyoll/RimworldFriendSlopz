#pragma once

#include "CoreMinimal.h"
#include "UObject/Interface.h"
#include "ResponseSaveParticipant.generated.h"

class FJsonObject;

UINTERFACE(MinimalAPI)
class UResponseSaveParticipant : public UInterface
{
	GENERATED_BODY()
};

/**
 * Anything with persistent state implements this and registers with UResponseSaveSubsystem.
 * Saves are JSON so they stay readable, diffable and moddable.
 */
class RESPONSECORE_API IResponseSaveParticipant
{
	GENERATED_BODY()

public:
	/** Unique key for this system's block in the save file, e.g. "clock", "dispatch", "events". */
	virtual FString GetSaveKey() const = 0;
	virtual void WriteSave(TSharedRef<FJsonObject> Out) const = 0;
	virtual void ReadSave(const TSharedRef<FJsonObject>& In) = 0;
};
