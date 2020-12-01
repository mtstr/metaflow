namespace Metaflow

open System
open System.Collections.Generic

module Monitoring =
    type ITelemetryClient =
        abstract TrackEvent: string -> seq<Event<'t>> -> unit
        abstract TrackException: Operation -> string -> Exception -> unit
        abstract TrackRequest: Operation -> string -> unit
