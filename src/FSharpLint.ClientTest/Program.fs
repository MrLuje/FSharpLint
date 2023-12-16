open FSharpLint.Client.Contracts
open FSharpLint.Client.LSPFSharpLintService
open System.Threading
open System.Diagnostics
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharpLint.Framework
open System
open System.IO

let testFile = "/home/vince/src/github/mrluje/FSharpLint/src/FSharpLint.ClientTest/Program.fs"

let generateAst source =
    let checker = FSharpChecker.Create(keepAssemblyContents=true)
    let sourceText = SourceText.ofString source

    let options = ParseFile.getProjectOptionsFromScript checker testFile source

    let parseResults =
        checker.ParseFile(testFile, sourceText, options |> checker.GetParsingOptionsFromProjectOptions |> fst)
        |> Async.RunSynchronously

    parseResults.ParseTree

let fsharpLintService: FSharpLintService = new LSPFSharpLintService() :> FSharpLintService
async {
    let path = Environment.GetEnvironmentVariable("PATH")
    // ensure current FSharpLint.Console output is in PATH
    Environment.SetEnvironmentVariable("PATH", Path.GetFullPath $"../../../../../src/FSharpLint.Console/bin/Release/net6.0:{path}")
    
    if Environment.GetCommandLineArgs() |> Array.contains "--debug" then
        Console.WriteLine("Waiting for debugger...")
        while not Debugger.IsAttached do
            Thread.Sleep ((TimeSpan.FromSeconds 1).Milliseconds)
        Console.WriteLine("Attached !!")

    let! version = fsharpLintService.VersionAsync({ FilePath = testFile; ProjectPath = None }, CancellationToken.None) |> Async.AwaitTask
    Debug.Assert(version.Code = (int)FSharpLint.Client.LSPFSharpLintServiceTypes.FSharpLintResponseCode.Version)

    // let warns = FSharpLint.Application.Lint.lintFile OptionalLintParameters.Default testFile
    
    let r = (fsharpLintService.LintFileAsync({
        FilePath = testFile
        ProjectPath = None
        LintConfigPath = None
    }).Result)
    Debug.Assert(r.Code = (int)FSharpLint.Client.LSPFSharpLintServiceTypes.FSharpLintResponseCode.Linted)
    printfn $"LintResults for %s{testFile}: %A{r.Result}"

    return version
}
|> Async.RunSynchronously
|> ignore
