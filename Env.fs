namespace Expecto.Runner

open System.Reflection
open Expecto

module internal Env =
    [<Literal>]
    let ExecutorUri = "executor://Expecto.Runner"

    let runner = Assembly.GetExecutingAssembly()

    let expecto = Assembly.GetAssembly(typeof<ExpectoException>).FullName
