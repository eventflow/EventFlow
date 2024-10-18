---
layout: default
title: Metadata
parent: Basics
nav_order: 2
---

# Metadata

Metadata is all the "additional" information that resides with a emitted
event, some of which is required information.

In EventFlow metadata is merely an `IEnumerable` of
`KeyValuePair<string,string>` for which each is a metadata entry.

Out of the box these metadata keys are added to each aggregate event.

-  `event_name` and `event_version` - A name and version for the
   event which is used during event deserialization.
-  `timestamp` - A `DateTimeOffset` for when the event was emitted
   from the aggregate.
-  `aggregate_sequence_number` - The version the aggregate was after
   the event was emitted, e.g. `1` for the very first event emitted.


## Custom metadata provider

If you require additional information to be stored along with each
event, then you can implement the `IMetadataProvider` interface and
register the class using e.g. `.AddMetadataProvider(...)` on
`EventFlowOptions`.

## Additional built-in providers

EventFlow ships with a collection of ready-to-use providers in some of
its NuGet packages.

### EventFlow

-  **AddEventTypeMetadataProvider**
-  `event_type_assembly_name` - Assembly name of the assembly
   containing the event
-  `event_type_assembly_version` - Assembly version of the assembly
   containing the event
-  `event_type_fullname` - Full name of the event corresponding to
   `Type.FullName` for the aggregate event type.
-  **AddGuidMetadataProvider**
-  `guid` - A new `Guid` for each event.
-  **AddMachineNameMetadataProvider**
-  `environment_machinename` - Adds the machine name handling the
   event from `Environment.MachineName`

### EventFlow.Owin

-  **AddRequestHeadersMetadataProvider**
-  `request_header[HEADER]` - Adds all headers from the OWIN request
   as metadata, each as a separate entry for which `HEADER` is
   replaced with the name of the header. E.g. the
   `request_header[Connection]` might contain the value
   `Keep-Alive`.
-  **AddUriMetadataProvider**
-  `request_uri` - OWIN request URI.
-  `request_method` - OWIN request method.
-  **AddUserHostAddressMetadataProvider**
-  `user_host_address` - The provider tries to find the correct user
   host address by inspecting request headers, i.e., if you have a load
   balancer in front of your application, then the request IP is not the
   real user address, but the load balancer should send the user IP as a
   header.
-  `user_host_address_source_header` - The header from which the
   user host address was taken.
-  `remote_ip_address` - The remote IP address. Note that this might
   be the IP address of your load balancer.
