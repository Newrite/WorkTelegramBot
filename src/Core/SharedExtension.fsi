
/// Functions for Option type (functor and monad).
module Option

val string: optionValue: 'a option -> string

val ofResult: resultValue: Result<'a,'b> -> 'a option

module OptionComputationExpression

type OptionBuilder =
    
    new: unit -> OptionBuilder
    
    member Bind: x: 'j option * f: ('j -> 'k option) -> 'k option
    
    member Combine: a: unit option * b: (unit -> 'a option) -> 'a option
    
    member Delay: f: 'h -> 'h
    
    member For: sequence: seq<'b> * body: ('b -> unit option) -> unit option
    
    member Return: x: 'l -> 'l option
    
    member ReturnFrom: x: 'i -> 'i
    
    member Run: f: (unit -> 'g) -> 'g
    
    member TryFinally: body: (unit -> 'e) * compensation: (unit -> unit) -> 'e
    
    member TryWith: body: (unit -> 'f) * handler: (exn -> 'f) -> 'f
    
    member
      Using: disposable: 'c * body: ('c -> 'd) -> 'd
               when 'c :> System.IDisposable and 'c: null
    
    member
      While: guard: (unit -> bool) * body: (unit -> unit option) -> unit option
    
    member Zero: unit -> unit option

val option: OptionBuilder

/// Functions for Result type (functor and monad).
/// For applicatives, see Validation.
module Result

/// Pass in a function to handle each case of `Result`
val bimap:
  onSuccess: ('a -> 'b) -> onError: ('c -> 'b) -> xR: Result<'a,'c> -> 'b

val map: (('a -> 'b) -> Result<'a,'c> -> Result<'b,'c>)

val mapError: (('a -> 'b) -> Result<'c,'a> -> Result<'c,'b>)

val bind: (('a -> Result<'b,'c>) -> Result<'a,'c> -> Result<'b,'c>)

val iter: f: ('a -> unit) -> result: Result<'a,'b> -> unit

/// Apply a Result<fn> to a Result<x> monadically
val apply: fR: Result<('a -> 'b),'c> -> xR: Result<'a,'c> -> Result<'b,'c>

val sequence: aListOfResults: Result<'a,'b> list -> Result<'a list,'b>

/// Lift a two parameter function to use Result parameters
val lift2:
  f: ('a -> 'b -> 'c) -> x1: Result<'a,'d> -> x2: Result<'b,'d> -> Result<'c,'d>

/// Lift a three parameter function to use Result parameters
val lift3:
  f: ('a -> 'b -> 'c -> 'd) -> x1: Result<'a,'e> -> x2: Result<'b,'e>
  -> x3: Result<'c,'e> -> Result<'d,'e>

/// Lift a four parameter function to use Result parameters
val lift4:
  f: ('a -> 'b -> 'c -> 'd -> 'e) -> x1: Result<'a,'f> -> x2: Result<'b,'f>
  -> x3: Result<'c,'f> -> x4: Result<'d,'f> -> Result<'e,'f>

/// Apply a monadic function with two parameters
val bind2:
  f: ('a -> 'b -> Result<'c,'d>) -> x1: Result<'a,'d> -> x2: Result<'b,'d>
    -> Result<'c,'d>

/// Apply a monadic function with three parameters
val bind3:
  f: ('a -> 'b -> 'c -> Result<'d,'e>) -> x1: Result<'a,'e> -> x2: Result<'b,'e>
  -> x3: Result<'c,'e> -> Result<'d,'e>

/// Predicate that returns true on success
val isOk: _arg1: Result<'a,'b> -> bool

/// Predicate that returns true on failure
val isError: xR: Result<'a,'b> -> bool

/// Lift a given predicate into a predicate that works on Results
val filter: pred: ('a -> bool) -> _arg1: Result<'a,'b> -> bool

/// On success, return the value. On error, return a default value
val ifError: defaultVal: 'a -> _arg1: Result<'a,'b> -> 'a

/// Apply a monadic function to an Result<x option>
val bindOption:
  f: ('a -> Result<'b,'c>) -> xR: 'a option -> Result<'b option,'c>

/// Convert an Option into a Result. If none, use the passed-in errorValue
val ofOption: errorValue: 'a -> opt: 'b option -> Result<'b,'a>

/// Convert a Result into an Option
val toOption: xR: Result<'a,'b> -> 'a option

/// Convert the Error case into an Option
/// (useful with List.choose to find all errors in a list of Results)
val toErrorOption: _arg1: Result<'a,'b> -> 'b option

module ResultComputationExpression

type ResultBuilder =
    
    new: unit -> ResultBuilder
    
    member Bind: x: Result<'n,'o> * f: ('n -> Result<'p,'o>) -> Result<'p,'o>
    
    member
      Combine: a: Result<unit,'a> * b: (unit -> Result<'b,'a>) -> Result<'b,'a>
    
    member Delay: f: 'k -> 'k
    
    member
      For: sequence: seq<'c> * body: ('c -> Result<unit,'d>) -> Result<unit,'d>
    
    member Return: x: 'q -> Result<'q,'r>
    
    member ReturnFrom: x: 'm -> 'm
    
    member Run: f: (unit -> 'j) -> 'j
    
    member TryFinally: body: (unit -> 'g) * compensation: (unit -> unit) -> 'g
    
    member TryWith: body: (unit -> 'h) * handler: (exn -> 'h) -> 'h
    
    member
      Using: disposable: 'e * body: ('e -> 'f) -> 'f
               when 'e :> System.IDisposable and 'e: null
    
    member
      While: guard: (unit -> bool) * body: (unit -> Result<unit,'i>)
               -> Result<unit,'i>
    
    member Zero: unit -> Result<unit,'l>

val result: ResultBuilder

[<Struct>]
type Validation<'Success,'Failure> = Result<'Success,'Failure list>

/// Functions for the `Validation` type (mostly applicative)
module Validation

/// Alias for Result.Map
val map: (('a -> 'b) -> Result<'a,'c> -> Result<'b,'c>)

/// Apply a Validation<fn> to a Validation<x> applicatively
val apply:
  fV: Validation<('a -> 'b),'c> -> xV: Validation<'a,'c> -> Validation<'b,'c>

val sequence:
  aListOfValidations: Validation<'a,'b> list -> Validation<'a list,'b>

val ofResult: xR: Result<'a,'b> -> Validation<'a,'b>

val toResult: xV: Validation<'a,'b> -> Result<'a,'b list>

module Async

/// Lift a function to Async
val map: f: ('a -> 'b) -> xA: Async<'a> -> Async<'b>

/// Lift a value to Async
val retn: x: 'a -> Async<'a>

/// Apply an Async function to an Async value
val apply: fA: Async<('a -> 'b)> -> xA: Async<'a> -> Async<'b>

/// Apply a monadic function to an Async value
val bind: f: ('a -> Async<'b>) -> xA: Async<'a> -> Async<'b>

type AsyncResult<'Success,'Failure> = Async<Result<'Success,'Failure>>

module AsyncResult

/// Lift a function to AsyncResult
val map: f: ('a -> 'b) -> x: AsyncResult<'a,'c> -> AsyncResult<'b,'c>

/// Lift a function to AsyncResult
val mapError: f: ('a -> 'b) -> x: AsyncResult<'c,'a> -> AsyncResult<'c,'b>

/// Apply ignore to the internal value
val ignore: x: AsyncResult<'a,'b> -> AsyncResult<unit,'b>

/// Lift a value to AsyncResult
val retn: x: 'a -> AsyncResult<'a,'b>

/// Handles asynchronous exceptions and maps them into Failure cases using the provided function
val catch: f: (exn -> 'a) -> x: AsyncResult<'b,'a> -> AsyncResult<'b,'a>

/// Apply an AsyncResult function to an AsyncResult value, monadically
val applyM:
  fAsyncResult: AsyncResult<('a -> 'b),'c> -> xAsyncResult: AsyncResult<'a,'c>
    -> AsyncResult<'b,'c>

/// Apply an AsyncResult function to an AsyncResult value, applicatively
val applyA:
  fAsyncResult: AsyncResult<('a -> 'b),'c list>
  -> xAsyncResult: AsyncResult<'a,'c list> -> AsyncResult<'b,'c list>

/// Apply a monadic function to an AsyncResult value
val bind:
  f: ('a -> AsyncResult<'b,'c>) -> xAsyncResult: AsyncResult<'a,'c>
    -> AsyncResult<'b,'c>

/// Convert a list of AsyncResult into a AsyncResult<list> using monadic style.
/// Only the first error is returned. The error type need not be a list.
val sequenceM: resultList: AsyncResult<'a,'b> list -> AsyncResult<'a list,'b>

/// Convert a list of AsyncResult into a AsyncResult<list> using applicative style.
/// All the errors are returned. The error type must be a list.
val sequenceA:
  resultList: AsyncResult<'a,'b list> list -> AsyncResult<'a list,'b list>

/// Lift a value into an Ok inside a AsyncResult
val ofSuccess: x: 'a -> AsyncResult<'a,'b>

/// Lift a value into an Error inside a AsyncResult
val ofError: x: 'a -> AsyncResult<'b,'a>

/// Lift a Result into an AsyncResult
val ofResult: x: Result<'a,'b> -> AsyncResult<'a,'b>

/// Lift a Async into an AsyncResult
val ofAsync: x: Async<'a> -> AsyncResult<'a,'b>

val sleep: ms: int -> AsyncResult<unit,'a>

/// The `asyncResult` computation expression is available globally without qualification
module AsyncResultComputationExpression

type AsyncResultBuilder =
    
    new: unit -> AsyncResultBuilder
    
    member
      Bind: asyncResult: AsyncResult<'c,'d> * f: ('c -> AsyncResult<'e,'d>)
              -> AsyncResult<'e,'d>
    
    member
      Combine: computation1: AsyncResult<unit,'TError> *
               computation2: AsyncResult<'U,'TError> -> AsyncResult<'U,'TError>
    
    member
      Delay: generator: (unit -> AsyncResult<'T,'TError>)
               -> AsyncResult<'T,'TError>
    
    member
      For: sequence: #seq<'T> * binder: ('T -> AsyncResult<unit,'TError>)
             -> AsyncResult<unit,'TError>
    
    member Return: result: 'f -> AsyncResult<'f,'g>
    
    member ReturnFrom: asyncResult: 'b -> 'b
    
    member
      TryFinally: computation: AsyncResult<'T,'TError> *
                  compensation: (unit -> unit) -> AsyncResult<'T,'TError>
    
    member
      TryWith: computation: AsyncResult<'T,'TError> *
               handler: (System.Exception -> AsyncResult<'T,'TError>)
                 -> AsyncResult<'T,'TError>
    
    member
      Using: resource: 'T * binder: ('T -> AsyncResult<'U,'TError>)
               -> AsyncResult<'U,'TError> when 'T :> System.IDisposable
    
    member
      While: guard: (unit -> bool) * computation: AsyncResult<unit,'TError>
               -> AsyncResult<unit,'TError>
    
    member Zero: unit -> AsyncResult<unit,'TError>

val asyncResult: AsyncResultBuilder

