#pragma once

#include "CoreMinimal.h"
#include "Subsystems/WorldSubsystem.h"
#include "DispatchTypes.h"
#include "ResponseSaveParticipant.h"
#include "DispatchSubsystem.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnIncidentChanged, int64, IncidentId);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnJobOffered, int64, IncidentId, FName, CallSign);

/**
 * Control room model: the incident log, grading, the job queue and the AI dispatcher.
 * All mutations go through here so every change is written to the incident log.
 * The AI dispatcher is only a policy on top of the public API, so a human dispatcher can replace it later.
 */
UCLASS()
class RESPONSEDISPATCH_API UDispatchSubsystem : public UTickableWorldSubsystem, public IResponseSaveParticipant
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Tick(float DeltaTime) override;
	virtual TStatId GetStatId() const override { RETURN_QUICK_DECLARE_CYCLE_STAT(UDispatchSubsystem, STATGROUP_Tickables); }

	// --- Data
	/** Parse a call_type.schema.json document. Returns the number of call types loaded. */
	int32 LoadCallTypesFromJson(const FString& JsonText);
	int32 LoadCallTypesFromDirectory(const FString& AbsoluteDir);
	const FCallTypeDefinition* FindCallType(FName Id) const { return CallTypes.Find(Id); }
	const TMap<FName, FCallTypeDefinition>& GetCallTypes() const { return CallTypes; }

	// --- Clock: UResponseClockSubsystem when present; the local fallback exists for headless tests.
	UFUNCTION(BlueprintCallable, Category = "Dispatch") void SetGameTime(FDateTime NewTime) { GameTime = NewTime; }
	UFUNCTION(BlueprintPure, Category = "Dispatch") FDateTime GetGameTime() const { return GameTime; }
	/** Game seconds per real second, used only when there is no clock subsystem. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dispatch") float TimeScale = 1.f;

	// IResponseSaveParticipant
	virtual FString GetSaveKey() const override { return TEXT("dispatch"); }
	virtual void WriteSave(TSharedRef<FJsonObject> Out) const override;
	virtual void ReadSave(const TSharedRef<FJsonObject>& In) override;

	// --- Incidents
	UFUNCTION(BlueprintCallable, Category = "Dispatch")
	int64 CreateIncident(FName CallType, const TArray<FName>& DetailFlags, FVector Location, const FString& LocationDescription,
		EIncidentSource Source = EIncidentSource::Emergency999, bool bRural = false);

	UFUNCTION(BlueprintCallable, Category = "Dispatch")
	bool Regrade(int64 IncidentId, EIncidentGrade NewGrade, const FString& By, const FString& Reason);

	UFUNCTION(BlueprintCallable, Category = "Dispatch")
	bool AddLog(int64 IncidentId, const FString& Author, EIncidentLogKind Kind, const FString& Text);

	UFUNCTION(BlueprintCallable, Category = "Dispatch")
	bool SetIncidentStatus(int64 IncidentId, EIncidentStatus NewStatus, const FString& By);

	UFUNCTION(BlueprintCallable, Category = "Dispatch")
	bool CloseIncident(int64 IncidentId, FName ClosingCode, const FString& By);

	const FIncident* FindIncident(int64 IncidentId) const { return Incidents.Find(IncidentId); }

	/** Open incidents that still need units, highest priority first. */
	UFUNCTION(BlueprintCallable, Category = "Dispatch") TArray<FIncident> GetQueue() const;

	// --- Units
	UFUNCTION(BlueprintCallable, Category = "Dispatch") void RegisterUnit(const FDispatchUnit& Unit);
	UFUNCTION(BlueprintCallable, Category = "Dispatch") void UpdateUnitLocation(FName CallSign, FVector Location);
	UFUNCTION(BlueprintCallable, Category = "Dispatch") bool SetUnitStatus(FName CallSign, EUnitStatus NewStatus);
	UFUNCTION(BlueprintCallable, Category = "Dispatch") bool AssignUnit(int64 IncidentId, FName CallSign, const FString& By);
	/** Player (or any unit) volunteers for a job. Logged as self-deployed. */
	UFUNCTION(BlueprintCallable, Category = "Dispatch") bool SelfDeploy(int64 IncidentId, FName CallSign);
	/** Pull a unit off its current job to a higher-priority one. Logged on both incidents. */
	UFUNCTION(BlueprintCallable, Category = "Dispatch") bool Divert(FName CallSign, int64 ToIncidentId, const FString& By);
	UFUNCTION(BlueprintCallable, Category = "Dispatch") bool DeclineOffer(int64 IncidentId, FName CallSign);

	UPROPERTY(BlueprintAssignable) FOnIncidentChanged OnIncidentCreated;
	UPROPERTY(BlueprintAssignable) FOnIncidentChanged OnIncidentUpdated;
	UPROPERTY(BlueprintAssignable) FOnIncidentChanged OnTargetBreached;
	/** The AI dispatcher offers a job to the player over the radio. */
	UPROPERTY(BlueprintAssignable) FOnJobOffered OnJobOffered;

private:
	FIncident* Get(int64 Id) { return Incidents.Find(Id); }
	FDispatchUnit* GetUnit(FName CallSign) { return Units.Find(CallSign); }
	void Log(FIncident& Inc, const FString& Author, EIncidentLogKind Kind, const FString& Text);
	void CheckBreaches();
	void RunAIDispatcher();
	const FDispatchUnit* PickUnit(const FIncident& Inc, bool bPlayerOnly) const;

	TMap<FName, FCallTypeDefinition> CallTypes;
	TMap<int64, FIncident> Incidents;
	TMap<FName, FDispatchUnit> Units;

	struct FOffer { FName CallSign; FDateTime ExpiresAt; };
	TMap<int64, FOffer> PendingOffers;
	TSet<int64> DeclinedByPlayer;

	FDateTime GameTime = FDateTime(2026, 10, 26, 22, 0, 0);
	int64 NextIncidentId = 1;
	int32 DailySequence = 0;
	int32 SequenceDay = -1;
};
