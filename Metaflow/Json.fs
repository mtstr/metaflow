namespace Metaflow

open System.Text.Json.Serialization
open System.Text.Json

module Json =
    let converter =
        JsonFSharpConverter
            (unionTagCaseInsensitive = true,
             unionEncoding =
                 (JsonUnionEncoding.ExternalTag
                  ||| JsonUnionEncoding.NamedFields
                  ||| JsonUnionEncoding.UnwrapFieldlessTags
                  ||| JsonUnionEncoding.UnwrapOption))


    let options =
        let options = JsonSerializerOptions()
        options.Converters.Add(converter)
        options.IgnoreNullValues <- true
        options.PropertyNameCaseInsensitive <- true
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        options

