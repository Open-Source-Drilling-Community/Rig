# Drill-floor depth migration

This runbook governs the staged replacement of the misnamed
`DrillFloorElevation` and redundant `IsFixedPlatform` Rig properties.

## Execution status

The expand version and its consumers were deployed to development, production,
and AWE. Independent application and SQLite backups were verified before the
durable writes. Development contains one migrated Rig, production contains
eight migrated Rigs and three preserved photos, and AWE contains no Rigs.

Production migrated three platform depths without changing their IEEE-754
values or signs and assigned 0.5 m standard deviation. `patterson 301` was
reviewed as `ConventionalLandRig`; its inconsistent legacy 10 m value was
discarded because the rotary-table reference belongs to its WellBore. Rig and
photo UUIDs were preserved.

The public-contract contraction is now implemented. The historical SQLite
`IsFixedPlatform` column is intentionally retained in the deployed table shape
to avoid a destructive table rebuild, but it is derived from `RigType` and is
not exposed by any public model or contract. At startup, the contracted service
validates all stored Rig JSON and transactionally removes only the obsolete
`DrillFloorElevation` and Rig-level `IsFixedPlatform` keys. Malformed JSON fails
the migration closed and rolls back the complete cleanup. The operation logs
the examined and changed record counts and is idempotent.

## Invariants

- Every historical `DrillFloorElevation` value is already a depth in SI metres
  relative to WGS84. Copy it exactly to the new mean; never invert its sign.
- `FixedPlatformProperties.DrillFloorDepth` is a Gaussian property. When a
  migrated record has no uncertainty, use 0.5 m as its standard deviation.
- `RigType.PlatformRig` is the discriminator. Only that variant may contain
  `FixedPlatformProperties` or a Rig-owned `ClusterID`.
- An explicit, non-unknown `RigType` that conflicts with legacy fields is
  ambiguous and must be reported for review, not silently rewritten.
- Preserve resource UUIDs, creation timestamps, and modification/concurrency
  tokens during the storage-shape migration.

## Phase 1 - expand contract and consumers

1. Add the platform-only object and Gaussian drill-floor depth to Rig.
2. Accept old-only and new-only payloads. Populate the missing representation
   in memory so old and new clients can coexist.
3. Default a missing drill-floor standard deviation to 0.5 m.
4. Reject platform properties on every non-`PlatformRig` variant and reject
   conflicting old/new depth values.
5. Mark the two old properties obsolete but keep them serialized.
6. Regenerate REST clients and MCP schemas, then migrate every known consumer
   to `RigType` and the Gaussian mean.

No durable record is rewritten merely because it is read during this phase.

## Phase 2 - deploy the expand version

Roll out and verify the expand version in the development deployment first,
then in each remaining Kubernetes deployment using its explicitly confirmed
context and release. Preserve the PVC, claim name, mount path, database
filename, and SQLite `Recreate` strategy.

Before enabling a data write in each deployment:

1. Create and download an application-level Rig batch export.
2. Create an independent, recoverable PVC or SQLite snapshot.
3. Restore each backup into an isolated location and verify record counts and
   representative Rig reads.
4. Record the image digest, release values, database path, backup identifiers,
   and pre-migration audit totals.

## Phase 3 - audit and migrate durable data

The migration operation must be idempotent, transactional per database, and
must stop before writing if any ambiguous record is found. Its dry-run report
must count and identify:

- old-only, new-only, matching dual-shape, and conflicting dual-shape records;
- null/unknown `RigType` with legacy `IsFixedPlatform=true`;
- explicit `RigType` values that conflict with `IsFixedPlatform`;
- non-platform Rigs that contain either drill-floor field or `ClusterID`;
- platform Rigs with missing or invalid Cluster references;
- missing means, non-finite values, and negative/non-finite uncertainties.

For an unambiguous legacy platform record, set:

```text
RigType = PlatformRig                         (only when null/Unknown and the legacy flag is true)
FixedPlatformProperties.DrillFloorDepth.GaussianValue.Mean
    = DrillFloorElevation                    (exact value, no sign change)
FixedPlatformProperties.DrillFloorDepth.GaussianValue.StandardDeviation
    = 0.5                                    (only when absent)
```

Re-run the audit after the transaction and compare UUIDs, timestamps, record
counts, and representative serialized values with the pre-migration report.
Exercise create/read/update through REST and MCP and verify the affected Web
pages before continuing to the next deployment.

## Phase 4 - contract

The old fields may be removed only after:

- all three durable stores report zero old-only and conflicting records;
- all supported consumers use `RigType` and `DrillFloorDepth`;
- no supported request logs contain either obsolete JSON property throughout
  the agreed compatibility window;
- application exports and independent snapshots have passed restore checks.

Then remove `DrillFloorElevation`, the Rig-level `IsFixedPlatform`, and the
dual-shape compatibility adapter. Regenerate every dependent schema/client,
rebuild and test all consumers, and deploy in the same environment order. The
legacy physical projection column may be removed only by a separately backed
up and tested SQLite table migration; retaining it as an internal value derived
from `RigType` does not retain either obsolete public property. The contracted
startup migration removes both obsolete keys from persisted Rig JSON in one
transaction after validating every row; it does not modify UUIDs, timestamps,
the relational projection columns, photos, or other JSON values.
