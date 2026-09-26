#include "DispatchRules.h"

namespace DispatchRules
{
	FGradeResult GradeCall(const FCallTypeDefinition& Def, const TArray<FName>& Flags)
	{
		for (const FGradingRule& Rule : Def.GradingRules)
		{
			for (const FName& Flag : Rule.IfAny)
			{
				if (Flags.Contains(Flag))
				{
					return { Rule.Grade, Rule.Reason.IsEmpty() ? FString::Printf(TEXT("Reported: %s"), *Flag.ToString()) : Rule.Reason };
				}
			}
		}
		return { Def.DefaultGrade, TEXT("Default grade for call type") };
	}

	FDateTime TargetAttendBy(EIncidentGrade Grade, const FDateTime& CreatedAt, bool bRural,
		float G1UrbanMinutes, float G1RuralMinutes, float G2Minutes)
	{
		switch (Grade)
		{
		case EIncidentGrade::G1: return CreatedAt + FTimespan::FromMinutes(bRural ? G1RuralMinutes : G1UrbanMinutes);
		case EIncidentGrade::G2: return CreatedAt + FTimespan::FromMinutes(G2Minutes);
		default: return FDateTime();
		}
	}

	float CallWeight(const FCallTypeDefinition& Def, const FDateTime& At, bool bRaining)
	{
		float W = Def.BaseWeight;
		if (Def.HourlyModifier.Num() == 24)
		{
			W *= Def.HourlyModifier[At.GetHour()];
		}
		// Friday evening to Sunday counts as weekend demand.
		const EDayOfWeek Day = At.GetDayOfWeek();
		const bool bWeekend = Day == EDayOfWeek::Saturday || Day == EDayOfWeek::Sunday || (Day == EDayOfWeek::Friday && At.GetHour() >= 18);
		if (bWeekend)
		{
			W *= Def.WeekendModifier;
		}
		if (bRaining)
		{
			W *= Def.RainModifier;
		}
		return FMath::Max(0.f, W);
	}

	FString MakeReference(int32 DailySequence, const FDateTime& At)
	{
		return FString::Printf(TEXT("%04d-%02d%02d%02d"), DailySequence % 10000, At.GetDay(), At.GetMonth(), At.GetYear() % 100);
	}

	bool QueueLess(const FIncident& A, const FIncident& B, const FDateTime& Now)
	{
		if (A.Grade != B.Grade)
		{
			return A.Grade < B.Grade;
		}
		const bool bAT = A.TargetAttendBy.GetTicks() != 0;
		const bool bBT = B.TargetAttendBy.GetTicks() != 0;
		if (bAT && bBT && A.TargetAttendBy != B.TargetAttendBy)
		{
			return A.TargetAttendBy < B.TargetAttendBy;
		}
		if (bAT != bBT)
		{
			return bAT;
		}
		return A.CreatedAt < B.CreatedAt;
	}
}
