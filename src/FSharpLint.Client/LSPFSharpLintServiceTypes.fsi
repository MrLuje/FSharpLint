module FSharpLint.Client.LSPFSharpLintServiceTypes

open FSharpLint.Client.Contracts

type FSharpLintResponseCode =
    | Linted = 1
    | Error = 2
    | Ignored = 3
    | Version = 4
    | ToolNotFound = 5
    | FileNotFound = 6
    | FilePathIsNotAbsolute = 7
    | CancellationWasRequested = 8
    | DaemonCreationFailed = 9

// [<RequireQualifiedAccess>]
// type FormatSelectionResponse =
//     | Formatted of filename: string * formattedContent: string * formattedRange: FormatSelectionRange
//     | Error of filename: string * formattingError: string

//     member AsFormatResponse: unit -> FSharpLintResponse

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
    { Process: System.Diagnostics.Process
      RpcClient: StreamJsonRpc.JsonRpc
      StartInfo: FSharpLintToolStartInfo }

    interface System.IDisposable

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
