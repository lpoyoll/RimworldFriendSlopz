#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "ResponseCoreSettings.generated.h"

UENUM(BlueprintType)
enum class EStreetNameMode : uint8
{
	Real,
	Altered
};

/** Project settings > Game > Response Core. World-wide constants and realism toggles. */
UCLASS(Config = Game, DefaultConfig, meta = (DisplayName = "Response Core"))
class RESPONSECORE_API UResponseCoreSettings : public UDeveloperSettings
{
	GENERATED_BODY()

public:
	/** BNG point that maps to UE (0,0,0). Must match Pipeline/config/tameside.json. Never change after content exists. */
	UPROPERTY(Config, EditAnywhere, Category = "World") double OriginEasting = 393000.0;
	UPROPERTY(Config, EditAnywhere, Category = "World") double OriginNorthing = 398000.0;
	/** Latitude/longitude used for the sun (Ashton-under-Lyne). */
	UPROPERTY(Config, EditAnywhere, Category = "World") double Latitude = 53.4889;
	UPROPERTY(Config, EditAnywhere, Category = "World") double Longitude = -2.0870;

	/** Seed for the whole simulated city. Same seed, same people, records and crime. */
	UPROPERTY(Config, EditAnywhere, Category = "Simulation") int64 WorldSeed = 1013;
	/** Game seconds per real second. 1 = real time (the realism default). */
	UPROPERTY(Config, EditAnywhere, Category = "Simulation", meta = (ClampMin = 0)) float DefaultTimeScale = 1.f;

	UPROPERTY(Config, EditAnywhere, Category = "Presentation") EStreetNameMode StreetNames = EStreetNameMode::Real;
};
