# Metadata providers
In order to add extra information for each event, one or more metadata
providers must be used. A provider returns a `IEnumerable` of
`KeyValuePair<string,string>` for which each is considered metadata.

## Built-in providers
EventFlow ships with a collection of ready-to-use providers.

The following is a list of the metadata providers and the
metadata keys that the add for each event.

* **AddEventTypeMetadataProvider**
 * `event_type_assembly_name` - Assembly name of the assembly
   containing the event
 * `event_type_assembly_version` - Assembly version of the assembly
   containing the event
 * `event_type_fullname` - Full name of the event corresponding to
   `Type.FullName` for the aggregate event type.
* **AddGuidMetadataProvider**
 * `guid` - A new `Guid` for each event.
* **AddMachineNameMetadataProvider**
 * `environment_machinename` - Adds the machine name handling the
   event from `Environment.MachineName`
