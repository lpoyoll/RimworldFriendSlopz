#include "ResponseEventLog.h"
#include "ResponseClockSubsystem.h"
#include "ResponseCoreSettings.h"
#include "ResponseSaveSubsystem.h"
#include "Dom/JsonObject.h"
#include "Engine/GameInstance.h"
#include "Engine/World.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonWriter.h"

void UResponseEventLog::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	if (UGameInstance* GI = GetWorld() ? GetWorld()->GetGameInstance() : nullptr)
	{
		if (UResponseSaveSubsystem* Save = GI->GetSubsystem<UResponseSaveSubsystem>())
		{
			Save->RegisterParticipant(this);
		}
	}
}

FDateTime UResponseEventLog::Now() const
{
	const UWorld* W = GetWorld();
	if (const UResponseClockSubsystem* Clock = W ? W->GetSubsystem<UResponseClockSubsystem>() : nullptr)
	{
		return Clock->GetLocalTime();
	}
	return FallbackTime;
}

const FResponseEvent& UResponseEventLog::Record(FGameplayTag Type, const FString& Actor, const FString& Text, int64 IncidentId,
	FResponseId Subject, FVector Location, const TMap<FName, FString>& Data)
{
	FResponseEvent E;
	E.Sequence = NextSequence++;
	E.Id = FResponseId::Make(EResponseIdKind::Event, GetDefault<UResponseCoreSettings>()->WorldSeed, static_cast<uint64>(E.Sequence));
	E.At = Now();
	E.Type = Type;
	E.Actor = Actor;
	E.Subject = Subject;
	E.IncidentId = IncidentId;
	E.Location = Location;
	E.Text = Text;
	E.Data = Data;
	const FResponseEvent& Stored = Events.Add_GetRef(MoveTemp(E));
	OnEventRecorded.Broadcast(Stored);
	return Stored;
}

TArray<FResponseEvent> UResponseEventLog::Query(const FResponseEventQuery& Q) const
{
	TArray<FResponseEvent> Out;
	for (const FResponseEvent& E : Events)
	{
		if (Q.Type.IsValid() && !E.Type.MatchesTag(Q.Type)) continue;
		if (Q.IncidentId != 0 && E.IncidentId != Q.IncidentId) continue;
		if (!Q.Actor.IsEmpty() && E.Actor != Q.Actor) continue;
		if (Q.From.GetTicks() != 0 && E.At < Q.From) continue;
		if (Q.To.GetTicks() != 0 && E.At > Q.To) continue;
		Out.Add(E);
	}
	return Out;
}

TSharedRef<FJsonObject> UResponseEventLog::ToJson(const FResponseEvent& E)
{
	TSharedRef<FJsonObject> J = MakeShared<FJsonObject>();
	J->SetStringField(TEXT("id"), E.Id.ToString());
	J->SetNumberField(TEXT("seq"), static_cast<double>(E.Sequence));
	J->SetStringField(TEXT("at"), E.At.ToIso8601());
	J->SetStringField(TEXT("type"), E.Type.ToString());
	J->SetStringField(TEXT("actor"), E.Actor);
	if (E.Subject.IsValid()) { J->SetStringField(TEXT("subject"), E.Subject.ToString()); }
	if (E.IncidentId != 0) { J->SetNumberField(TEXT("incident"), static_cast<double>(E.IncidentId)); }
	J->SetArrayField(TEXT("location_cm"), { MakeShared<FJsonValueNumber>(E.Location.X), MakeShared<FJsonValueNumber>(E.Location.Y), MakeShared<FJsonValueNumber>(E.Location.Z) });
	J->SetStringField(TEXT("text"), E.Text);
	TSharedRef<FJsonObject> D = MakeShared<FJsonObject>();
	for (const TPair<FName, FString>& P : E.Data) { D->SetStringField(P.Key.ToString(), P.Value); }
	J->SetObjectField(TEXT("data"), D);
	return J;
}

bool UResponseEventLog::FromJson(const TSharedRef<FJsonObject>& J, FResponseEvent& E)
{
	if (!FResponseId::Parse(J->GetStringField(TEXT("id")), E.Id)) { return false; }
	E.Sequence = static_cast<int64>(J->GetNumberField(TEXT("seq")));
	FDateTime::ParseIso8601(*J->GetStringField(TEXT("at")), E.At);
	E.Type = FGameplayTag::RequestGameplayTag(FName(*J->GetStringField(TEXT("type"))), false);
	E.Actor = J->GetStringField(TEXT("actor"));
	FString Subject;
	if (J->TryGetStringField(TEXT("subject"), Subject)) { FResponseId::Parse(Subject, E.Subject); }
	double Inc;
	if (J->TryGetNumberField(TEXT("incident"), Inc)) { E.IncidentId = static_cast<int64>(Inc); }
	const TArray<TSharedPtr<FJsonValue>>* Loc;
	if (J->TryGetArrayField(TEXT("location_cm"), Loc) && Loc->Num() == 3)
	{
		E.Location = FVector((*Loc)[0]->AsNumber(), (*Loc)[1]->AsNumber(), (*Loc)[2]->AsNumber());
	}
	E.Text = J->GetStringField(TEXT("text"));
	const TSharedPtr<FJsonObject>* D;
	if (J->TryGetObjectField(TEXT("data"), D))
	{
		for (const TPair<FString, TSharedPtr<FJsonValue>>& P : (*D)->Values) { E.Data.Add(FName(*P.Key), P.Value->AsString()); }
	}
	return true;
}

FString UResponseEventLog::ExportJson(const FResponseEventQuery& Q) const
{
	TArray<TSharedPtr<FJsonValue>> Arr;
	for (const FResponseEvent& E : Query(Q)) { Arr.Add(MakeShared<FJsonValueObject>(ToJson(E))); }
	FString Out;
	FJsonSerializer::Serialize(Arr, TJsonWriterFactory<>::Create(&Out));
	return Out;
}

void UResponseEventLog::WriteSave(TSharedRef<FJsonObject> Out) const
{
	TArray<TSharedPtr<FJsonValue>> Arr;
	for (const FResponseEvent& E : Events) { Arr.Add(MakeShared<FJsonValueObject>(ToJson(E))); }
	Out->SetArrayField(TEXT("events"), Arr);
	Out->SetNumberField(TEXT("next_seq"), static_cast<double>(NextSequence));
}

void UResponseEventLog::ReadSave(const TSharedRef<FJsonObject>& In)
{
	Events.Reset();
	const TArray<TSharedPtr<FJsonValue>>* Arr;
	if (In->TryGetArrayField(TEXT("events"), Arr))
	{
		for (const TSharedPtr<FJsonValue>& V : *Arr)
		{
			FResponseEvent E;
			if (FromJson(V->AsObject().ToSharedRef(), E)) { Events.Add(MoveTemp(E)); }
		}
	}
	NextSequence = static_cast<int64>(In->GetNumberField(TEXT("next_seq")));
}
