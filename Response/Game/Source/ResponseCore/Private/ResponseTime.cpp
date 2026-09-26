#include "ResponseTime.h"

namespace
{
	FDateTime LastSundayAt1Utc(int32 Year, int32 Month)
	{
		FDateTime D(Year, Month, FDateTime::DaysInMonth(Year, Month), 1, 0, 0);
		while (D.GetDayOfWeek() != EDayOfWeek::Sunday)
		{
			D -= FTimespan::FromDays(1);
		}
		return D;
	}

	struct FSolar { double EqTimeMin; double DeclRad; };

	FSolar Solar(const FDateTime& Utc)
	{
		const double Gamma = 2.0 * PI / 365.0 * (Utc.GetDayOfYear() - 1 + (Utc.GetHour() - 12) / 24.0);
		FSolar S;
		S.EqTimeMin = 229.18 * (0.000075 + 0.001868 * FMath::Cos(Gamma) - 0.032077 * FMath::Sin(Gamma)
			- 0.014615 * FMath::Cos(2 * Gamma) - 0.040849 * FMath::Sin(2 * Gamma));
		S.DeclRad = 0.006918 - 0.399912 * FMath::Cos(Gamma) + 0.070257 * FMath::Sin(Gamma) - 0.006758 * FMath::Cos(2 * Gamma)
			+ 0.000907 * FMath::Sin(2 * Gamma) - 0.002697 * FMath::Cos(3 * Gamma) + 0.00148 * FMath::Sin(3 * Gamma);
		return S;
	}
}

namespace ResponseTime
{
	bool IsBritishSummerTime(const FDateTime& Utc)
	{
		const int32 Y = Utc.GetYear();
		return Utc >= LastSundayAt1Utc(Y, 3) && Utc < LastSundayAt1Utc(Y, 10);
	}

	FDateTime UtcToLocal(const FDateTime& Utc)
	{
		return IsBritishSummerTime(Utc) ? Utc + FTimespan::FromHours(1) : Utc;
	}

	FDateTime LocalToUtc(const FDateTime& Local)
	{
		const FDateTime AsBst = Local - FTimespan::FromHours(1);
		return IsBritishSummerTime(AsBst) ? AsBst : Local;
	}

	bool SunriseSunsetUtc(const FDateTime& Date, double LatDeg, double LonDeg, FDateTime& OutRise, FDateTime& OutSet)
	{
		const FDateTime Noon(Date.GetYear(), Date.GetMonth(), Date.GetDay(), 12);
		const FSolar S = Solar(Noon);
		const double Lat = FMath::DegreesToRadians(LatDeg);
		const double CosHa = FMath::Cos(FMath::DegreesToRadians(90.833)) / (FMath::Cos(Lat) * FMath::Cos(S.DeclRad)) - FMath::Tan(Lat) * FMath::Tan(S.DeclRad);
		if (CosHa < -1.0 || CosHa > 1.0)
		{
			return false;
		}
		const double HaDeg = FMath::RadiansToDegrees(FMath::Acos(CosHa));
		const FDateTime Midnight = Date.GetDate();
		OutRise = Midnight + FTimespan::FromMinutes(720.0 - 4.0 * (LonDeg + HaDeg) - S.EqTimeMin);
		OutSet = Midnight + FTimespan::FromMinutes(720.0 - 4.0 * (LonDeg - HaDeg) - S.EqTimeMin);
		return true;
	}

	void SunPosition(const FDateTime& Utc, double LatDeg, double LonDeg, double& OutElevation, double& OutAzimuth)
	{
		const FSolar S = Solar(Utc);
		const double Minutes = Utc.GetHour() * 60.0 + Utc.GetMinute() + Utc.GetSecond() / 60.0;
		const double TrueSolar = Minutes + S.EqTimeMin + 4.0 * LonDeg;
		const double Ha = FMath::DegreesToRadians(TrueSolar / 4.0 - 180.0);
		const double Lat = FMath::DegreesToRadians(LatDeg);
		const double CosZen = FMath::Clamp(FMath::Sin(Lat) * FMath::Sin(S.DeclRad) + FMath::Cos(Lat) * FMath::Cos(S.DeclRad) * FMath::Cos(Ha), -1.0, 1.0);
		const double Zen = FMath::Acos(CosZen);
		OutElevation = 90.0 - FMath::RadiansToDegrees(Zen);
		const double Az = FMath::Atan2(FMath::Sin(Ha), FMath::Cos(Ha) * FMath::Sin(Lat) - FMath::Tan(S.DeclRad) * FMath::Cos(Lat));
		OutAzimuth = FMath::Fmod(FMath::RadiansToDegrees(Az) + 180.0 + 360.0, 360.0);
	}

	bool IsWeekendDemand(const FDateTime& Local)
	{
		const EDayOfWeek D = Local.GetDayOfWeek();
		return D == EDayOfWeek::Saturday || D == EDayOfWeek::Sunday || (D == EDayOfWeek::Friday && Local.GetHour() >= 18);
	}
}
