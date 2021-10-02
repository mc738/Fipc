// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO.Pipes
open System.Security.Principal
open Fipc.Core

// Define a function to construct a message to print
let from whom =
    sprintf "from %s" whom

[<EntryPoint>]
let main argv =
    Client.connection "testpipe"
    0 // return an integer exit code