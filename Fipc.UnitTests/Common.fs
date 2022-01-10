namespace Fipc.UnitTests

open System
open System.IO
open System.Security.Cryptography
open Fipc.Core
open Fipc.Core.Common
open Microsoft.VisualStudio.TestTools.UnitTesting

module Common =
    
    
    [<TestClass>]
    type CoreTests () =

        [<TestMethod>]
        member this.``Message header parsing with random correlator.`` () =
            let expected = FipcMessageHeader.Create(10, RandomNumberGenerator.GetBytes(8))
            match FipcMessageHeader.TryParse(expected.Serialize(), true) with
            | Ok actual -> Assert.AreEqual(expected, actual)
            | Error e -> Assert.Fail(e)
            
            
        [<TestMethod>]      
        member this.``E2E message test with fake stream.`` () =
            let expected = FipcMessage.StringMessage("Hello, World!")
            
            use ms = new MemoryStream()
            Operations.tryWriteMessage ms expected
            |> Result.bind (fun _ ->
                ms.Seek(0, SeekOrigin.Begin) |> ignore
                Operations.tryReadMessage FipcContentType.Text ms)
            |> fun r ->
                match r with
                | Ok actual -> Assert.AreEqual(expected, actual)
                | Error e -> Assert.Fail(e)