#include "ResponseId.h"

namespace ResponseHash
{
	uint64 SplitMix64(uint64 X)
	{
		X += 0x9E3779B97F4A7C15ull;
		X = (X ^ (X >> 30)) * 0xBF58476D1CE4E5B9ull;
		X = (X ^ (X >> 27)) * 0x94D049BB133111EBull;
		return X ^ (X >> 31);
	}

	uint64 Combine(uint64 A, uint64 B)
	{
		return SplitMix64(A ^ (SplitMix64(B) + 0x9E3779B97F4A7C15ull + (A << 6) + (A >> 2)));
	}

	uint64 String(const FString& S)
	{
		uint64 H = 0xCBF29CE484222325ull;
		for (const TCHAR C : S)
		{
			H ^= static_cast<uint64>(C);
			H *= 0x100000001B3ull;
		}
		return H;
	}
}

const TCHAR* FResponseId::Prefix(EResponseIdKind Kind)
{
	switch (Kind)
	{
	case EResponseIdKind::Npc: return TEXT("npc");
	case EResponseIdKind::Vehicle: return TEXT("veh");
	case EResponseIdKind::Address: return TEXT("adr");
	case EResponseIdKind::Incident: return TEXT("inc");
	case EResponseIdKind::PncRecord: return TEXT("pnc");
	case EResponseIdKind::Exhibit: return TEXT("exh");
	case EResponseIdKind::Event: return TEXT("evt");
	case EResponseIdKind::Footprint: return TEXT("fp");
	default: return TEXT("none");
	}
}

FString FResponseId::ToString() const
{
	return FString::Printf(TEXT("%s_%016llx"), Prefix(Kind), static_cast<uint64>(Value));
}

bool FResponseId::Parse(const FString& S, FResponseId& Out)
{
	FString Left, Right;
	if (!S.Split(TEXT("_"), &Left, &Right) || Right.Len() != 16)
	{
		return false;
	}
	for (uint8 K = 1; K <= static_cast<uint8>(EResponseIdKind::Footprint); ++K)
	{
		if (Left == Prefix(static_cast<EResponseIdKind>(K)))
		{
			uint64 V = 0;
			for (const TCHAR C : Right)
			{
				if (!FChar::IsHexDigit(C)) { return false; }
				V = (V << 4) | static_cast<uint64>(FParse::HexDigit(C));
			}
			Out.Kind = static_cast<EResponseIdKind>(K);
			Out.Value = static_cast<int64>(V);
			return true;
		}
	}
	return false;
}

FResponseId FResponseId::Make(EResponseIdKind Kind, int64 WorldSeed, uint64 Index)
{
	FResponseId Id;
	Id.Kind = Kind;
	Id.Value = static_cast<int64>(ResponseHash::Combine(ResponseHash::Combine(static_cast<uint64>(WorldSeed), static_cast<uint64>(Kind)), Index));
	return Id;
}

// ---------------------------------------------------------------- xoshiro256**

static FORCEINLINE uint64 Rotl(uint64 X, int K) { return (X << K) | (X >> (64 - K)); }

FResponseRandom::FResponseRandom(uint64 Seed)
{
	uint64 X = Seed;
	for (uint64& Word : S)
	{
		X = ResponseHash::SplitMix64(X);
		Word = X;
	}
}

FResponseRandom FResponseRandom::ForSystem(int64 WorldSeed, const FString& SystemName)
{
	return FResponseRandom(ResponseHash::Combine(static_cast<uint64>(WorldSeed), ResponseHash::String(SystemName)));
}

uint64 FResponseRandom::Next()
{
	const uint64 Result = Rotl(S[1] * 5, 7) * 9;
	const uint64 T = S[1] << 17;
	S[2] ^= S[0]; S[3] ^= S[1]; S[1] ^= S[2]; S[0] ^= S[3];
	S[2] ^= T;
	S[3] = Rotl(S[3], 45);
	return Result;
}

double FResponseRandom::Unit()
{
	return static_cast<double>(Next() >> 11) * (1.0 / 9007199254740992.0);
}

int32 FResponseRandom::Range(int32 Min, int32 Max)
{
	if (Max <= Min) { return Min; }
	const uint64 Span = static_cast<uint64>(static_cast<int64>(Max) - Min + 1);
	return Min + static_cast<int32>(Next() % Span);
}

int32 FResponseRandom::Weighted(const TArray<float>& Weights)
{
	double Total = 0;
	for (const float W : Weights) { Total += FMath::Max(0.f, W); }
	if (Total <= 0) { return INDEX_NONE; }
	double R = Unit() * Total;
	for (int32 I = 0; I < Weights.Num(); ++I)
	{
		R -= FMath::Max(0.f, Weights[I]);
		if (R < 0) { return I; }
	}
	return Weights.Num() - 1;
}
