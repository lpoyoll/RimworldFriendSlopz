#pragma once

#include "CoreMinimal.h"

/**
 * Pure time maths: UK civil time (GMT/BST) and the sun position for Tameside.
 * Game time is UK local civil time, because that is what the MDT, the Storm log and the custody record show.
 */
namespace ResponseTime
{
	/** BST runs from 01:00 UTC on the last Sunday of March to 01:00 UTC on the last Sunday of October. */
	RESPONSECORE_API bool IsBritishSummerTime(const FDateTime& Utc);
	RESPONSECORE_API FDateTime UtcToLocal(const FDateTime& Utc);
	/** At the autumn change the repeated local hour is treated as BST (first occurrence). */
	RESPONSECORE_API FDateTime LocalToUtc(const FDateTime& Local);

	/** NOAA sunrise/sunset in UTC for the given date. Returns false for polar day/night (never at 53 deg N). */
	RESPONSECORE_API bool SunriseSunsetUtc(const FDateTime& Date, double LatDeg, double LonDeg, FDateTime& OutRise, FDateTime& OutSet);
	/** Solar elevation and azimuth (degrees; azimuth clockwise from north) at a UTC time. */
	RESPONSECORE_API void SunPosition(const FDateTime& Utc, double LatDeg, double LonDeg, double& OutElevation, double& OutAzimuth);

	/** Friday 18:00 to Sunday 23:59, the night-time economy demand window. */
	RESPONSECORE_API bool IsWeekendDemand(const FDateTime& Local);
}
