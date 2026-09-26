#include "DispatchSubsystem.h"
#include "DispatchRules.h"
#include "DispatchSettings.h"
#include "Dom/JsonObject.h"
#include "HAL/FileManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"

DEFINE_LOG_CATEGORY_STATIC(LogDispatch, Log, All);

namespace
{
	const TCHAR* SystemAuthor = TEXT("SYSTEM");
	const TCHAR* AIAuthor = TEXT("AI_DISPATCH");

	bool ParseGrade(const FString& S, EIncidentGrade& Out)
	{
		if (S == TEXT("G1")) { Out = EIncidentGrade::G1; return true; }
		if (S == TEXT("G2")) { Out = EIncidentGrade::G2; return true; }
		if (S == TEXT("G3")) { Out = EIncidentGrade::G3; return true; }
		if (S == TEXT("G4")) { Out = EIncidentGrade::G4; return true; }
		return false;
	}

	ERiskAssessment ParseRisk(const FString& S)
	{
		if (S == TEXT("DASH")) return ERiskAssessment::DASH;
		if (S == TEXT("missing_person")) return ERiskAssessment::MissingPerson;
		if (S == TEXT("none")) return ERiskAssessment::None;
		return ERiskAssessment::THRIVE;
	}

	TArray<FName> NameArray(const TSharedPtr<FJsonObject>& Obj, const TCHAR* Field)
	{
		TArray<FName> Out;
		const TArray<TSharedPtr<FJsonValue>>* Arr;
		if (Obj->TryGetArrayField(Field, Arr))
		{
			for (const TSharedPtr<FJsonValue>& V : *Arr) { Out.Add(FName(*V->AsString())); }
		}
		return Out;
	}

	FString GradeName(EIncidentGrade G) { return FString::Printf(TEXT("G%d"), static_cast<int32>(G) + 1); }
}

void UDispatchSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	const UDispatchSettings* Settings = GetDefault<UDispatchSettings>();
	const FString Dir = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / Settings->CallTypesDirectory);
	const int32 N = LoadCallTypesFromDirectory(Dir);
	UE_LOG(LogDispatch, Log, TEXT("Loaded %d call types from %s"), N, *Dir);
}

int32 UDispatchSubsystem::LoadCallTypesFromDirectory(const FString& AbsoluteDir)
{
	TArray<FString> Files;
	IFileManager::Get().FindFiles(Files, *(AbsoluteDir / TEXT("*.json")), true, false);
	int32 Total = 0;
	for (const FString& File : Files)
	{
		FString Text;
		if (FFileHelper::LoadFileToString(Text, *(AbsoluteDir / File)))
		{
			Total += LoadCallTypesFromJson(Text);
		}
	}
	return Total;
}

int32 UDispatchSubsystem::LoadCallTypesFromJson(const FString& JsonText)
{
	TSharedPtr<FJsonObject> Root;
	if (!FJsonSerializer::Deserialize(TJsonReaderFactory<>::Create(JsonText), Root) || !Root.IsValid())
	{
		UE_LOG(LogDispatch, Error, TEXT("Call type JSON failed to parse"));
		return 0;
	}
	const TArray<TSharedPtr<FJsonValue>>* List;
	if (!Root->TryGetArrayField(TEXT("call_types"), List))
	{
		return 0;
	}

	int32 Loaded = 0;
	for (const TSharedPtr<FJsonValue>& V : *List)
	{
		const TSharedPtr<FJsonObject> O = V->AsObject();
		FCallTypeDefinition D;
		D.Id = FName(*O->GetStringField(TEXT("id")));
		D.DisplayName = FText::FromString(O->GetStringField(TEXT("display_name")));
		D.OpeningCode = FName(*O->GetStringField(TEXT("opening_code")));
		FString TagName;
		if (O->TryGetStringField(TEXT("tag"), TagName))
		{
			D.Tag = FGameplayTag::RequestGameplayTag(FName(*TagName), /*ErrorIfNotFound*/ false);
		}
		if (!ParseGrade(O->GetStringField(TEXT("default_grade")), D.DefaultGrade))
		{
			UE_LOG(LogDispatch, Error, TEXT("Call type %s has an invalid default_grade"), *D.Id.ToString());
			continue;
		}
		D.BaseWeight = O->GetNumberField(TEXT("base_weight"));
		const TArray<TSharedPtr<FJsonValue>>* Hourly;
		if (O->TryGetArrayField(TEXT("hourly_modifier"), Hourly))
		{
			for (const TSharedPtr<FJsonValue>& H : *Hourly) { D.HourlyModifier.Add(H->AsNumber()); }
		}
		double Num;
		if (O->TryGetNumberField(TEXT("weekend_modifier"), Num)) { D.WeekendModifier = Num; }
		if (O->TryGetNumberField(TEXT("rain_modifier"), Num)) { D.RainModifier = Num; }
		int32 Int;
		if (O->TryGetNumberField(TEXT("min_units"), Int)) { D.MinUnits = Int; }

		const TArray<TSharedPtr<FJsonValue>>* Rules;
		if (O->TryGetArrayField(TEXT("grading_rules"), Rules))
		{
			for (const TSharedPtr<FJsonValue>& R : *Rules)
			{
				const TSharedPtr<FJsonObject> RO = R->AsObject();
				FGradingRule Rule;
				Rule.IfAny = NameArray(RO, TEXT("if_any"));
				ParseGrade(RO->GetStringField(TEXT("grade")), Rule.Grade);
				RO->TryGetStringField(TEXT("reason"), Rule.Reason);
				D.GradingRules.Add(Rule);
			}
		}
		const TArray<TSharedPtr<FJsonValue>>* Flags;
		if (O->TryGetArrayField(TEXT("detail_flags"), Flags))
		{
			for (const TSharedPtr<FJsonValue>& F : *Flags)
			{
				const TSharedPtr<FJsonObject> FO = F->AsObject();
				D.DetailFlags.Add({ FName(*FO->GetStringField(TEXT("flag"))), static_cast<float>(FO->GetNumberField(TEXT("p"))) });
			}
		}
		FString Str;
		if (O->TryGetStringField(TEXT("risk_assessment"), Str)) { D.RiskAssessment = ParseRisk(Str); }
		if (O->TryGetStringField(TEXT("sensitivity"), Str)) { D.Sensitivity = FName(*Str); }
		D.RequiredSkills = NameArray(O, TEXT("required_skills"));
		D.LikelyOffences = NameArray(O, TEXT("likely_offences"));
		D.ScenarioTemplates = NameArray(O, TEXT("scenario_templates"));

		// Later files override earlier ones with the same id, which is how mods replace call types.
		CallTypes.Add(D.Id, MoveTemp(D));
		++Loaded;
	}
	return Loaded;
}

void UDispatchSubsystem::Log(FIncident& Inc, const FString& Author, EIncidentLogKind Kind, const FString& Text)
{
	Inc.Log.Add({ GameTime, Author, Kind, Text });
}

int64 UDispatchSubsystem::CreateIncident(FName CallType, const TArray<FName>& DetailFlags, FVector Location,
	const FString& LocationDescription, EIncidentSource Source, bool bRural)
{
	const FCallTypeDefinition* Def = CallTypes.Find(CallType);
	if (!Def)
	{
		UE_LOG(LogDispatch, Warning, TEXT("CreateIncident: unknown call type %s"), *CallType.ToString());
		return 0;
	}

	if (GameTime.GetDayOfYear() != SequenceDay)
	{
		SequenceDay = GameTime.GetDayOfYear();
		DailySequence = 0;
	}

	FIncident Inc;
	Inc.Id = NextIncidentId++;
	Inc.Reference = DispatchRules::MakeReference(++DailySequence, GameTime);
	Inc.CallType = CallType;
	Inc.Source = Source;
	Inc.CreatedAt = GameTime;
	Inc.Location = Location;
	Inc.LocationDescription = LocationDescription;
	Inc.DetailFlags = DetailFlags;
	Log(Inc, SystemAuthor, EIncidentLogKind::System, FString::Printf(TEXT("%s received: %s"), *Def->OpeningCode.ToString(), *Def->DisplayName.ToString()));

	const DispatchRules::FGradeResult G = DispatchRules::GradeCall(*Def, DetailFlags);
	const UDispatchSettings* S = GetDefault<UDispatchSettings>();
	Inc.Grade = G.Grade;
	Inc.GradeHistory.Add({ GameTime, G.Grade, AIAuthor, G.Reason });
	Inc.TargetAttendBy = DispatchRules::TargetAttendBy(G.Grade, GameTime, bRural, S->G1UrbanTargetMinutes, S->G1RuralTargetMinutes, S->G2TargetMinutes);
	Inc.Status = G.Grade == EIncidentGrade::G4 ? EIncidentStatus::Resolved : EIncidentStatus::Queued;
	Log(Inc, AIAuthor, EIncidentLogKind::Grade, FString::Printf(TEXT("Graded %s: %s"), *GradeName(G.Grade), *G.Reason));

	const int64 Id = Inc.Id;
	Incidents.Add(Id, MoveTemp(Inc));
	OnIncidentCreated.Broadcast(Id);
	return Id;
}

bool UDispatchSubsystem::Regrade(int64 IncidentId, EIncidentGrade NewGrade, const FString& By, const FString& Reason)
{
	FIncident* Inc = Get(IncidentId);
	if (!Inc || Inc->Grade == NewGrade) { return false; }
	const UDispatchSettings* S = GetDefault<UDispatchSettings>();
	Inc->Grade = NewGrade;
	Inc->GradeHistory.Add({ GameTime, NewGrade, By, Reason });
	// A new target runs from the regrade time, not the original call.
	Inc->TargetAttendBy = DispatchRules::TargetAttendBy(NewGrade, GameTime, false, S->G1UrbanTargetMinutes, S->G1RuralTargetMinutes, S->G2TargetMinutes);
	Inc->bTargetBreached = false;
	Log(*Inc, By, EIncidentLogKind::Grade, FString::Printf(TEXT("Regraded %s: %s"), *GradeName(NewGrade), *Reason));
	OnIncidentUpdated.Broadcast(IncidentId);
	return true;
}

bool UDispatchSubsystem::AddLog(int64 IncidentId, const FString& Author, EIncidentLogKind Kind, const FString& Text)
{
	FIncident* Inc = Get(IncidentId);
	if (!Inc) { return false; }
	Log(*Inc, Author, Kind, Text);
	OnIncidentUpdated.Broadcast(IncidentId);
	return true;
}

bool UDispatchSubsystem::SetIncidentStatus(int64 IncidentId, EIncidentStatus NewStatus, const FString& By)
{
	FIncident* Inc = Get(IncidentId);
	if (!Inc || !Inc->IsOpen()) { return false; }
	Inc->Status = NewStatus;
	if (NewStatus == EIncidentStatus::AtScene && Inc->FirstArrivedAt.GetTicks() == 0)
	{
		Inc->FirstArrivedAt = GameTime;
	}
	Log(*Inc, By, EIncidentLogKind::Status, UEnum::GetDisplayValueAsText(NewStatus).ToString());
	OnIncidentUpdated.Broadcast(IncidentId);
	return true;
}

bool UDispatchSubsystem::CloseIncident(int64 IncidentId, FName ClosingCode, const FString& By)
{
	FIncident* Inc = Get(IncidentId);
	if (!Inc || Inc->Status == EIncidentStatus::Closed) { return false; }
	for (const FName& CallSign : Inc->AssignedUnits)
	{
		if (FDispatchUnit* U = GetUnit(CallSign); U && U->CurrentIncident == IncidentId)
		{
			U->CurrentIncident = 0;
			U->Status = EUnitStatus::Available;
		}
	}
	Inc->Status = EIncidentStatus::Closed;
	Inc->ClosingCode = ClosingCode;
	PendingOffers.Remove(IncidentId);
	Log(*Inc, By, EIncidentLogKind::Result, FString::Printf(TEXT("Closed: %s"), *ClosingCode.ToString()));
	OnIncidentUpdated.Broadcast(IncidentId);
	return true;
}

TArray<FIncident> UDispatchSubsystem::GetQueue() const
{
	TArray<FIncident> Out;
	for (const TPair<int64, FIncident>& P : Incidents)
	{
		const FCallTypeDefinition* Def = CallTypes.Find(P.Value.CallType);
		if (P.Value.NeedsUnits(Def ? Def->MinUnits : 1))
		{
			Out.Add(P.Value);
		}
	}
	const FDateTime Now = GameTime;
	Out.Sort([Now](const FIncident& A, const FIncident& B) { return DispatchRules::QueueLess(A, B, Now); });
	return Out;
}

void UDispatchSubsystem::RegisterUnit(const FDispatchUnit& Unit)
{
	Units.Add(Unit.CallSign, Unit);
}

void UDispatchSubsystem::UpdateUnitLocation(FName CallSign, FVector Location)
{
	if (FDispatchUnit* U = GetUnit(CallSign)) { U->Location = Location; }
}

bool UDispatchSubsystem::SetUnitStatus(FName CallSign, EUnitStatus NewStatus)
{
	FDispatchUnit* U = GetUnit(CallSign);
	if (!U) { return false; }
	U->Status = NewStatus;
	return true;
}

bool UDispatchSubsystem::AssignUnit(int64 IncidentId, FName CallSign, const FString& By)
{
	FIncident* Inc = Get(IncidentId);
	FDispatchUnit* U = GetUnit(CallSign);
	if (!Inc || !U || !Inc->IsOpen() || Inc->AssignedUnits.Contains(CallSign)) { return false; }
	if (U->Status != EUnitStatus::Available) { return false; }
	Inc->AssignedUnits.Add(CallSign);
	U->Status = EUnitStatus::Committed;
	U->CurrentIncident = IncidentId;
	if (Inc->Status == EIncidentStatus::Queued || Inc->Status == EIncidentStatus::Graded || Inc->Status == EIncidentStatus::Received)
	{
		Inc->Status = EIncidentStatus::Assigned;
	}
	PendingOffers.Remove(IncidentId);
	Log(*Inc, By, EIncidentLogKind::Assignment, FString::Printf(TEXT("%s assigned"), *CallSign.ToString()));
	OnIncidentUpdated.Broadcast(IncidentId);
	return true;
}

bool UDispatchSubsystem::SelfDeploy(int64 IncidentId, FName CallSign)
{
	if (!AssignUnit(IncidentId, CallSign, CallSign.ToString())) { return false; }
	return AddLog(IncidentId, CallSign.ToString(), EIncidentLogKind::Narrative, TEXT("Self-deployed"));
}

bool UDispatchSubsystem::Divert(FName CallSign, int64 ToIncidentId, const FString& By)
{
	FDispatchUnit* U = GetUnit(CallSign);
	FIncident* To = Get(ToIncidentId);
	if (!U || !To || !To->IsOpen()) { return false; }
	if (FIncident* From = Get(U->CurrentIncident); From && From->Id != ToIncidentId)
	{
		From->AssignedUnits.Remove(CallSign);
		Log(*From, By, EIncidentLogKind::Assignment, FString::Printf(TEXT("%s diverted to %s"), *CallSign.ToString(), *To->Reference));
		if (From->AssignedUnits.Num() == 0 && From->IsOpen())
		{
			From->Status = EIncidentStatus::Queued; // back in the queue, still needing a unit
		}
		OnIncidentUpdated.Broadcast(From->Id);
	}
	U->Status = EUnitStatus::Available;
	U->CurrentIncident = 0;
	return AssignUnit(ToIncidentId, CallSign, By);
}

bool UDispatchSubsystem::DeclineOffer(int64 IncidentId, FName CallSign)
{
	const FOffer* O = PendingOffers.Find(IncidentId);
	if (!O || O->CallSign != CallSign) { return false; }
	PendingOffers.Remove(IncidentId);
	DeclinedByPlayer.Add(IncidentId);
	return AddLog(IncidentId, CallSign.ToString(), EIncidentLogKind::Narrative, TEXT("Unable to attend"));
}

void UDispatchSubsystem::Tick(float DeltaTime)
{
	GameTime += FTimespan::FromSeconds(DeltaTime * TimeScale);
	CheckBreaches();
	if (GetDefault<UDispatchSettings>()->bAutoDispatch)
	{
		RunAIDispatcher();
	}
}

void UDispatchSubsystem::CheckBreaches()
{
	for (TPair<int64, FIncident>& P : Incidents)
	{
		FIncident& Inc = P.Value;
		if (Inc.bTargetBreached || !Inc.IsOpen() || Inc.TargetAttendBy.GetTicks() == 0 || Inc.FirstArrivedAt.GetTicks() != 0)
		{
			continue;
		}
		if (GameTime > Inc.TargetAttendBy)
		{
			Inc.bTargetBreached = true;
			Log(Inc, SystemAuthor, EIncidentLogKind::System, FString::Printf(TEXT("%s attendance target breached"), *GradeName(Inc.Grade)));
			OnTargetBreached.Broadcast(Inc.Id);
		}
	}
}

const FDispatchUnit* UDispatchSubsystem::PickUnit(const FIncident& Inc, bool bPlayerOnly) const
{
	const FCallTypeDefinition* Def = CallTypes.Find(Inc.CallType);
	const FDispatchUnit* Best = nullptr;
	double BestDistSq = TNumericLimits<double>::Max();
	for (const TPair<FName, FDispatchUnit>& P : Units)
	{
		const FDispatchUnit& U = P.Value;
		if (U.Status != EUnitStatus::Available || U.bIsPlayer != bPlayerOnly || Inc.AssignedUnits.Contains(U.CallSign))
		{
			continue;
		}
		bool bSkilled = true;
		if (Def)
		{
			for (const FName& Skill : Def->RequiredSkills) { bSkilled &= U.Skills.Contains(Skill); }
		}
		const double D = FVector::DistSquared(U.Location, Inc.Location);
		if (bSkilled && D < BestDistSq)
		{
			Best = &U;
			BestDistSq = D;
		}
	}
	return Best;
}

void UDispatchSubsystem::RunAIDispatcher()
{
	const UDispatchSettings* S = GetDefault<UDispatchSettings>();
	const double OfferRadiusCm = S->PlayerOfferRadiusMetres * 100.0;

	for (const FIncident& Queued : GetQueue())
	{
		if (Queued.Grade == EIncidentGrade::G3) { continue; } // G3 is booked as an appointment, not dispatched live

		if (const FOffer* O = PendingOffers.Find(Queued.Id))
		{
			if (GameTime < O->ExpiresAt) { continue; }
			PendingOffers.Remove(Queued.Id);
			DeclinedByPlayer.Add(Queued.Id);
		}

		// Offer to the player first if close enough, so the player's shift is fed by real demand.
		if (!DeclinedByPlayer.Contains(Queued.Id))
		{
			if (const FDispatchUnit* P = PickUnit(Queued, true); P && FVector::Dist(P->Location, Queued.Location) <= OfferRadiusCm)
			{
				PendingOffers.Add(Queued.Id, { P->CallSign, GameTime + FTimespan::FromSeconds(S->PlayerOfferSeconds) });
				OnJobOffered.Broadcast(Queued.Id, P->CallSign);
				continue;
			}
		}

		if (const FDispatchUnit* U = PickUnit(Queued, false))
		{
			AssignUnit(Queued.Id, U->CallSign, AIAuthor);
		}
	}
}
