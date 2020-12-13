namespace Metaflow

open System

module Monitoring =
    type ITelemetryClient =
        abstract TrackEvent: string -> seq<Event<'t>> -> unit
        abstract TrackException: Operation -> string -> Exception -> unit
        abstract TrackRequest: Operation -> string -> unit
