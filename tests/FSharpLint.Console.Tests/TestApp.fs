module TestApp

open System
open System.IO
open System.Threading
open NUnit.Framework
open FSharpLint.Client.LSPFSharpLintService
open FSharpLint.Client.Contracts
open FSharpLint.Client.LSPFSharpLintServiceTypes

let getErrorsFromOutput (output:string) =
    let splitOutput = output.Split([|Environment.NewLine|], StringSplitOptions.None)

    set [ for i in 1..splitOutput.Length - 1 do
            if splitOutput.[i].StartsWith "Error" then yield splitOutput.[i - 1] ]

type TemporaryFile(config, extension) =
    let filename = Path.ChangeExtension(Path.GetTempFileName(), extension)
    do
        File.WriteAllText(filename, config)

    member __.FileName = filename

    interface System.IDisposable with
        member __.Dispose() =
            File.Delete(filename)

let main input =
    use stdout = new StringWriter()
    let existing = Console.Out
    Console.SetOut(stdout)
    try
        let returnCode = FSharpLint.Console.Program.main input
        (returnCode, getErrorsFromOutput <| stdout.ToString())
    finally
        Console.SetOut(existing)

[<TestFixture>]
type TestConsoleApplication() =
    [<Test>]
    member __.``Lint file, expected rules are triggered.``() =
        let config = """
        type Signature =
            abstract member Encoded : string
            abstract member PathName : string
        """
        use input = new TemporaryFile(config, "fs")

        let (returnCode, errors) = main [| "lint"; input.FileName |]

        Assert.AreEqual(-1, returnCode)
        Assert.AreEqual(set ["Consider changing `Signature` to be prefixed with `I`."], errors)

    [<Test>]
    member __.``Lint source without any config, rule enabled in default config is triggered for given source.``() =
        let input = """
        type Signature =
            abstract member Encoded : string
            abstract member PathName : string
        """

        let (returnCode, errors) = main [| "lint"; input |]

        Assert.AreEqual(-1, returnCode)
        Assert.AreEqual(set ["Consider changing `Signature` to be prefixed with `I`."], errors)

    [<Test>]
    member __.``Lint source with valid config to disable rule, disabled rule is not triggered for given source.``() =
        let config = """
        {
            "InterfaceNames": {
                "enabled": false
            }
        }
        """
        use config = new TemporaryFile(config, "json")

        let input = """
        type Signature =
            abstract member Encoded : string
            abstract member PathName : string
        """

        let (returnCode, errors) = main [| "lint"; "--lint-config"; config.FileName; input |]

        Assert.AreEqual(0, returnCode)
        Assert.AreEqual(Set.empty, errors)
        
    [<Test>]
    member __.``Lint source with error suppressed, no error is given.``() =
        let input = """
        // fsharplint:disable-next-line
        type Signature =
            abstract member Encoded : string
            abstract member PathName : string
        """
        
        let (returnCode, errors) = main [| "lint"; input |]
        
        Assert.AreEqual(0, returnCode)
        Assert.AreEqual(Set.empty, errors)
        
    [<Test>]
    member __.``Get version from Daemon mode``() =
        let path = Environment.GetEnvironmentVariable("PATH")
        // ensure current FSharpLint.Console output is in PATH
        Environment.SetEnvironmentVariable("PATH", Path.GetFullPath $"../../../../../src/FSharpLint.Console/bin/Release/net6.0:{path}")

        use input = new TemporaryFile(String.Empty, "fs")
        let fsharpLintService: FSharpLintService = new LSPFSharpLintService() :> FSharpLintService
        let versionResponse = 
            async {
                let request = 
                    {
                        FilePath = input.FileName
                        ProjectPath = None
                    }
                let! version = fsharpLintService.VersionAsync(request, CancellationToken.None) |> Async.AwaitTask
                return version
            }
            |> Async.RunSynchronously
        
        Assert.AreEqual(LanguagePrimitives.EnumToValue FSharpLintResponseCode.Version, versionResponse.Code)
