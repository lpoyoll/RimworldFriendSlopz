#pragma once

#include "CoreMinimal.h"
#include "Subsystems/WorldSubsystem.h"
#include "ResponseSaveParticipant.h"
#include "ResponseClockSubsystem.generated.h"

UENUM(BlueprintType)
enum class EShiftName : uint8
{
	Earlies, Lates, Nights
};

/** A response shift. Times are local; a shift may run past midnight. */
USTRUCT(BlueprintType)
struct RESPONSECORE_API FShiftDefinition
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadOnly) EShiftName Name = EShiftName::Earlies;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 StartHour = 7;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 LengthHours = 10;
	/** Briefing length at the start of the shift (minutes). */
	UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 BriefingMinutes = 30;
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnClockHour, int32, LocalHour);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnClockDaylight, bool, bIsDaylight);

/**
 * The single game clock. Game time is UK local civil time (GMT/BST handled). Everything that timestamps
 * (dispatch, event log, PACE clock, BWV) reads time from here.
 */
UCLASS()
class RESPONSECORE_API UResponseClockSubsystem : public UTickableWorldSubsystem, public IResponseSaveParticipant
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Tick(float DeltaTime) override;
	virtual TStatId GetStatId() const override { RETURN_QUICK_DECLARE_CYCLE_STAT(UResponseClockSubsystem, STATGROUP_Tickables); }

	UFUNCTION(BlueprintPure, Category = "Clock") FDateTime GetLocalTime() const { return LocalTime; }
	UFUNCTION(BlueprintPure, Category = "Clock") FDateTime GetUtcTime() const;
	UFUNCTION(BlueprintCallable, Category = "Clock") void SetLocalTime(FDateTime NewLocal);
	/** Advance the clock directly (tests, skipping to the next shift). Fires hour/daylight events as it goes. */
	UFUNCTION(BlueprintCallable, Category = "Clock") void Advance(FTimespan Delta);

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Clock") float TimeScale = 1.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Clock") bool bPaused = false;

	UFUNCTION(BlueprintPure, Category = "Clock") bool IsDaylight() const;
	UFUNCTION(BlueprintPure, Category = "Clock") void GetSunPosition(double& Elevation, double& Azimuth) const;
	UFUNCTION(BlueprintPure, Category = "Clock") bool IsWeekendDemand() const;

	/** Shift that contains the given local time (latest-starting shift wins where shifts overlap). */
	UFUNCTION(BlueprintPure, Category = "Clock") bool GetShiftAt(FDateTime Local, FShiftDefinition& OutShift, FDateTime& OutStart) const;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Clock") TArray<FShiftDefinition> Shifts;

	UPROPERTY(BlueprintAssignable) FOnClockHour OnHourChanged;
	UPROPERTY(BlueprintAssignable) FOnClockDaylight OnDaylightChanged;

	// IResponseSaveParticipant
	virtual FString GetSaveKey() const override { return TEXT("clock"); }
	virtual void WriteSave(TSharedRef<FJsonObject> Out) const override;
	virtual void ReadSave(const TSharedRef<FJsonObject>& In) override;

private:
	FDateTime LocalTime = FDateTime(2026, 10, 26, 22, 0, 0);
	bool bWasDaylight = false;
};
