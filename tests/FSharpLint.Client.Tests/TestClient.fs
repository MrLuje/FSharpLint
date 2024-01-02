module FSharpLint.Client.Tests

open NUnit.Framework
open System.IO
open System
open Contracts
open LSPFSharpLintService
open LSPFSharpLintServiceTypes

let (</>) x y = Path.Combine(x, y)

let basePath = TestContext.CurrentContext.TestDirectory </> ".." </> ".." </> ".." </> ".." </> ".."
let fsharpLintConsoleDll = basePath </> "src" </> "FSharpLint.Console" </> "bin" </> "Release" </> "net6.0" </> "dotnet-fsharplint.dll"
let fsharpConsoleOutputDir = Path.GetFullPath (Path.GetDirectoryName(fsharpLintConsoleDll))

[<RequireQualifiedAccess>]
type ToolStatus = | Available | NotAvailable
type ToolLocationOverride(toolStatus: ToolStatus) =
    let tempFolder = Path.GetTempFileName()

    do match toolStatus with
       | ToolStatus.Available -> Environment.SetEnvironmentVariable("FSHARPLINT_SEARCH_PATH_OVERRIDE", fsharpConsoleOutputDir)
       | ToolStatus.NotAvailable -> 
            let path = Environment.GetEnvironmentVariable("PATH")
            // ensure bin dir is not in path
            if path.Contains(fsharpConsoleOutputDir, StringComparison.InvariantCultureIgnoreCase) then
                Assert.Inconclusive()

            File.Delete(tempFolder)
            Directory.CreateDirectory(tempFolder) |> ignore

            // set search path to an empty dir
            Environment.SetEnvironmentVariable("FSHARPLINT_SEARCH_PATH_OVERRIDE", tempFolder)
            
    interface IDisposable with
        member this.Dispose() =
            if File.Exists tempFolder then
                File.Delete tempFolder

let runVersionCall filePath (service: FSharpLintService) =
    async {
        let request = 
            {
                FilePath = filePath
            }
        let! version = service.VersionAsync(request) |> Async.AwaitTask
        return version
    }
    |> Async.RunSynchronously

let runLintFileCall filePath (service: FSharpLintService) =
    async {
        let request = 
            {
                FilePath = filePath
                LintConfigPath = None
            }
        let! lintResult = service.LintFileAsync(request) |> Async.AwaitTask
        return lintResult
    }
    |> Async.RunSynchronously

[<Test>]
let ``Daemon cannot be found``() =
    using (new ToolLocationOverride(ToolStatus.NotAvailable)) <| fun _ ->
    
        let testHintsFile = basePath </> "tests" </> "FSharpLint.FunctionalTest.TestedProject" </> "FSharpLint.FunctionalTest.TestedProject.NetCore" </> "TestHints.fs"
        let fsharpLintService: FSharpLintService = new LSPFSharpLintService() :> FSharpLintService
        let versionResponse = runVersionCall testHintsFile fsharpLintService
        
        Assert.AreEqual(LanguagePrimitives.EnumToValue FSharpLintResponseCode.ErrToolNotFound, versionResponse.Code)

[<Test>]
let ``Daemon answer with its version number``() =
    using (new ToolLocationOverride(ToolStatus.Available)) <| fun _ ->

        let testHintsFile = basePath </> "tests" </> "FSharpLint.FunctionalTest.TestedProject" </> "FSharpLint.FunctionalTest.TestedProject.NetCore" </> "TestHints.fs"
        let fsharpLintService: FSharpLintService = new LSPFSharpLintService() :> FSharpLintService
        let versionResponse = runVersionCall testHintsFile fsharpLintService

        match versionResponse.Result with
        | Content result -> Assert.IsFalse (String.IsNullOrWhiteSpace result)
        | _ -> Assert.Fail("Response should be a version number")

        Assert.AreEqual(LanguagePrimitives.EnumToValue FSharpLintResponseCode.OkCurrentDaemonVersion, versionResponse.Code)

[<Test>]
let ``Daemon cannot work with relative path``() =
    using (new ToolLocationOverride(ToolStatus.Available)) <| fun _ ->

        let testHintsFile = ".." </> "tests" </> "FSharpLint.FunctionalTest.TestedProject" </> "FSharpLint.FunctionalTest.TestedProject.NetCore" </> "TestHints.fs"
        let fsharpLintService: FSharpLintService = new LSPFSharpLintService() :> FSharpLintService
        let versionResponse = runVersionCall testHintsFile fsharpLintService
        
        Assert.AreEqual(LanguagePrimitives.EnumToValue FSharpLintResponseCode.ErrFilePathIsNotAbsolute, versionResponse.Code)

[<Test>]
let ``Daemon cannot work with non-existing file``() =
    using (new ToolLocationOverride(ToolStatus.Available)) <| fun _ ->

        let testHintsFile = basePath </> "tests" </> "FSharpLint.FunctionalTest.TestedProject" </> "FSharpLint.FunctionalTest.TestedProject.NetCore" </> "TestHintsOOOPS.fs"
        let fsharpLintService: FSharpLintService = new LSPFSharpLintService() :> FSharpLintService
        let versionResponse = runVersionCall testHintsFile fsharpLintService
        
        Assert.AreEqual(LanguagePrimitives.EnumToValue FSharpLintResponseCode.ErrFileNotFound, versionResponse.Code)

[<Test>]
let ``Daemon can lint a file with success``() =
    using (new ToolLocationOverride(ToolStatus.Available)) <| fun _ ->

        let testHintsFile = basePath </> "tests" </> "FSharpLint.FunctionalTest.TestedProject" </> "FSharpLint.FunctionalTest.TestedProject.NetCore" </> "TestHints.fs"
        let fsharpLintService: FSharpLintService = new LSPFSharpLintService() :> FSharpLintService
        let versionResponse = runLintFileCall testHintsFile fsharpLintService

        match versionResponse.Result with
        | Content result -> Assert.Fail("Should be a lint result")
        | LintResult warnings -> 
            Assert.IsNotEmpty warnings
            Assert.AreEqual(LanguagePrimitives.EnumToValue FSharpLintResponseCode.OkLint, versionResponse.Code)
