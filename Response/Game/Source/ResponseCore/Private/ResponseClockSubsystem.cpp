#include "ResponseClockSubsystem.h"
#include "ResponseCoreSettings.h"
#include "ResponseSaveSubsystem.h"
#include "ResponseTime.h"
#include "Dom/JsonObject.h"
#include "Engine/GameInstance.h"
#include "Engine/World.h"

void UResponseClockSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	TimeScale = GetDefault<UResponseCoreSettings>()->DefaultTimeScale;
	if (Shifts.Num() == 0)
	{
		// Common UK response pattern: earlies 07-17, lates 14-00, nights 22-07.
		Shifts.Add({ EShiftName::Earlies, 7, 10, 30 });
		Shifts.Add({ EShiftName::Lates, 14, 10, 30 });
		Shifts.Add({ EShiftName::Nights, 22, 9, 30 });
	}
	bWasDaylight = IsDaylight();
	if (UGameInstance* GI = GetWorld() ? GetWorld()->GetGameInstance() : nullptr)
	{
		if (UResponseSaveSubsystem* Save = GI->GetSubsystem<UResponseSaveSubsystem>())
		{
			Save->RegisterParticipant(this);
		}
	}
}

void UResponseClockSubsystem::Tick(float DeltaTime)
{
	if (!bPaused && TimeScale > 0.f)
	{
		Advance(FTimespan::FromSeconds(DeltaTime * TimeScale));
	}
}

FDateTime UResponseClockSubsystem::GetUtcTime() const
{
	return ResponseTime::LocalToUtc(LocalTime);
}

void UResponseClockSubsystem::SetLocalTime(FDateTime NewLocal)
{
	LocalTime = NewLocal;
	bWasDaylight = IsDaylight();
}

void UResponseClockSubsystem::Advance(FTimespan Delta)
{
	const FDateTime Before = LocalTime;
	LocalTime += Delta;
	// One event per hour boundary crossed, even for big jumps, so hourly systems (demand curves) stay consistent.
	FDateTime Hour(Before.GetYear(), Before.GetMonth(), Before.GetDay(), Before.GetHour());
	for (Hour += FTimespan::FromHours(1); Hour <= LocalTime; Hour += FTimespan::FromHours(1))
	{
		OnHourChanged.Broadcast(Hour.GetHour());
	}
	const bool bDay = IsDaylight();
	if (bDay != bWasDaylight)
	{
		bWasDaylight = bDay;
		OnDaylightChanged.Broadcast(bDay);
	}
}

bool UResponseClockSubsystem::IsDaylight() const
{
	double El, Az;
	GetSunPosition(El, Az);
	return El > -0.833; // sunrise/sunset definition (refraction + solar radius)
}

void UResponseClockSubsystem::GetSunPosition(double& Elevation, double& Azimuth) const
{
	const UResponseCoreSettings* S = GetDefault<UResponseCoreSettings>();
	ResponseTime::SunPosition(GetUtcTime(), S->Latitude, S->Longitude, Elevation, Azimuth);
}

bool UResponseClockSubsystem::IsWeekendDemand() const
{
	return ResponseTime::IsWeekendDemand(LocalTime);
}

bool UResponseClockSubsystem::GetShiftAt(FDateTime Local, FShiftDefinition& OutShift, FDateTime& OutStart) const
{
	bool bFound = false;
	for (const FShiftDefinition& Shift : Shifts)
	{
		// Check a start today and yesterday (for shifts crossing midnight).
		for (int32 DayOffset = 0; DayOffset >= -1; --DayOffset)
		{
			const FDateTime Start = Local.GetDate() + FTimespan::FromDays(DayOffset) + FTimespan::FromHours(Shift.StartHour);
			const FDateTime End = Start + FTimespan::FromHours(Shift.LengthHours);
			if (Local >= Start && Local < End && (!bFound || Start > OutStart))
			{
				OutShift = Shift;
				OutStart = Start;
				bFound = true;
			}
		}
	}
	return bFound;
}

void UResponseClockSubsystem::WriteSave(TSharedRef<FJsonObject> Out) const
{
	Out->SetStringField(TEXT("local_time"), LocalTime.ToIso8601());
	Out->SetNumberField(TEXT("time_scale"), TimeScale);
}

void UResponseClockSubsystem::ReadSave(const TSharedRef<FJsonObject>& In)
{
	FDateTime T;
	if (FDateTime::ParseIso8601(*In->GetStringField(TEXT("local_time")), T))
	{
		SetLocalTime(T);
	}
	double Scale;
	if (In->TryGetNumberField(TEXT("time_scale"), Scale))
	{
		TimeScale = Scale;
	}
}
