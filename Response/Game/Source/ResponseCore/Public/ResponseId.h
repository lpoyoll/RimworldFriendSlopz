#pragma once

#include "CoreMinimal.h"
#include "ResponseId.generated.h"

/** Kinds of persistent entity. The prefix appears in string IDs and data files (common.schema.json "id"). */
UENUM(BlueprintType)
enum class EResponseIdKind : uint8
{
	None, Npc, Vehicle, Address, Incident, PncRecord, Exhibit, Event, Footprint
};

/**
 * Stable 64-bit entity ID. Generated deterministically from (world seed, kind, index) so the same seed makes the same
 * city, and records can be generated lazily the first time a player looks them up.
 */
USTRUCT(BlueprintType)
struct RESPONSECORE_API FResponseId
{
	GENERATED_BODY()

	UPROPERTY(SaveGame, BlueprintReadOnly) EResponseIdKind Kind = EResponseIdKind::None;
	UPROPERTY(SaveGame) int64 Value = 0;

	bool IsValid() const { return Kind != EResponseIdKind::None; }
	bool operator==(const FResponseId& O) const { return Kind == O.Kind && Value == O.Value; }
	bool operator!=(const FResponseId& O) const { return !(*this == O); }

	/** e.g. npc_00a1b2c3d4e5f607 */
	FString ToString() const;
	static bool Parse(const FString& S, FResponseId& Out);
	static FResponseId Make(EResponseIdKind Kind, int64 WorldSeed, uint64 Index);
	static const TCHAR* Prefix(EResponseIdKind Kind);

	friend uint32 GetTypeHash(const FResponseId& Id) { return HashCombine(::GetTypeHash(Id.Value), ::GetTypeHash(static_cast<uint8>(Id.Kind))); }
};

/** SplitMix64: tiny, fast, well-distributed. Used for IDs and to seed RNG streams. */
namespace ResponseHash
{
	RESPONSECORE_API uint64 SplitMix64(uint64 X);
	RESPONSECORE_API uint64 Combine(uint64 A, uint64 B);
	/** FNV-1a over a string, for naming streams. */
	RESPONSECORE_API uint64 String(const FString& S);
}

/**
 * Deterministic random stream (xoshiro256**). Every system gets its own named stream derived from the world seed,
 * so adding randomness to one system never changes another system's results.
 */
struct RESPONSECORE_API FResponseRandom
{
	FResponseRandom() : FResponseRandom(0) {}
	explicit FResponseRandom(uint64 Seed);
	static FResponseRandom ForSystem(int64 WorldSeed, const FString& SystemName);

	uint64 Next();
	/** [0, 1) */
	double Unit();
	/** [Min, Max] inclusive */
	int32 Range(int32 Min, int32 Max);
	bool Chance(double P) { return Unit() < P; }
	/** Index chosen with probability proportional to Weights (all >= 0). Returns INDEX_NONE if all are zero. */
	int32 Weighted(const TArray<float>& Weights);

private:
	uint64 S[4];
};
