#pragma once

#include "CoreMinimal.h"
#include "GameplayTagContainer.h"
#include "Subsystems/WorldSubsystem.h"
#include "ResponseId.h"
#include "ResponseSaveParticipant.h"
#include "ResponseEventLog.generated.h"

/** One thing that happened. The single source of truth for BWV, debrief/NDM scoring, complaints and court. */
USTRUCT(BlueprintType)
struct RESPONSECORE_API FResponseEvent
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly) FResponseId Id;
	/** Sequence number, strictly increasing: ties in time keep their real order. */
	UPROPERTY(BlueprintReadOnly) int64 Sequence = 0;
	/** UK local game time. */
	UPROPERTY(BlueprintReadOnly) FDateTime At;
	UPROPERTY(BlueprintReadOnly) FGameplayTag Type;
	/** Who did it: call sign, NPC id string, or SYSTEM. */
	UPROPERTY(BlueprintReadOnly) FString Actor;
	UPROPERTY(BlueprintReadOnly) FResponseId Subject;
	UPROPERTY(BlueprintReadOnly) int64 IncidentId = 0;
	UPROPERTY(BlueprintReadOnly) FVector Location = FVector::ZeroVector;
	UPROPERTY(BlueprintReadOnly) FString Text;
	UPROPERTY(BlueprintReadOnly) TMap<FName, FString> Data;
};

USTRUCT(BlueprintType)
struct RESPONSECORE_API FResponseEventQuery
{
	GENERATED_BODY()

	/** Match events whose type is this tag or a child of it. Empty = any. */
	UPROPERTY(BlueprintReadWrite) FGameplayTag Type;
	UPROPERTY(BlueprintReadWrite) int64 IncidentId = 0;
	UPROPERTY(BlueprintReadWrite) FString Actor;
	UPROPERTY(BlueprintReadWrite) FDateTime From;
	UPROPERTY(BlueprintReadWrite) FDateTime To;
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnResponseEvent, const FResponseEvent&, Event);

UCLASS()
class RESPONSECORE_API UResponseEventLog : public UWorldSubsystem, public IResponseSaveParticipant
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;

	/** Record an event at the current game time. Returns the stored event. */
	const FResponseEvent& Record(FGameplayTag Type, const FString& Actor, const FString& Text, int64 IncidentId = 0,
		FResponseId Subject = FResponseId(), FVector Location = FVector::ZeroVector, const TMap<FName, FString>& Data = {});

	UFUNCTION(BlueprintCallable, Category = "Events") TArray<FResponseEvent> Query(const FResponseEventQuery& Q) const;
	int32 Num() const { return Events.Num(); }

	/** JSON array matching schemas/event.schema.json. */
	FString ExportJson(const FResponseEventQuery& Q) const;

	UPROPERTY(BlueprintAssignable) FOnResponseEvent OnEventRecorded;

	/** Time source when no clock exists (tests); otherwise the clock subsystem is used. */
	FDateTime FallbackTime = FDateTime(2026, 10, 26, 22, 0, 0);

	// IResponseSaveParticipant
	virtual FString GetSaveKey() const override { return TEXT("events"); }
	virtual void WriteSave(TSharedRef<FJsonObject> Out) const override;
	virtual void ReadSave(const TSharedRef<FJsonObject>& In) override;

	static TSharedRef<FJsonObject> ToJson(const FResponseEvent& E);
	static bool FromJson(const TSharedRef<FJsonObject>& J, FResponseEvent& Out);

private:
	FDateTime Now() const;
	TArray<FResponseEvent> Events;
	int64 NextSequence = 1;
};
