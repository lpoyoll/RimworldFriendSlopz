"""Validates every JSON file in Data/ against its schema."""
import json
from pathlib import Path

import pytest
from jsonschema import Draft202012Validator
from referencing import Registry, Resource

ROOT = Path(__file__).resolve().parents[2]
SCHEMAS = ROOT / "schemas"


def registry():
    reg = Registry()
    for p in SCHEMAS.glob("*.schema.json"):
        s = json.loads(p.read_text())
        reg = reg.with_resource(s["$id"], Resource.from_contents(s))
    return reg


def validator(name):
    s = json.loads((SCHEMAS / name).read_text())
    Draft202012Validator.check_schema(s)
    return Draft202012Validator(s, registry=registry())


CASES = [
    ("call_type.schema.json", "Data/CallTypes/core_call_types.json"),
    ("incident.schema.json", "Data/Examples/incident.example.json"),
    ("npc_profile.schema.json", "Data/Examples/npc_profile.example.json"),
    ("pnc_record.schema.json", "Data/Examples/pnc_person.example.json"),
    ("building_facade.schema.json", "Data/Examples/building_facade.example.json"),
    ("event.schema.json", "Data/Examples/event.example.json"),
]


@pytest.mark.parametrize("schema,data", CASES)
def test_data_matches_schema(schema, data):
    errors = list(validator(schema).iter_errors(json.loads((ROOT / data).read_text())))
    assert not errors, "\n".join(f"{list(e.path)}: {e.message}" for e in errors)


def test_vertical_slice_has_ten_call_types_across_grades():
    cts = json.loads((ROOT / "Data/CallTypes/core_call_types.json").read_text())["call_types"]
    assert len(cts) == 10
    grades = {r["grade"] for c in cts for r in c["grading_rules"]} | {c["default_grade"] for c in cts}
    assert grades == {"G1", "G2", "G3", "G4"}
