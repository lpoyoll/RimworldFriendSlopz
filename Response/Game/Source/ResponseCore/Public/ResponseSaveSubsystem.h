#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "ResponseSaveSubsystem.generated.h"

class IResponseSaveParticipant;

/**
 * JSON save/load. Systems register themselves; a save is one file:
 *   { "format": "response-save", "version": 1, "world_seed": ..., "saved_at_utc": ..., "systems": { key: {...} } }
 * Stored in Saved/SaveGames/<slot>.json.
 */
UCLASS()
class RESPONSECORE_API UResponseSaveSubsystem : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	static constexpr int32 FormatVersion = 1;

	void RegisterParticipant(UObject* Participant);
	void UnregisterParticipant(UObject* Participant);

	UFUNCTION(BlueprintCallable, Category = "Save") bool SaveToSlot(const FString& Slot);
	UFUNCTION(BlueprintCallable, Category = "Save") bool LoadFromSlot(const FString& Slot);

	/** Build/apply the save document without touching disk (used by tests and by the slot functions). */
	FString SaveToString() const;
	bool LoadFromString(const FString& Json);

	static FString SlotPath(const FString& Slot);

private:
	TArray<TWeakObjectPtr<UObject>> Participants;
};
