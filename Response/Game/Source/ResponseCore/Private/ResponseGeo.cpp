#include "ResponseGeo.h"
#include "ResponseCoreSettings.h"

namespace ResponseGeo
{
	FVector BngToWorld(double E, double N, double H, double OriginE, double OriginN)
	{
		return FVector((E - OriginE) * 100.0, -(N - OriginN) * 100.0, H * 100.0);
	}

	FVector BngToWorld(double E, double N, double H)
	{
		const UResponseCoreSettings* S = GetDefault<UResponseCoreSettings>();
		return BngToWorld(E, N, H, S->OriginEasting, S->OriginNorthing);
	}

	void WorldToBng(const FVector& W, double OriginE, double OriginN, double& E, double& N, double& H)
	{
		E = W.X / 100.0 + OriginE;
		N = -W.Y / 100.0 + OriginN;
		H = W.Z / 100.0;
	}

	void WorldToBng(const FVector& W, double& E, double& N, double& H)
	{
		const UResponseCoreSettings* S = GetDefault<UResponseCoreSettings>();
		WorldToBng(W, S->OriginEasting, S->OriginNorthing, E, N, H);
	}

	FString FormatBng(double E, double N)
	{
		return FString::Printf(TEXT("%06d %06d"), FMath::RoundToInt(E), FMath::RoundToInt(N));
	}
}
