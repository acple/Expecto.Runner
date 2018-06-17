namespace Expecto.Runner

open System
open System.Collections.Generic
open System.Reflection
open System.Threading
open Expecto
open Expecto.Impl
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

type ExpectoTestCaseDiscoverySink (observer: IObserver<TestCase>, token: CancellationToken) =
    interface ITestCaseDiscoverySink with
        member __.SendTestCase(discoveredTest: TestCase) =
            token.ThrowIfCancellationRequested()
            observer.OnNext(discoveredTest)

[<ExtensionUri(Env.ExecutorUri)>]
type ExpectoTestExecutor () =
    let cancel = new CancellationTokenSource ()

    let runner (testCase: TestCase) =
        let token = cancel.Token
        token.ThrowIfCancellationRequested()

        let assembly = Assembly.LoadFrom(testCase.Source)
        let tests =
            match testFromAssembly assembly with
            | Some test -> test
            | None -> TestList ([], Normal)
            |> Test.filter ((=) testCase.DisplayName)

        token.ThrowIfCancellationRequested()

        let config = { defaultConfig with printer = TestPrinters.silent }

        let result = Async.RunSynchronously <| evalTestsWithCancel token config tests

        [ for _, summary in result do
          let outcome, message =
              match summary.result with
              | Passed -> TestOutcome.Passed, String.Empty
              | Ignored reason -> TestOutcome.Skipped, reason
              | Failed reason -> TestOutcome.Failed, reason
              | Error ex -> TestOutcome.Failed, ex.Message
          yield TestResult(testCase, Outcome = outcome, ErrorMessage = message, Duration = summary.duration)
        ]

    interface ITestExecutor with
        member __.RunTests
            ( tests: IEnumerable<string>
            , _runContext: IRunContext
            , frameworkHandle: IFrameworkHandle ) =
            let recorder = Seq.fold (fun _ -> frameworkHandle.RecordResult) ()
            let observer = AnonymousObserver (recorder << runner)
            let sink = ExpectoTestCaseDiscoverySink (observer, cancel.Token)
            let discoverer = ExpectoTestDiscoverer () :> ITestDiscoverer
            try
                discoverer.DiscoverTests(tests, null, frameworkHandle, sink)
            with
            | :? OperationCanceledException ->
                frameworkHandle.SendMessage(TestMessageLevel.Informational, "Operation canceled")
            | ex -> frameworkHandle.SendMessage(TestMessageLevel.Error, ex.ToString())

        member __.RunTests
            ( tests: IEnumerable<TestCase>
            , _runContext: IRunContext
            , frameworkHandle: IFrameworkHandle ) =
            try
                for test in tests do
                for result in runner test do
                    frameworkHandle.RecordResult(result)
            with
            | :? OperationCanceledException ->
                frameworkHandle.SendMessage(TestMessageLevel.Informational, "Operation canceled")
            | ex -> frameworkHandle.SendMessage(TestMessageLevel.Error, ex.ToString())

        member __.Cancel () = cancel.Cancel()
