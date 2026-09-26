#pragma once

#include "CoreMinimal.h"
#include "DispatchTypes.h"

/** Pure dispatch rules. No world or UObject state, so they are unit-tested directly. */
namespace DispatchRules
{
	struct FGradeResult
	{
		EIncidentGrade Grade = EIncidentGrade::G2;
		FString Reason;
	};

	/** First rule whose IfAny intersects the reported flags wins; otherwise the default grade. */
	RESPONSEDISPATCH_API FGradeResult GradeCall(const FCallTypeDefinition& Def, const TArray<FName>& Flags);

	/** Attendance target. Returns FDateTime() (zero) when the grade has no target. */
	RESPONSEDISPATCH_API FDateTime TargetAttendBy(EIncidentGrade Grade, const FDateTime& CreatedAt, bool bRural,
		float G1UrbanMinutes, float G1RuralMinutes, float G2Minutes);

	/** Relative demand weight for this call type at a given time and weather. */
	RESPONSEDISPATCH_API float CallWeight(const FCallTypeDefinition& Def, const FDateTime& At, bool bRaining);

	/** Storm-style reference: 4-digit daily sequence + ddMMyy, e.g. 0412-261026. */
	RESPONSEDISPATCH_API FString MakeReference(int32 DailySequence, const FDateTime& At);

	/** Queue priority: lower sorts first. Grade first, then time to (or past) target, then age. */
	RESPONSEDISPATCH_API bool QueueLess(const FIncident& A, const FIncident& B, const FDateTime& Now);
}
