namespace Expecto.Runner

open System
open System.Collections.Generic
open System.Reflection
open Expecto
open Expecto.Impl
open Expecto.Test
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

[<FileExtension ".dll">]
[<FileExtension ".exe">]
[<DefaultExecutorUri(Env.ExecutorUri)>]
type ExpectoTestDiscoverer () =
    interface ITestDiscoverer with
        member __.DiscoverTests
            ( sources: IEnumerable<string>
            , _discoveryContext: IDiscoveryContext
            , logger: IMessageLogger
            , discoverySink: ITestCaseDiscoverySink ) =
            let testCases =
                query {
                    for source in sources do
                    let assembly = Assembly.LoadFrom(source)
                    where (assembly.FullName <> Env.runner)
                    where (assembly.GetReferencedAssemblies() |> Seq.exists (fun x -> x.FullName = Env.expecto))
                    let tests =
                        match testFromAssembly assembly with
                        | Some test -> test
                        | None -> TestList ([], Normal)
                    for test in toTestCodeList tests do
                    select (TestCase (test.name, Uri (Env.ExecutorUri), source))
                }
            try
                for testCase in testCases do
                    discoverySink.SendTestCase(testCase)
            with
            | ex -> logger.SendMessage(TestMessageLevel.Error, ex.ToString())
