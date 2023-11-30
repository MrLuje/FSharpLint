open FSharpLint.Client
open FSharpLint.Client.Contracts
open FSharpLint.Client.LSPFSharpLintService
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharpLint.Framework
open System.IO
open FSharpLint.Application.Lint
open Newtonsoft.Json
open Newtonsoft.Json
open System

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
    let! version = fsharpLintService.VersionAsync(testFile, CancellationToken.None) |> Async.AwaitTask
    // let version = {| Code = 5 |}
    // Debug.Assert(version.Code = 5)

    let warns = FSharpLint.Application.Lint.lintFile OptionalLintParameters.Default testFile
    
    let jsonFormatter = new JsonSerializerSettings()
    // jsonFormatter.ReferenceLoopHandling <- ReferenceLoopHandling.Ignore
    // jsonFormatter.PreserveReferencesHandling <- PreserveReferencesHandling.Objects
    
    // let r = Range.Zero
    // let str = JsonConvert.SerializeObject(r, jsonFormatter)

    // match warns with
    // | LintResult.Success w -> 
    //     let ww = List.head w
    //     let s = JsonConvert.SerializeObject(ww.Details.TypeChecks, jsonFormatter)
    //     let s = JsonConvert.SerializeObject(ww.Details.SuggestedFix, jsonFormatter)
    //     let s = JsonConvert.SerializeObject(ww.Details.Range, jsonFormatter)
    //     let s = JsonConvert.SerializeObject(ww.Details.Message, jsonFormatter)
    //     let s = JsonConvert.SerializeObject(ww.Details, jsonFormatter)
    //     let s = JsonConvert.SerializeObject(ww, jsonFormatter)

    //     JsonConvert.SerializeObject([w[0]], jsonFormatter) |> ignore
    //     JsonConvert.SerializeObject([w[1]], jsonFormatter) |> ignore
    //     JsonConvert.SerializeObject([w[2]], jsonFormatter) |> ignore

    //     let arr = Array.init (List.length w) (fun i -> w[i])
    //     JsonConvert.SerializeObject(arr, jsonFormatter) |> ignore
    //     s |> ignore
    // | _ -> failwith "arf"

    // let str = JsonConvert.SerializeObject(warns, jsonFormatter)
    // let s = JsonSerializer.Serialize warns
    
    let fileContent = File.ReadAllText testFile

    Console.WriteLine("Waiting for input...")
    Console.Read() |> ignore

    let r = (fsharpLintService.LintFileAsync({
        FilePath = testFile
        ParsedFileInfo = None
        // ParsedFileInfo = {
        //     Ast = generateAst fileContent
        //     Source = fileContent
        //     TypeCheckResults = None
        // }
    }).Result)
    return version
}
|> Async.RunSynchronously
|> ignore
