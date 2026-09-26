#include "ResponseSaveSubsystem.h"
#include "ResponseCoreSettings.h"
#include "ResponseSaveParticipant.h"
#include "Dom/JsonObject.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonWriter.h"

DEFINE_LOG_CATEGORY_STATIC(LogResponseSave, Log, All);

void UResponseSaveSubsystem::RegisterParticipant(UObject* Participant)
{
	if (Participant && Participant->Implements<UResponseSaveParticipant>())
	{
		Participants.AddUnique(Participant);
	}
}

void UResponseSaveSubsystem::UnregisterParticipant(UObject* Participant)
{
	Participants.Remove(Participant);
}

FString UResponseSaveSubsystem::SlotPath(const FString& Slot)
{
	return FPaths::ProjectSavedDir() / TEXT("SaveGames") / (Slot + TEXT(".json"));
}

FString UResponseSaveSubsystem::SaveToString() const
{
	TSharedRef<FJsonObject> Root = MakeShared<FJsonObject>();
	Root->SetStringField(TEXT("format"), TEXT("response-save"));
	Root->SetNumberField(TEXT("version"), FormatVersion);
	Root->SetStringField(TEXT("world_seed"), LexToString(GetDefault<UResponseCoreSettings>()->WorldSeed));
	Root->SetStringField(TEXT("saved_at_utc"), FDateTime::UtcNow().ToIso8601());
	TSharedRef<FJsonObject> Systems = MakeShared<FJsonObject>();
	for (const TWeakObjectPtr<UObject>& Weak : Participants)
	{
		if (const IResponseSaveParticipant* P = Cast<IResponseSaveParticipant>(Weak.Get()))
		{
			TSharedRef<FJsonObject> Block = MakeShared<FJsonObject>();
			P->WriteSave(Block);
			Systems->SetObjectField(P->GetSaveKey(), Block);
		}
	}
	Root->SetObjectField(TEXT("systems"), Systems);
	FString Out;
	FJsonSerializer::Serialize(Root, TJsonWriterFactory<>::Create(&Out));
	return Out;
}

bool UResponseSaveSubsystem::LoadFromString(const FString& Json)
{
	TSharedPtr<FJsonObject> Root;
	if (!FJsonSerializer::Deserialize(TJsonReaderFactory<>::Create(Json), Root) || !Root.IsValid()
		|| Root->GetStringField(TEXT("format")) != TEXT("response-save"))
	{
		UE_LOG(LogResponseSave, Error, TEXT("Not a RESPONSE save file"));
		return false;
	}
	if (Root->GetIntegerField(TEXT("version")) > FormatVersion)
	{
		UE_LOG(LogResponseSave, Error, TEXT("Save is from a newer version"));
		return false;
	}
	const TSharedPtr<FJsonObject>* Systems;
	if (!Root->TryGetObjectField(TEXT("systems"), Systems))
	{
		return false;
	}
	for (const TWeakObjectPtr<UObject>& Weak : Participants)
	{
		if (IResponseSaveParticipant* P = Cast<IResponseSaveParticipant>(Weak.Get()))
		{
			const TSharedPtr<FJsonObject>* Block;
			if ((*Systems)->TryGetObjectField(P->GetSaveKey(), Block))
			{
				P->ReadSave(Block->ToSharedRef());
			}
		}
	}
	return true;
}

bool UResponseSaveSubsystem::SaveToSlot(const FString& Slot)
{
	return FFileHelper::SaveStringToFile(SaveToString(), *SlotPath(Slot), FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
}

bool UResponseSaveSubsystem::LoadFromSlot(const FString& Slot)
{
	FString Json;
	return FFileHelper::LoadFileToString(Json, *SlotPath(Slot)) && LoadFromString(Json);
}
