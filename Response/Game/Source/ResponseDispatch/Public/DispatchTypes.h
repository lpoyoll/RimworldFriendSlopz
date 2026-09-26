#pragma once

#include "CoreMinimal.h"
#include "Engine/DataTable.h"
#include "GameplayTagContainer.h"
#include "DispatchTypes.generated.h"

/** Call grade. Matches incident.schema.json "grade". */
UENUM(BlueprintType)
enum class EIncidentGrade : uint8
{
	G1 UMETA(DisplayName = "Grade 1 - Immediate"),
	G2 UMETA(DisplayName = "Grade 2 - Priority"),
	G3 UMETA(DisplayName = "Grade 3 - Scheduled"),
	G4 UMETA(DisplayName = "Grade 4 - Resolved without deployment"),
};

UENUM(BlueprintType)
enum class EIncidentStatus : uint8
{
	Received, Graded, Queued, Assigned, EnRoute, AtScene, Resolved, Closed, Cancelled
};

UENUM(BlueprintType)
enum class EIncidentSource : uint8
{
	Emergency999, NonEmergency101, Online, SelfGenerated, PartnerAgency, Alarm, ANPR
};

UENUM(BlueprintType)
enum class EIncidentLogKind : uint8
{
	Narrative, Status, Grade, Assignment, Risk, Result, System
};

UENUM(BlueprintType)
enum class ERiskAssessment : uint8
{
	None, DASH, MissingPerson, THRIVE
};

UENUM(BlueprintType)
enum class EUnitStatus : uint8
{
	Available, Committed, EnRoute, AtScene, Custody, Refreshment, OffDuty
};

/** First matching rule sets the grade. */
USTRUCT(BlueprintType)
struct RESPONSEDISPATCH_API FGradingRule
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<FName> IfAny;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) EIncidentGrade Grade = EIncidentGrade::G2;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) FString Reason;
};

USTRUCT(BlueprintType)
struct RESPONSEDISPATCH_API FDetailFlagChance
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadOnly) FName Flag;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, meta = (ClampMin = 0, ClampMax = 1)) float P = 0.f;
};

/** Row of the call types DataTable. Source of truth: Data/CallTypes/*.json (call_type.schema.json). */
USTRUCT(BlueprintType)
struct RESPONSEDISPATCH_API FCallTypeDefinition : public FTableRowBase
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadOnly) FName Id;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) FText DisplayName;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) FName OpeningCode;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) FGameplayTag Tag;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) EIncidentGrade DefaultGrade = EIncidentGrade::G2;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) float BaseWeight = 1.f;
	/** 24 entries, one per hour. Empty means flat. */
	UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<float> HourlyModifier;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) float WeekendModifier = 1.f;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) float RainModifier = 1.f;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<FGradingRule> GradingRules;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<FDetailFlagChance> DetailFlags;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 MinUnits = 1;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<FName> RequiredSkills;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) ERiskAssessment RiskAssessment = ERiskAssessment::THRIVE;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) FName Sensitivity;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<FName> LikelyOffences;
	UPROPERTY(EditAnywhere, BlueprintReadOnly) TArray<FName> ScenarioTemplates;
};

USTRUCT(BlueprintType)
struct RESPONSEDISPATCH_API FIncidentLogEntry
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly) FDateTime At;
	/** Call sign, operator ID or SYSTEM. */
	UPROPERTY(BlueprintReadOnly) FString Author;
	UPROPERTY(BlueprintReadOnly) EIncidentLogKind Kind = EIncidentLogKind::Narrative;
	UPROPERTY(BlueprintReadOnly) FString Text;
};

USTRUCT(BlueprintType)
struct RESPONSEDISPATCH_API FGradeChange
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly) FDateTime At;
	UPROPERTY(BlueprintReadOnly) EIncidentGrade Grade = EIncidentGrade::G2;
	UPROPERTY(BlueprintReadOnly) FString By;
	UPROPERTY(BlueprintReadOnly) FString Reason;
};

/** Runtime incident. Mirrors incident.schema.json. */
USTRUCT(BlueprintType)
struct RESPONSEDISPATCH_API FIncident
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly) int64 Id = 0;
	/** Storm-style log number, e.g. 0412-261026. */
	UPROPERTY(BlueprintReadOnly) FString Reference;
	UPROPERTY(BlueprintReadOnly) FName CallType;
	UPROPERTY(BlueprintReadOnly) EIncidentGrade Grade = EIncidentGrade::G2;
	UPROPERTY(BlueprintReadOnly) TArray<FGradeChange> GradeHistory;
	UPROPERTY(BlueprintReadOnly) EIncidentStatus Status = EIncidentStatus::Received;
	UPROPERTY(BlueprintReadOnly) EIncidentSource Source = EIncidentSource::Emergency999;
	UPROPERTY(BlueprintReadOnly) FDateTime CreatedAt;
	/** Zero means no attendance target (G3 appointments are set separately, G4 has none). */
	UPROPERTY(BlueprintReadOnly) FDateTime TargetAttendBy;
	UPROPERTY(BlueprintReadOnly) FDateTime FirstArrivedAt;
	/** UE world location (cm). BNG is recovered through the world origin, see docs/06. */
	UPROPERTY(BlueprintReadOnly) FVector Location = FVector::ZeroVector;
	UPROPERTY(BlueprintReadOnly) FString LocationDescription;
	UPROPERTY(BlueprintReadOnly) TArray<FName> DetailFlags;
	UPROPERTY(BlueprintReadOnly) TArray<FName> AssignedUnits;
	UPROPERTY(BlueprintReadOnly) TArray<FIncidentLogEntry> Log;
	UPROPERTY(BlueprintReadOnly) bool bTargetBreached = false;
	UPROPERTY(BlueprintReadOnly) FName ClosingCode;

	bool IsOpen() const { return Status != EIncidentStatus::Closed && Status != EIncidentStatus::Cancelled && Status != EIncidentStatus::Resolved; }
	bool NeedsUnits(int32 MinUnits) const { return IsOpen() && Grade != EIncidentGrade::G4 && AssignedUnits.Num() < MinUnits; }
};

/** A deployable resource: a patrol, the player, a double-crewed van. */
USTRUCT(BlueprintType)
struct RESPONSEDISPATCH_API FDispatchUnit
{
	GENERATED_BODY()

	/** Call sign, e.g. TA21 (T = Tameside, A = Ashton). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite) FName CallSign;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) EUnitStatus Status = EUnitStatus::Available;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) FVector Location = FVector::ZeroVector;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) TArray<FName> Skills;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) bool bIsPlayer = false;
	UPROPERTY(BlueprintReadOnly) int64 CurrentIncident = 0;
};
