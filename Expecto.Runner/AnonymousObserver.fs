namespace Expecto.Runner

open System

type AnonymousObserver<'a> (onNext: 'a -> unit, onError: exn -> unit, onCompleted: unit -> unit) =
    new (onNext, onError) = AnonymousObserver (onNext, onError, id)

    new (onNext, onCompleted) = AnonymousObserver (onNext, raise, onCompleted)

    new (onNext) = AnonymousObserver (onNext, raise)

    interface IObserver<'a> with
        member __.OnNext(value: 'a) = onNext value

        member __.OnError(error: Exception) = onError error

        member __.OnCompleted() = onCompleted ()
