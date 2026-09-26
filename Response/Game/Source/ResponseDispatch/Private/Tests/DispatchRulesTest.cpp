#include "DispatchRules.h"
#include "Misc/AutomationTest.h"

#if WITH_DEV_AUTOMATION_TESTS

namespace
{
	FCallTypeDefinition MakeDomestic()
	{
		FCallTypeDefinition D;
		D.Id = TEXT("domestic_incident");
		D.DefaultGrade = EIncidentGrade::G2;
		D.BaseWeight = 10.f;
		D.WeekendModifier = 2.f;
		D.RainModifier = 0.5f;
		D.GradingRules.Add({ { TEXT("ongoing"), TEXT("weapon") }, EIncidentGrade::G1, TEXT("Ongoing") });
		D.GradingRules.Add({ { TEXT("historic_report") }, EIncidentGrade::G3, FString() });
		return D;
	}
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FDispatchGradingTest, "Response.Dispatch.Grading",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::EngineFilter)
bool FDispatchGradingTest::RunTest(const FString&)
{
	const FCallTypeDefinition D = MakeDomestic();
	TestEqual(TEXT("first matching rule"), DispatchRules::GradeCall(D, { TEXT("weapon") }).Grade, EIncidentGrade::G1);
	TestEqual(TEXT("second rule"), DispatchRules::GradeCall(D, { TEXT("historic_report") }).Grade, EIncidentGrade::G3);
	TestEqual(TEXT("default"), DispatchRules::GradeCall(D, {}).Grade, EIncidentGrade::G2);
	return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FDispatchTargetsTest, "Response.Dispatch.Targets",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::EngineFilter)
bool FDispatchTargetsTest::RunTest(const FString&)
{
	const FDateTime T0(2026, 10, 26, 22, 0, 0);
	TestEqual(TEXT("G1 urban 15"), DispatchRules::TargetAttendBy(EIncidentGrade::G1, T0, false, 15, 20, 60), T0 + FTimespan::FromMinutes(15));
	TestEqual(TEXT("G1 rural 20"), DispatchRules::TargetAttendBy(EIncidentGrade::G1, T0, true, 15, 20, 60), T0 + FTimespan::FromMinutes(20));
	TestEqual(TEXT("G2 60"), DispatchRules::TargetAttendBy(EIncidentGrade::G2, T0, false, 15, 20, 60), T0 + FTimespan::FromMinutes(60));
	TestEqual(TEXT("G3 none"), DispatchRules::TargetAttendBy(EIncidentGrade::G3, T0, false, 15, 20, 60).GetTicks(), (int64)0);
	TestEqual(TEXT("reference"), DispatchRules::MakeReference(412, T0), FString(TEXT("0412-261026")));
	return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FDispatchWeightTest, "Response.Dispatch.Weight",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::EngineFilter)
bool FDispatchWeightTest::RunTest(const FString&)
{
	const FCallTypeDefinition D = MakeDomestic();
	const FDateTime Monday(2026, 10, 26, 12, 0, 0);
	const FDateTime Saturday(2026, 10, 31, 12, 0, 0);
	TestEqual(TEXT("weekday dry"), DispatchRules::CallWeight(D, Monday, false), 10.f);
	TestEqual(TEXT("weekend"), DispatchRules::CallWeight(D, Saturday, false), 20.f);
	TestEqual(TEXT("rain"), DispatchRules::CallWeight(D, Monday, true), 5.f);
	return true;
}

#endif
