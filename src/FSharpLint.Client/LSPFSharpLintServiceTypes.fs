module FSharpLint.Client.LSPFSharpLintServiceTypes

open System
open System.Diagnostics
open StreamJsonRpc
open FSharpLint.Client.Contracts

type FSharpLintResponseCode =
    | Linted = 1
    | Error = 2
    | Ignored = 3
    | Version = 4
    | ToolNotFound = 5
    | FileNotFound = 6
    | Configuration = 7
    | FilePathIsNotAbsolute = 8
    | CancellationWasRequested = 9
    | DaemonCreationFailed = 10

// [<RequireQualifiedAccess>]
// type FormatSelectionResponse =
//     | Formatted of filename: string * formattedContent: string * formattedRange: FormatSelectionRange
//     | Error of filename: string * formattingError: string

//     member this.AsFormatResponse() =
//         match this with
//         | FormatSelectionResponse.Formatted(name, content, formattedRange) ->
//             { Code = int FSharpLintResponseCode.Formatted
//               FilePath = name
//               Content = Some content
//               SelectedRange = Some formattedRange
//               Cursor = None }
//         | FormatSelectionResponse.Error(name, ex) ->
//             { Code = int FSharpLintResponseCode.Error
//               FilePath = name
//               Content = Some ex
//               SelectedRange = None
//               Cursor = None }

// [<RequireQualifiedAccess>]
// type FormatDocumentResponse =
//     | Formatted of filename: string * formattedContent: string * cursor: FormatCursorPosition option
//     | Unchanged of filename: string
//     | Error of filename: string * formattingError: string
//     | IgnoredFile of filename: string

type FSharpLintVersion = FSharpLintVersion of string
type FSharpLintExecutableFile = FSharpLintExecutableFile of string
type Folder = Folder of path: string

[<RequireQualifiedAccess>]
type FSharpLintToolStartInfo =
    | LocalTool of workingDirectory: Folder
    | GlobalTool
    | ToolOnPath of executableFile: FSharpLintExecutableFile

type RunningFSharpLintTool =
    { Process: Process
      RpcClient: JsonRpc
      StartInfo: FSharpLintToolStartInfo }

    interface IDisposable with
        member this.Dispose() : unit =
            if not this.Process.HasExited then
                this.Process.Kill()

            this.Process.Dispose()
            this.RpcClient.Dispose()

[<RequireQualifiedAccess>]
type ProcessStartError =
    | ExecutableFileNotFound of
        executableFile: string *
        arguments: string *
        workingDirectory: string *
        pathEnvironmentVariable: string *
        error: string
    | UnExpectedException of executableFile: string * arguments: string * error: string

[<RequireQualifiedAccess>]
type DotNetToolListError =
    | ProcessStartError of ProcessStartError
    | ExitCodeNonZero of executableFile: string * arguments: string * exitCode: int * error: string

type FSharpLintToolFound = FSharpLintToolFound of version: FSharpLintVersion * startInfo: FSharpLintToolStartInfo

[<RequireQualifiedAccess>]
type FSharpLintToolError =
    | NoCompatibleVersionFound
    | DotNetListError of DotNetToolListError
