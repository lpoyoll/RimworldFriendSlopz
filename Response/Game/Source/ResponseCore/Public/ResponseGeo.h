#pragma once

#include "CoreMinimal.h"

/**
 * British National Grid <-> UE world coordinates. Mirrors Pipeline/tameside_pipeline/coords.py (docs/06).
 *   X =  (E - OriginE) * 100,  Y = -(N - OriginN) * 100,  Z = H_odn * 100
 */
namespace ResponseGeo
{
	RESPONSECORE_API FVector BngToWorld(double Easting, double Northing, double HeightOdn = 0.0);
	RESPONSECORE_API FVector BngToWorld(double Easting, double Northing, double HeightOdn, double OriginE, double OriginN);
	RESPONSECORE_API void WorldToBng(const FVector& World, double& OutEasting, double& OutNorthing, double& OutHeightOdn);
	RESPONSECORE_API void WorldToBng(const FVector& World, double OriginE, double OriginN, double& OutEasting, double& OutNorthing, double& OutHeightOdn);
	/** "SJ 94507 99..."-style is not needed yet; this formats a plain 12-figure reference for logs: "394508 400516". */
	RESPONSECORE_API FString FormatBng(double Easting, double Northing);
}
