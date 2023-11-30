module FSharpLint.Client.Contracts

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open FSharpLint.Application
open FSharpLint.Framework.Suggestion
open FSharp.Compiler.Text

[<RequireQualifiedAccess>]
module Methods =
    [<Literal>]
    let Version = "fsharplint/version"

    [<Literal>]
    let LintFile = "fsharplint/lintfile"

    [<Literal>]
    let FormatSelection = "fsharplint/formatSelection"

    [<Literal>]
    let Configuration = "fsharplint/configuration"

type LintFileRequest =
    {
        // OptionalLintParameters: OptionalLintParameters
        FilePath: string
        ParsedFileInfo: ParsedFileInformation option
    }

type FormatDocumentRequest =
    { SourceCode: string
      FilePath: string
      Config: IReadOnlyDictionary<string, string> option
      Cursor: FormatCursorPosition option }

    member this.IsSignatureFile = this.FilePath.EndsWith(".fsi", StringComparison.Ordinal)

and FormatCursorPosition =
    class
        val Line: int
        val Column: int

        new(line: int, column: int) = { Line = line; Column = column }
    end

type FormatSelectionRequest =
    {
        SourceCode: string
        /// File path will be used to identify the .editorconfig options
        /// Unless the configuration is passed
        FilePath: string
        /// Overrides the found .editorconfig.
        Config: IReadOnlyDictionary<string, string> option
        /// Range follows the same semantics of the FSharp Compiler Range type.
        Range: FormatSelectionRange
    }

    member this.IsSignatureFile = this.FilePath.EndsWith(".fsi", StringComparison.Ordinal)

and FormatSelectionRange =
    class
        val StartLine: int
        val StartColumn: int
        val EndLine: int
        val EndColumn: int

        new(startLine: int, startColumn: int, endLine: int, endColumn: int) =
            { StartLine = startLine
              StartColumn = startColumn
              EndLine = endLine
              EndColumn = endColumn }
    end

type SelectionRange =
    class
        val StartLine: int
        val StartColumn: int
        val EndLine: int
        val EndColumn: int
        new(startLine: int, startColumn: int, endLine: int, endColumn: int) =
            { StartLine = startLine
              StartColumn = startColumn
              EndLine = endLine
              EndColumn = endColumn }
    end

type SuggestedFixC = {
    /// Text to be replaced.
    FromText:string

    /// Location of the text to be replaced.
    FromRange:SelectionRange

    /// Text to replace the `FromText`, i.e. the fix.
    ToText:string
}

[<NoEquality; NoComparison>]
type WarningDetailsC = {
    /// Location of the code that prompted the suggestion.
    Range:SelectionRange

    /// Suggestion message to describe the possible problem to the user.
    Message:string

    /// Information to provide an automated fix.
    SuggestedFix:SuggestedFixC option
}

/// A lint "warning", sources the location of the warning with a suggestion on how it may be fixed.
[<NoEquality; NoComparison>]
type LintWarningC = {
    /// Unique identifier for the rule that caused the warning.
    RuleIdentifier:string

    /// Unique name for the rule that caused the warning.
    RuleName:string

    /// Path to the file where the error occurs.
    FilePath:string

    /// Text that caused the error (the `Range` of the content of `FileName`).
    ErrorText:string

    /// Details for the warning.
    Details:WarningDetailsC
}

type FSharpLintResponse = { 
    Code: int
    FilePath: string
    Content: string option
    Result: LintWarningC list 
}

type FSharpLintService =
    interface
        inherit IDisposable

        abstract member VersionAsync: filePath: string * ?cancellationToken: CancellationToken -> Task<FSharpLintResponse>

        abstract member LintFileAsync: LintFileRequest * ?cancellationToken: CancellationToken -> Task<FSharpLintResponse>
    end
