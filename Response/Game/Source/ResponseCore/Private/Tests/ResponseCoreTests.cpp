#include "Misc/AutomationTest.h"
#include "ResponseEventLog.h"
#include "ResponseEventTags.h"
#include "ResponseGeo.h"
#include "ResponseId.h"
#include "ResponseTime.h"

#if WITH_DEV_AUTOMATION_TESTS

#define CoreTestFlags (EAutomationTestFlags::EditorContext | EAutomationTestFlags::EngineFilter)

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FResponseIdTest, "Response.Core.Id", CoreTestFlags)
bool FResponseIdTest::RunTest(const FString&)
{
	const FResponseId A = FResponseId::Make(EResponseIdKind::Npc, 1013, 42);
	const FResponseId B = FResponseId::Make(EResponseIdKind::Npc, 1013, 42);
	const FResponseId C = FResponseId::Make(EResponseIdKind::Npc, 1013, 43);
	TestTrue(TEXT("deterministic"), A == B);
	TestTrue(TEXT("distinct"), A != C);
	const FString S = A.ToString();
	TestTrue(TEXT("prefix"), S.StartsWith(TEXT("npc_")) && S.Len() == 20);
	FResponseId Parsed;
	TestTrue(TEXT("parse"), FResponseId::Parse(S, Parsed) && Parsed == A);
	TestFalse(TEXT("reject junk"), FResponseId::Parse(TEXT("npc_xyz"), Parsed));
	return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FResponseRandomTest, "Response.Core.Random", CoreTestFlags)
bool FResponseRandomTest::RunTest(const FString&)
{
	FResponseRandom A = FResponseRandom::ForSystem(1013, TEXT("Dispatch"));
	FResponseRandom B = FResponseRandom::ForSystem(1013, TEXT("Dispatch"));
	FResponseRandom C = FResponseRandom::ForSystem(1013, TEXT("City"));
	bool bSame = true, bDiffers = false;
	for (int32 I = 0; I < 100; ++I)
	{
		const uint64 X = A.Next();
		bSame &= X == B.Next();
		bDiffers |= X != C.Next();
	}
	TestTrue(TEXT("same stream reproduces"), bSame);
	TestTrue(TEXT("streams independent"), bDiffers);
	int32 Hits[3] = { 0, 0, 0 };
	for (int32 I = 0; I < 30000; ++I) { ++Hits[A.Weighted({ 1.f, 0.f, 3.f })]; }
	TestEqual(TEXT("zero weight never chosen"), Hits[1], 0);
	TestTrue(TEXT("3:1 ratio"), FMath::IsNearlyEqual(Hits[2] / static_cast<double>(Hits[0]), 3.0, 0.3));
	return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FResponseGeoTest, "Response.Core.Geo", CoreTestFlags)
bool FResponseGeoTest::RunTest(const FString&)
{
	// Same numbers as Pipeline/tests/test_coords.py and the CLI example in docs/06.
	const FVector W = ResponseGeo::BngToWorld(393900.0, 399000.0, 0.0, 393000.0, 398000.0);
	TestEqual(TEXT("east +X"), W.X, 90000.0);
	TestEqual(TEXT("north -Y"), W.Y, -100000.0);
	double E, N, H;
	ResponseGeo::WorldToBng(ResponseGeo::BngToWorld(394508.25, 400516.5, 145.8, 393000.0, 398000.0), 393000.0, 398000.0, E, N, H);
	TestTrue(TEXT("round trip"), FMath::IsNearlyEqual(E, 394508.25, 1e-6) && FMath::IsNearlyEqual(N, 400516.5, 1e-6) && FMath::IsNearlyEqual(H, 145.8, 1e-6));
	return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FResponseTimeTest, "Response.Core.Time", CoreTestFlags)
bool FResponseTimeTest::RunTest(const FString&)
{
	// BST 2026: 29 March 01:00 UTC to 25 October 01:00 UTC.
	TestFalse(TEXT("GMT before"), ResponseTime::IsBritishSummerTime(FDateTime(2026, 3, 29, 0, 59)));
	TestTrue(TEXT("BST after"), ResponseTime::IsBritishSummerTime(FDateTime(2026, 3, 29, 1, 0)));
	TestTrue(TEXT("BST before end"), ResponseTime::IsBritishSummerTime(FDateTime(2026, 10, 25, 0, 59)));
	TestFalse(TEXT("GMT after end"), ResponseTime::IsBritishSummerTime(FDateTime(2026, 10, 25, 1, 0)));
	TestEqual(TEXT("local summer"), ResponseTime::UtcToLocal(FDateTime(2026, 7, 1, 12)), FDateTime(2026, 7, 1, 13));
	TestEqual(TEXT("local->utc"), ResponseTime::LocalToUtc(FDateTime(2026, 7, 1, 13)), FDateTime(2026, 7, 1, 12));

	// Ashton sunrise/sunset (checked against published Manchester times, within 3 minutes).
	FDateTime Rise, Set;
	TestTrue(TEXT("midsummer"), ResponseTime::SunriseSunsetUtc(FDateTime(2026, 6, 21), 53.4889, -2.0870, Rise, Set));
	TestTrue(TEXT("rise ~03:38 UTC"), FMath::Abs((Rise - FDateTime(2026, 6, 21, 3, 38)).GetTotalMinutes()) < 3);
	TestTrue(TEXT("set ~20:40 UTC"), FMath::Abs((Set - FDateTime(2026, 6, 21, 20, 40)).GetTotalMinutes()) < 3);
	TestTrue(TEXT("midwinter"), ResponseTime::SunriseSunsetUtc(FDateTime(2026, 12, 21), 53.4889, -2.0870, Rise, Set));
	TestTrue(TEXT("rise ~08:21 UTC"), FMath::Abs((Rise - FDateTime(2026, 12, 21, 8, 21)).GetTotalMinutes()) < 3);

	double El, Az;
	ResponseTime::SunPosition(FDateTime(2026, 6, 21, 12, 8), 53.4889, -2.0870, El, Az);
	TestTrue(TEXT("solar noon elevation ~60 deg"), FMath::IsNearlyEqual(El, 60.0, 1.0));
	TestTrue(TEXT("solar noon due south"), FMath::IsNearlyEqual(Az, 180.0, 3.0));

	TestTrue(TEXT("Fri 18:00 weekend demand"), ResponseTime::IsWeekendDemand(FDateTime(2026, 10, 30, 18, 0)));
	TestFalse(TEXT("Fri 17:59 not"), ResponseTime::IsWeekendDemand(FDateTime(2026, 10, 30, 17, 59)));
	return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FResponseEventJsonTest, "Response.Core.EventJson", CoreTestFlags)
bool FResponseEventJsonTest::RunTest(const FString&)
{
	FResponseEvent E;
	E.Id = FResponseId::Make(EResponseIdKind::Event, 1013, 7);
	E.Sequence = 7;
	E.At = FDateTime(2026, 10, 26, 22, 14, 5);
	E.Type = TAG_Event_Dispatch_Graded;
	E.Actor = TEXT("AI_DISPATCH");
	E.IncidentId = 12;
	E.Location = FVector(100, -200, 14500);
	E.Text = TEXT("Graded G1");
	E.Data.Add(TEXT("ref"), TEXT("0001-261026"));
	FResponseEvent Back;
	TestTrue(TEXT("parse"), UResponseEventLog::FromJson(UResponseEventLog::ToJson(E), Back));
	TestTrue(TEXT("id"), Back.Id == E.Id);
	TestEqual(TEXT("time"), Back.At, E.At);
	TestTrue(TEXT("tag"), Back.Type == E.Type);
	TestEqual(TEXT("loc"), Back.Location, E.Location);
	TestEqual(TEXT("data"), Back.Data.FindRef(TEXT("ref")), FString(TEXT("0001-261026")));
	return true;
}

#endif
