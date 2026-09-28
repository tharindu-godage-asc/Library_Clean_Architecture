# Feature Spec: <Feature Title>

branch: claude/feature/<feature-slug>
spec: _specs/<feature-slug>.md
owner: numustafa
date: <YYYY-MM-DD>

---

## 0) Summary

**One-liner:** <What are we building?>
**Why now:** <Why does this matter?>
**Who is this for:** <internal users / teams>
**Success looks like:** <measurable outcomes>

---

## 1) Goals and Non-Goals

### Goals
- ...

### Non-Goals
- ...

---

## 2) User Workflow

### Primary workflow
1. ...
2. ...

### Secondary workflows (optional)
- ...

---

## 3) S****l API Surface (What you expose)
### 3.1 Functions
For each function:
- Name:
- Signature:
- Parameters (types, defaults, validation):
- Returns (type + schema):
- Example calls:
- Errors/warnings behavior:

## 4) Data Contracts (DF schemas + join keys)
### 4.1 Inputs
List each input df you will use and where it comes from:
- positions_df:
- settlement_df:
- ...

### 4.2 Required columns (minimum contract)
For each df:
- Required columns (name → type → meaning)
- Optional columns (if used)
- Expected uniqueness / dedup strategy
- Join keys used (exactly)

### 4.3 Output schema
- Output columns added (and meaning)
- Any renamed/standardized columns

## 5) Business Rules
### 5.1 Portfolio filtering
- Default behavior (e.g., all portfolios)
- If portfolios provided:
- Other filters:

### 5.2 ITM/OTM classification
- Underlying price definition used:
- Call/Put rule table (include ATM handling):
- Missing underlying/strike behavior:

### 5.3 Pricing / Greeks
- V1 behavior (computed vs placeholder null columns)
- V2 behavior (future)

## 6) Architecture (must match TECHNICAL_BLUEPRINT)
### 6.1 clients/ (I/O boundary)
- What data is fetched here:

### 6.2 constants/ (values only)
- Column lists (e.g., transactions fields list)
- Mappings, thresholds, config

### 6.3 utils/ (pure transforms, vectorized)
List transforms as df-in → df-out steps:
- normalize_positions(...)
- normalize_settlement(...)
- merge_positions_settlement(...)
- classify_itm_otm(...)
- compute_greeks(...) (or placeholder)

### 6.4 orchestration layer (e.g., recon/ or S****l entrypoints)
- Wiring flow (fetch → transforms → output)
- Caching/perf expectations
- Empty df handling strategy

## 7) Edge Cases
- Missing settlement rows
- Duplicated keys
- Empty portfolio results
- Bad types / coercion rules
- Time/as-of mismatch

## 8) Acceptance Criteria
Checklist that can be tested:
- [ ] Function output schema matches spec
- [ ] Filtering works (single/multi/all portfolios)
- [ ] Classification correct on golden fixture
- [ ] Missing data behavior correct (warn/empty/nulls)

## 9) Test Plan
### 9.1 Golden fixture (required)
Define a tiny fixture dataset + expected output columns/rows.

### 9.2 Unit tests (utils/)
List each transform + cases.

### 9.3 Integration tests (orchestration)
Happy path + missing settlement + filter variations.

## 10) References Used (required)
- TECHNICAL_BLUEPRINT.md: <what you followed>
- temp/<file>: <what you reused>

## 11) Open Questions
- Q:
- Decision owner / date: