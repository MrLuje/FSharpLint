module FSharpLint.Console.Daemon

open System
open System.Diagnostics
open System.IO
open System.IO.Abstractions
open System.Threading
open System.Threading.Tasks
open StreamJsonRpc
open FSharpLint.Client.Contracts
open FSharpLint.Core.Version
open FSharp.Core
open FSharpLint.Application
open Newtonsoft.Json

type FSharpLintDaemon(sender: Stream, reader: Stream) as this =
    let rpc: JsonRpc = JsonRpc.Attach(sender, reader, this)
    let traceListener = new DefaultTraceListener()

    do
        // hook up request/response logging for debugging
        rpc.TraceSource <- TraceSource(typeof<FSharpLintDaemon>.Name, SourceLevels.Verbose)
        rpc.TraceSource.Listeners.Add traceListener |> ignore<int>

    let disconnectEvent = new ManualResetEvent(false)

    let exit () = disconnectEvent.Set() |> ignore

    let fs = FileSystem()

    do rpc.Disconnected.Add(fun _ -> exit ())

    interface IDisposable with
        member this.Dispose() =
            traceListener.Dispose()
            disconnectEvent.Dispose()

    /// returns a hot task that resolves when the stream has terminated
    member this.WaitForClose = rpc.Completion

    [<JsonRpcMethod(Methods.Version)>]
    member _.Version() : string = fsharpLintVersion

    [<JsonRpcMethod(Methods.LintFile)>]
    member _.LintFile(request: LintFileRequest) : LintWarningC list = 
        let r = Lint.lintFile (Lint.OptionalLintParameters.Default) (request.FilePath)
        match r with
        | LintResult.Success warnings ->
            let result = 
                warnings
                |> List.map(fun w -> {
                    ErrorText = w.ErrorText
                    FilePath = w.FilePath
                    RuleIdentifier = w.RuleIdentifier
                    RuleName = w.RuleName
                    Details = {
                        Range = SelectionRange(w.Details.Range.StartLine, w.Details.Range.StartColumn, w.Details.Range.EndLine, w.Details.Range.EndColumn)
                        Message = w.Details.Message
                        SuggestedFix = 
                            w.Details.SuggestedFix
                            |> Option.bind(fun fix -> fix.Value)
                            |> Option.map(fun fix -> {
                                FromRange = SelectionRange(fix.FromRange.StartLine, fix.FromRange.StartColumn, fix.FromRange.EndLine, fix.FromRange.EndColumn)
                                FromText = fix.FromText
                                ToText = fix.ToText
                            })
                    }
                })
            Debug.Assert (JsonConvert.SerializeObject result <> "")
            result
        | LintResult.Failure _ -> []
