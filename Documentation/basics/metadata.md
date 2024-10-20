---
layout: default
title: Metadata
parent: Basics
nav_order: 2
---

# Metadata

Metadata is all the "additional" information that resides with an emitted
event, some of which is required information.

In EventFlow, metadata is merely an `IEnumerable` of
`KeyValuePair<string,string>` for which each is a metadata entry.

Out of the box, these metadata keys are added to each aggregate event:

-  `event_name` and `event_version` - A name and version for the
   event which is used during event deserialization.
-  `timestamp` - A `DateTimeOffset` for when the event was emitted
   from the aggregate.
-  `aggregate_sequence_number` - The version the aggregate was after
   the event was emitted, e.g., `1` for the very first event emitted.

## Custom metadata provider

If you require additional information to be stored along with each
event, you can implement the `IMetadataProvider` interface and
register the class using e.g., `.AddMetadataProvider(...)` on
`EventFlowOptions`.

## Additional built-in providers

EventFlow ships with a collection of ready-to-use providers in some of
its NuGet packages.

### EventFlow

- **`AddEventTypeMetadataProvider`**

    * `event_type_assembly_name` - Assembly name of the assembly containing the event.
    * `event_type_assembly_version` - Assembly version of the assembly containing the event.
    * `event_type_fullname` - Full name of the event corresponding to `Type.FullName` for the aggregate event type.

- **`AddGuidMetadataProvider`**

    * `guid` - A new `Guid` for each event.

- **`AddMachineNameMetadataProvider`**

    * `environment_machinename` - Adds the machine name handling the event from `Environment.MachineName`.
