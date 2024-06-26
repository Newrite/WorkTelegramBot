namespace global

// Some extensions from https://github.com/demystifyfp/FsToolkit.ErrorHandling
// Base Agent type from https://gist.github.com/gsomix/9e1c3c8c24d77447143a6ccf5518af2f

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Channels
open System.Collections.Generic

[<AutoOpen>]
module Operators =
  let inline (^) f x = f x

[<RequireQualifiedAccess>]
module private Channel =

  let tryReadAsync (mailbox: ChannelReader<'Msg>) (token: CancellationToken) =
    task {
      try
        let! msg = mailbox.ReadAsync(token)
        return ValueSome msg
      with :? OperationCanceledException ->
        return ValueNone
    }

[<Struct; NoEquality; NoComparison>]
[<RequireQualifiedAccess>]
type private AgentConstructor<'Msg> =
| WithAgentInjected of BodyInjected: (Agent<'Msg> -> 'Msg -> Task<unit>)
| Default of Body: ('Msg -> Task<unit>)

and Agent<'Msg> private (agentCtor: AgentConstructor<'Msg>) as self =
  let cts = new CancellationTokenSource()
  let options = UnboundedChannelOptions(SingleReader = true)
  let mailbox: Channel<'Msg> = Channel.CreateUnbounded<'Msg>(options)

  let body =
    match agentCtor with
    | AgentConstructor.WithAgentInjected body -> body self
    | AgentConstructor.Default body -> body
  
  let loop (mailbox: ChannelReader<'Msg>) (token: CancellationToken) =
    task {
      try
        while not token.IsCancellationRequested do
          match! Channel.tryReadAsync mailbox token with
          | ValueNone -> ()
          | ValueSome msg -> do! body msg
      with ex ->
        eprintfn $"Mailbox error: {ex}"
    }
  
  member _.Start() =
    Task.Run<unit>(fun _ -> loop mailbox.Reader cts.Token) |> ignore
 
  member _.Post(item) = mailbox.Writer.TryWrite(item) |> ignore

  member _.PostAndReplyAsync (buildMessage: TaskCompletionSource<_> -> 'Msg) =
    let tcs = TaskCompletionSource<_>()
    mailbox.Writer.TryWrite(buildMessage tcs) |> ignore
    tcs.Task

  member self.PostAndReply (buildMessage: TaskCompletionSource<_> -> 'Msg) =
    let result = self.PostAndReplyAsync buildMessage
    result.Result

  interface IDisposable with
    member _.Dispose() = cts.Cancel()

  static member MakeDefault (body: 'Msg -> Task<unit>) =
    new Agent<'Msg>(AgentConstructor.Default body)

  static member MakeAndStartDefault (body: 'Msg -> Task<unit>) =
    let agent = Agent.MakeDefault body
    agent.Start()
    agent

  static member MakeInjected (body: Agent<'Msg> -> 'Msg -> Task<unit>) =
    new Agent<'Msg>(AgentConstructor.WithAgentInjected body)

  static member MakeAndStartInjected (body: Agent<'Msg> -> 'Msg -> Task<unit>) =
    let agent = Agent.MakeInjected body
    agent.Start()
    agent

[<RequireQualifiedAccess>]
module Agent =
  
  let post item (agent: Agent<_>) =
    agent.Post item

  let postAndReply item (agent: Agent<_>) =
    agent.PostAndReply(item)

  let postAndReplyAsync item (agent: Agent<_>) =
    agent.PostAndReplyAsync(item)

module ChannelCollections =
  
  [<Struct>]
  [<NoComparison>]
  [<RequireQualifiedAccess>]
  type private ChannelDictionaryMessage<'Key, 'Value> =
    | AddOrUpdate of addKey:'Key * addValue:'Value
    | Remove of removeKey:'Key
    | Get of key:'Key * getTcs:TaskCompletionSource<'Value>
    | Values of valueTcs:TaskCompletionSource<Dictionary.ValueCollection<'Key, 'Value>>
    | Keys of keysTcs:TaskCompletionSource<Dictionary.KeyCollection<'Key, 'Value>>
    | ContainsKey of containKey:'Key * keyTcs:TaskCompletionSource<bool>
    | TryGet of tryKey:'Key * tryGetTcs:TaskCompletionSource<'Value ValueOption>
    | ToList of listTcs:TaskCompletionSource<list<'Key * 'Value>>
  
  type ChannelDictionary<'Key, 'Value when 'Key : equality>() = 
    let cts = new CancellationTokenSource()
    let options = UnboundedChannelOptions(SingleReader = true)
    let mailbox: Channel<ChannelDictionaryMessage<'Key, 'Value>> =
      Channel.CreateUnbounded<ChannelDictionaryMessage<'Key, 'Value>>(options)
    let dict = Dictionary<'Key, 'Value>()
  
    let body msg = task {
      match msg with
      | ChannelDictionaryMessage.ToList tcs -> 
        if dict.Count > 0 then
          tcs.SetResult [ for keyValue in dict do yield (keyValue.Key, keyValue.Value)]
        else 
          tcs.SetResult [ ]
      | ChannelDictionaryMessage.AddOrUpdate (key, value) -> dict[key] <- value
      | ChannelDictionaryMessage.Remove key -> dict.Remove(key) |> ignore
      | ChannelDictionaryMessage.Get (key, tcs) ->
        try
          let value = dict[key]
          tcs.SetResult value
        with ex ->
          tcs.SetException ex
      | ChannelDictionaryMessage.TryGet (key, tcs) ->
        if dict.ContainsKey(key) then
          dict[key] |> ValueSome |> tcs.SetResult
        else
          ValueNone |> tcs.SetResult
      | ChannelDictionaryMessage.Values tcs -> tcs.SetResult dict.Values
      | ChannelDictionaryMessage.Keys tcs -> tcs.SetResult dict.Keys
      | ChannelDictionaryMessage.ContainsKey (key, tcs) -> dict.ContainsKey(key) |> tcs.SetResult
    }
  
    let loop (mailbox: ChannelReader<ChannelDictionaryMessage<'Key, 'Value>>) (token: CancellationToken) =
      task {
        try
          while not token.IsCancellationRequested do
            match! Channel.tryReadAsync mailbox token with
            | ValueNone -> ()
            | ValueSome msg -> do! body msg
        with 
          | :? OperationCanceledException ->
            dict.Clear()
          | ex ->
            eprintfn $"Channel dictionary error: {ex}"
      }
  
    do
      Task.Run<unit>(fun _ -> loop mailbox.Reader cts.Token) |> ignore
  
    member _.AddOrUpdate(key, value) = 
      mailbox.Writer.TryWrite(ChannelDictionaryMessage.AddOrUpdate(key, value)) |> ignore
  
    member _.Remove(key) = 
      mailbox.Writer.TryWrite(ChannelDictionaryMessage.Remove key) |> ignore
  
    member _.GetAsync(key) =
      let tcs = new TaskCompletionSource<'Value>()
      mailbox.Writer.TryWrite(ChannelDictionaryMessage.Get(key, tcs)) |> ignore
      tcs.Task
  
    member _.TryGetAsync(key) =
      let tcs = new TaskCompletionSource<'Value ValueOption>()
      mailbox.Writer.TryWrite(ChannelDictionaryMessage.TryGet(key, tcs)) |> ignore
      tcs.Task
  
    member _.ValuesAsync() =
      let tcs = new TaskCompletionSource<Dictionary.ValueCollection<'Key, 'Value>>()
      mailbox.Writer.TryWrite(ChannelDictionaryMessage.Values tcs) |> ignore
      tcs.Task
  
    member _.KeysAsync() =
      let tcs = new TaskCompletionSource<Dictionary.KeyCollection<'Key, 'Value>>()
      mailbox.Writer.TryWrite(ChannelDictionaryMessage.Keys tcs) |> ignore
      tcs.Task

    member _.ToListAsync() =
      let tcs = new TaskCompletionSource<list<'Key * 'Value>>()
      mailbox.Writer.TryWrite(ChannelDictionaryMessage.ToList tcs) |> ignore
      tcs.Task
  
    member self.Item
      with get(key) = self.GetAsync(key)
  
  
    interface IDisposable with
      member _.Dispose() = cts.Cancel()
      

  [<RequireQualifiedAccess>]
  module ChannelDictionary =
    
    let remove key (dict: ChannelDictionary<_,_>) = dict.Remove(key)
    let addOrUpdate key value (dict: ChannelDictionary<_,_>) = dict.AddOrUpdate(key, value)
    let getAsync key (dict: ChannelDictionary<_,_>) = dict.GetAsync(key)
    let tryGetAsync key (dict: ChannelDictionary<_,_>) = dict.TryGetAsync(key)
    let valuesAsync (dict: ChannelDictionary<_,_>) = dict.ValuesAsync()
    let keysAsync (dict: ChannelDictionary<_,_>) = dict.KeysAsync()

    let ofPair key value =
      let chDict = new ChannelDictionary<_, _>()
      chDict.AddOrUpdate(key, value)
      chDict

    let ofList (keyValuePairList: list<'key * 'value>) =
      let chDict = new ChannelDictionary<_, _>()
      for (key, value) in keyValuePairList do
        chDict.AddOrUpdate(key, value)
      chDict

    let toListAsync (dict: ChannelDictionary<_,_>) =
      dict.ToListAsync()

  type ChannelList<'Value>() = class end

  [<RequireQualifiedAccess>]
  module ChannelList =

    let a = ()

type ExtBool =
  | True
  | False
  | Partial

type Either<'LeftValue, 'RightValue> =
  | Left of 'LeftValue
  | Right of 'RightValue

[<RequireQualifiedAccess>]
module Either =

  let ifLeft either =
    match either with
    | Left _ -> true
    | Right _ -> false

  let ifRight either =
    match either with
    | Left _ -> false
    | Right _ -> true

[<RequireQualifiedAccess>]
module Option =

  let inline string optionValue =
    match optionValue with
    | Some v -> v.ToString()
    | None -> "None"

  let inline ofValueOption (vopt: 'value voption) : 'value option =
    match vopt with
    | ValueSome v -> Some v
    | ValueNone -> None

  let inline toValueOption (opt: 'value option) : 'value voption =
    match opt with
    | Some v -> ValueSome v
    | None -> ValueNone

  let inline traverseResult
    ([<InlineIfLambda>] binder: 'input -> Result<'okOutput, 'error>)
    (input: option<'input>)
    : Result<'okOutput option, 'error> =
    match input with
    | None -> Ok None
    | Some v -> binder v |> Result.map Some

  let inline sequenceResult (opt: Result<'ok, 'error> option) : Result<'ok option, 'error> =
    traverseResult id opt

#if !FABLE_COMPILER
  let inline tryParse< ^T when ^T: (static member TryParse: string * byref< ^T > -> bool) and ^T: (new:
    unit -> ^T)>
    (valueToParse: string)
    : ^T option =
    let mutable output = new ^T()

    let parsed =
      (^T: (static member TryParse: string * byref< ^T > -> bool) (valueToParse, &output))

    match parsed with
    | true -> Some output
    | _ -> None

  let inline tryGetValue (key: string) (dictionary: ^Dictionary) : ^value option =
    let mutable output = Unchecked.defaultof< ^value>

    let parsed =
      (^Dictionary: (member TryGetValue: string * byref< ^value > -> bool) (dictionary, key, &output))

    match parsed with
    | true -> Some output
    | false -> None
#endif

  /// <summary>
  /// Takes two options and returns a tuple of the pair or none if either are none
  /// </summary>
  /// <param name="option1">The input option</param>
  /// <param name="option2">The input option</param>
  /// <returns></returns>
  let inline zip (left: 'left option) (right: 'right option) : ('left * 'right) option =
    match left, right with
    | Some v1, Some v2 -> Some(v1, v2)
    | _ -> None


  let inline ofResult (r: Result<'ok, 'error>) : 'ok option =
    match r with
    | Ok v -> Some v
    | Error _ -> None

  /// <summary>
  /// Convert a potentially null value to an option.
  ///
  /// self is different from <see cref="FSharp.Core.Option.ofObj">Option.ofObj</see> where it doesn't require the value to be constrained to null.
  /// self is beneficial where third party APIs may generate a record type using reflection and it can be null.
  /// See <a href="https://latkin.org/blog/2015/05/18/null-checking-considerations-in-f-its-harder-than-you-think/">Null-checking considerations in F#</a> for more details.
  /// </summary>
  /// <param name="value">The potentially null value</param>
  /// <returns>An option</returns>
  /// <seealso cref="FSharp.Core.Option.ofObj"/>
  let inline ofNull (value: 'nullableValue) : 'nullableValue option =
    if System.Object.ReferenceEquals(value, null) then
      None
    else
      Some value


  /// <summary>
  ///
  /// <c>bindNull binder option</c> evaluates to <c>match option with None -> None | Some x -> binder x |> Option.ofNull</c>
  ///
  /// Automatically onverts the result of binder that is pontentially null into an option.
  /// </summary>
  /// <param name="binder">A function that takes the value of type 'value from an option and transforms it into
  /// a value of type 'nullableValue.</param>
  /// <param name="option">The input option</param>
  /// <typeparam name="'value"></typeparam>
  /// <typeparam name="'nullableValue"></typeparam>
  /// <returns>An option of the output type of the binder.</returns>
  /// <seealso cref="ofNull"/>
  let inline bindNull
    ([<InlineIfLambda>] binder: 'value -> 'nullableValue)
    (option: Option<'value>)
    : 'nullableValue option =
    match option with
    | Some x -> binder x |> ofNull
    | None -> None

[<AutoOpen>]
module OptionCE =

  type OptionBuilder() =
    member inline _.Return(x: 'value) : 'value option = Some x

    member inline _.ReturnFrom(m: 'value option) : 'value option = m

    member inline _.Bind
      (
        input: 'input option,
        [<InlineIfLambda>] binder: 'input -> 'output option
      ) : 'output option =
      Option.bind binder input

    // Could not get it to work solely with Source. In loop cases it would potentially match the #seq overload and ask for type annotation
    member inline self.Bind
      (
        m: 'input when 'input: null,
        [<InlineIfLambda>] binder: 'input -> 'output option
      ) : 'output option =
      self.Bind(Option.ofObj m, binder)

    member inline self.Zero() : unit option = self.Return()

    member inline _.Combine
      (
        m: 'input option,
        [<InlineIfLambda>] binder: 'input -> 'output option
      ) : 'output option =
      Option.bind binder m

    member inline self.Combine(m1: unit option, m2: 'output option) : 'output option =
      self.Bind(m1, (fun () -> m2))

    member inline _.Delay
      ([<InlineIfLambda>] delayer: unit -> 'value option)
      : (unit -> 'value option) =
      delayer

    member inline _.Run([<InlineIfLambda>] delayed) = delayed ()

    member inline self.TryWith([<InlineIfLambda>] computation, handler) : 'value =
      try
        self.Run computation
      with
      | e -> handler e

    member inline self.TryFinally([<InlineIfLambda>] computation, compensation) =
      try
        self.Run computation
      finally
        compensation ()

    member inline self.Using
      (
        resource: 'disposable :> IDisposable,
        [<InlineIfLambda>] binder: 'disposable -> 'value option
      ) : 'value option =
      self.TryFinally(
        (fun () -> binder resource),
        (fun () ->
          if not <| obj.ReferenceEquals(resource, null) then
            resource.Dispose())
      )

    member inline self.While
      (
        [<InlineIfLambda>] guard: unit -> bool,
        [<InlineIfLambda>] computation: unit -> unit option
      ) : unit option =
      if guard () then
        let mutable whileBuilder = Unchecked.defaultof<_>

        whileBuilder <-
          fun () ->
            self.Bind(
              self.Run computation,
              (fun () ->
                if guard () then
                  self.Run whileBuilder
                else
                  self.Zero())
            )

        self.Run whileBuilder
      else
        self.Zero()

    member inline self.For
      (
        sequence: #seq<'value>,
        [<InlineIfLambda>] binder: 'value -> unit option
      ) : unit option =
      self.Using(
        sequence.GetEnumerator(),
        fun enum -> self.While(enum.MoveNext, self.Delay(fun () -> binder enum.Current))
      )

    member inline _.BindReturn
      (
        input: 'input option,
        [<InlineIfLambda>] mapper: 'input -> 'output
      ) : 'output option =
      Option.map mapper input

    member inline _.BindReturn
      (
        x: 'input,
        [<InlineIfLambda>] f: 'input -> 'output
      ) : 'output option =
      Option.map f (Option.ofObj x)

    member inline _.MergeSources
      (
        option1: 'left option,
        option2: 'right option
      ) : ('left * 'right) option =
      Option.zip option1 option2

    /// <summary>
    /// Method lets us transform data types into our internal representation.  self is the identity method to recognize the self type.
    ///
    /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
    /// </summary>
    member inline _.Source(result: 'value option) : 'value option = result


    // /// <summary>
    // /// Method lets us transform data types into our internal representation.
    // /// </summary>
    member inline _.Source(vopt: 'value voption) : 'value option = Option.ofValueOption vopt

  let option = OptionBuilder()

[<AutoOpen>]
module OptionExtensionsLower =
  type OptionBuilder with

    member inline _.Source(nullableObj: 'value when 'value: null) : 'value option =
      Option.ofObj nullableObj

    member inline _.Source(m: string) : string option = Option.ofObj m

    member inline _.MergeSources
      (
        nullableObj1: 'left,
        option2: 'right option
      ) : ('left * 'right) option =
      Option.zip (Option.ofObj nullableObj1) option2

    member inline _.MergeSources
      (
        option1: 'left option,
        nullableObj2: 'right
      ) : ('left * 'right) option =
      Option.zip (option1) (Option.ofObj nullableObj2)

    member inline _.MergeSources
      (
        nullableObj1: 'left,
        nullableObj2: 'right
      ) : ('left * 'right) option =
      Option.zip (Option.ofObj nullableObj1) (Option.ofObj nullableObj2)

[<AutoOpen>]
module OptionExtensions =

  type OptionBuilder with

    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline _.Source(s: #seq<'value>) : #seq<'value> = s

    // /// <summary>
    // /// Method lets us transform data types into our internal representation.
    // /// </summary>
    member inline _.Source(nullable: Nullable<'value>) : 'value option = Option.ofNullable nullable

[<RequireQualifiedAccess>]
module Result =

  let inline map
    ([<InlineIfLambda>] mapper: 'okInput -> 'okOutput)
    (input: Result<'okInput, 'error>)
    : Result<'okOutput, 'error> =
    match input with
    | Ok x -> Ok(mapper x)
    | Error e -> Error e

  let inline mapError
    ([<InlineIfLambda>] errorMapper: 'errorInput -> 'errorOutput)
    (input: Result<'ok, 'errorInput>)
    : Result<'ok, 'errorOutput> =
    match input with
    | Ok x -> Ok x
    | Error e -> Error(errorMapper e)

  let inline bind
    ([<InlineIfLambda>] binder: 'okInput -> Result<'okOutput, 'error>)
    (input: Result<'okInput, 'error>)
    : Result<'okOutput, 'error> =
    match input with
    | Ok x -> binder x
    | Error e -> Error e

  let inline isOk (value: Result<'ok, 'error>) : bool =
    match value with
    | Ok _ -> true
    | Error _ -> false

  let inline isError (value: Result<'ok, 'error>) : bool =
    match value with
    | Ok _ -> false
    | Error _ -> true

  let inline either
    ([<InlineIfLambda>] onOk: 'okInput -> 'output)
    ([<InlineIfLambda>] onError: 'errorInput -> 'output)
    (input: Result<'okInput, 'errorInput>)
    : 'output =
    match input with
    | Ok x -> onOk x
    | Error err -> onError err

  let inline eitherMap
    ([<InlineIfLambda>] onOk: 'okInput -> 'okOutput)
    ([<InlineIfLambda>] onError: 'errorInput -> 'errorOutput)
    (input: Result<'okInput, 'errorInput>)
    : Result<'okOutput, 'errorOutput> =
    match input with
    | Ok x -> Ok(onOk x)
    | Error err -> Error(onError err)

  let inline apply
    (applier: Result<'okInput -> 'okOutput, 'error>)
    (input: Result<'okInput, 'error>)
    : Result<'okOutput, 'error> =
    match (applier, input) with
    | Ok f, Ok x -> Ok(f x)
    | Error e, _
    | _, Error e -> Error e

  let inline map2
    ([<InlineIfLambda>] mapper: 'okInput1 -> 'okInput2 -> 'okOutput)
    (input1: Result<'okInput1, 'error>)
    (input2: Result<'okInput2, 'error>)
    : Result<'okOutput, 'error> =
    match (input1, input2) with
    | Ok x, Ok y -> Ok(mapper x y)
    | Error e, _
    | _, Error e -> Error e


  let inline map3
    ([<InlineIfLambda>] mapper: 'okInput1 -> 'okInput2 -> 'okInput3 -> 'okOutput)
    (input1: Result<'okInput1, 'error>)
    (input2: Result<'okInput2, 'error>)
    (input3: Result<'okInput3, 'error>)
    : Result<'okOutput, 'error> =
    match (input1, input2, input3) with
    | Ok x, Ok y, Ok z -> Ok(mapper x y z)
    | Error e, _, _
    | _, Error e, _
    | _, _, Error e -> Error e

  let inline fold
    ([<InlineIfLambda>] onOk: 'okInput -> 'output)
    ([<InlineIfLambda>] onError: 'errorInput -> 'output)
    (input: Result<'okInput, 'errorInput>)
    : 'output =
    match input with
    | Ok x -> onOk x
    | Error err -> onError err

  let inline ofChoice (input: Choice<'ok, 'error>) : Result<'ok, 'error> =
    match input with
    | Choice1Of2 x -> Ok x
    | Choice2Of2 e -> Error e

  let inline tryCreate (fieldName: string) (x: 'a) : Result< ^b, (string * 'c) > =
    let tryCreate' x =
      (^b: (static member TryCreate: 'a -> Result< ^b, 'c >) x)

    tryCreate' x |> mapError (fun z -> (fieldName, z))


  /// <summary>
  /// Returns <paramref name="result"/> if it is <c>Ok</c>, otherwise returns <paramref name="ifError"/>
  /// </summary>
  /// <param name="ifError">The value to use if <paramref name="result"/> is <c>Error</c></param>
  /// <param name="result">The input result.</param>
  /// <remarks>
  /// </remarks>
  /// <example>
  /// <code>
  ///     Error ("First") |> Result.orElse (Error ("Second")) // evaluates to Error ("Second")
  ///     Error ("First") |> Result.orElseWith (Ok ("Second")) // evaluates to Ok ("Second")
  ///     Ok ("First") |> Result.orElseWith (Error ("Second")) // evaluates to Ok ("First")
  ///     Ok ("First") |> Result.orElseWith (Ok ("Second")) // evaluates to Ok ("First")
  /// </code>
  /// </example>
  /// <returns>
  /// The result if the result is Ok, else returns <paramref name="ifError"/>.
  /// </returns>
  let inline orElse
    (ifError: Result<'ok, 'errorOutput>)
    (result: Result<'ok, 'error>)
    : Result<'ok, 'errorOutput> =
    match result with
    | Ok x -> Ok x
    | Error e -> ifError


  /// <summary>
  /// Returns <paramref name="result"/> if it is <c>Ok</c>, otherwise executes <paramref name="ifErrorFunc"/> and returns the result.
  /// </summary>
  /// <param name="ifErrorFunc">A function that provides an alternate result when evaluated.</param>
  /// <param name="result">The input result.</param>
  /// <remarks>
  /// <paramref name="ifErrorFunc"/>  is not executed unless <paramref name="result"/> is an <c>Error</c>.
  /// </remarks>
  /// <example>
  /// <code>
  ///     Error ("First") |> Result.orElseWith (fun _ -> Error ("Second")) // evaluates to Error ("Second")
  ///     Error ("First") |> Result.orElseWith (fun _ -> Ok ("Second")) // evaluates to Ok ("Second")
  ///     Ok ("First") |> Result.orElseWith (fun _ -> Error ("Second")) // evaluates to Ok ("First")
  ///     Ok ("First") |> Result.orElseWith (fun _ -> Ok ("Second")) // evaluates to Ok ("First")
  /// </code>
  /// </example>
  /// <returns>
  /// The result if the result is Ok, else the result of executing <paramref name="ifErrorFunc"/>.
  /// </returns>
  let inline orElseWith
    ([<InlineIfLambda>] ifErrorFunc: 'error -> Result<'ok, 'errorOutput>)
    (result: Result<'ok, 'error>)
    : Result<'ok, 'errorOutput> =
    match result with
    | Ok x -> Ok x
    | Error e -> ifErrorFunc e

  /// Replaces the wrapped value with unit
  let inline ignore<'ok, 'error> (result: Result<'ok, 'error>) : Result<unit, 'error> =
    match result with
    | Ok _ -> Ok()
    | Error e -> Error e

  /// Returns the specified error if the value is false.
  let inline requireTrue (error: 'error) (value: bool) : Result<unit, 'error> =
    if value then Ok() else Error error

  /// Returns the specified error if the value is true.
  let inline requireFalse (error: 'error) (value: bool) : Result<unit, 'error> =
    if not value then Ok() else Error error

  /// Converts an Option to a Result, using the given error if None.
  let inline requireSome (error: 'error) (option: 'ok option) : Result<'ok, 'error> =
    match option with
    | Some x -> Ok x
    | None -> Error error

  /// Converts an Option to a Result, using the given error if Some.
  let inline requireNone (error: 'error) (option: 'value option) : Result<unit, 'error> =
    match option with
    | Some _ -> Error error
    | None -> Ok()

  /// Converts a nullable value into a Result, using the given error if null
  let inline requireNotNull (error: 'error) (value: 'ok) : Result<'ok, 'error> =
    match value with
    | null -> Error error
    | nonnull -> Ok nonnull

  /// Returns Ok if the two values are equal, or the specified error if not.
  /// Same as requireEqual, but with a signature that fits piping better than
  /// normal function application.
  let inline requireEqualTo (other: 'value) (error: 'error) (self: 'value) : Result<unit, 'error> =
    if self = other then
      Ok()
    else
      Error error

  /// Returns Ok if the two values are equal, or the specified error if not.
  /// Same as requireEqualTo, but with a signature that fits normal function
  /// application better than piping.
  let inline requireEqual (x1: 'value) (x2: 'value) (error: 'error) : Result<unit, 'error> =
    if x1 = x2 then Ok() else Error error

  /// Returns Ok if the sequence is empty, or the specified error if not.
  let inline requireEmpty (error: 'error) (xs: #seq<'value>) : Result<unit, 'error> =
    if Seq.isEmpty xs then
      Ok()
    else
      Error error

  /// Returns the specified error if the sequence is empty, or Ok if not.
  let inline requireNotEmpty (error: 'error) (xs: #seq<'value>) : Result<unit, 'error> =
    if Seq.isEmpty xs then
      Error error
    else
      Ok()

  /// Returns the first item of the sequence if it exists, or the specified
  /// error if the sequence is empty
  let inline requireHead (error: 'error) (xs: #seq<'ok>) : Result<'ok, 'error> =
    match Seq.tryHead xs with
    | Some x -> Ok x
    | None -> Error error

  /// Replaces an error value with a custom error value.
  let inline setError (error: 'error) (result: Result<'ok, 'errorIgnored>) : Result<'ok, 'error> =
    result |> mapError (fun _ -> error)

  /// Replaces a unit error value with a custom error value. Safer than setError
  /// since you're not losing any information.
  let inline withError (error: 'error) (result: Result<'ok, unit>) : Result<'ok, 'error> =
    result |> mapError (fun () -> error)

  /// Returns the contained value if Ok, otherwise returns ifError.
  let inline defaultValue (ifError: 'ok) (result: Result<'ok, 'error>) : 'ok =
    match result with
    | Ok x -> x
    | Error _ -> ifError

  // Returns the contained value if Error, otherwise returns ifOk.
  let inline defaultError (ifOk: 'error) (result: Result<'ok, 'error>) : 'error =
    match result with
    | Error error -> error
    | Ok _ -> ifOk

  /// Returns the contained value if Ok, otherwise evaluates ifErrorThunk and
  /// returns the result.
  let inline defaultWith
    ([<InlineIfLambda>] ifErrorThunk: unit -> 'ok)
    (result: Result<'ok, 'error>)
    : 'ok =
    match result with
    | Ok x -> x
    | Error _ -> ifErrorThunk ()

  /// Same as defaultValue for a result where the Ok value is unit. The name
  /// describes better what is actually happening in self case.
  let inline ignoreError<'error> (result: Result<unit, 'error>) : unit = defaultValue () result

  /// If the result is Ok and the predicate returns true, executes the function
  /// on the Ok value. Passes through the input value.
  let inline teeIf
    ([<InlineIfLambda>] predicate: 'ok -> bool)
    ([<InlineIfLambda>] inspector: 'ok -> unit)
    (result: Result<'ok, 'error>)
    : Result<'ok, 'error> =
    match result with
    | Ok x -> if predicate x then inspector x
    | Error _ -> ()

    result

  /// If the result is Error and the predicate returns true, executes the
  /// function on the Error value. Passes through the input value.
  let inline teeErrorIf
    ([<InlineIfLambda>] predicate: 'error -> bool)
    ([<InlineIfLambda>] inspector: 'error -> unit)
    (result: Result<'ok, 'error>)
    : Result<'ok, 'error> =
    match result with
    | Ok _ -> ()
    | Error x -> if predicate x then inspector x

    result

  /// If the result is Ok, executes the function on the Ok value. Passes through
  /// the input value.
  let inline tee
    ([<InlineIfLambda>] inspector: 'ok -> unit)
    (result: Result<'ok, 'error>)
    : Result<'ok, 'error> =
    teeIf (fun _ -> true) inspector result

  /// If the result is Error, executes the function on the Error value. Passes
  /// through the input value.
  let inline teeError
    ([<InlineIfLambda>] inspector: 'error -> unit)
    (result: Result<'ok, 'error>)
    : Result<'ok, 'error> =
    teeErrorIf (fun _ -> true) inspector result

  /// Converts a Result<Async<_>,_> to an Async<Result<_,_>>
  let inline sequenceAsync (resAsync: Result<Async<'ok>, 'error>) : Async<Result<'ok, 'error>> =
    async {
      match resAsync with
      | Ok asnc ->
        let! x = asnc
        return Ok x
      | Error err -> return Error err
    }

  ///
  let inline traverseAsync
    ([<InlineIfLambda>] f: 'okInput -> Async<'okOutput>)
    (res: Result<'okInput, 'error>)
    : Async<Result<'okOutput, 'error>> =
    sequenceAsync ((map f) res)


  /// Returns the Ok value or runs the specified function over the error value.
  let inline valueOr ([<InlineIfLambda>] f: 'error -> 'ok) (res: Result<'ok, 'error>) : 'ok =
    match res with
    | Ok x -> x
    | Error x -> f x

  /// Takes two results and returns a tuple of the pair
  let zip
    (left: Result<'leftOk, 'error>)
    (right: Result<'rightOk, 'error>)
    : Result<'leftOk * 'rightOk, 'error> =
    match left, right with
    | Ok x1res, Ok x2res -> Ok(x1res, x2res)
    | Error e, _ -> Error e
    | _, Error e -> Error e

  /// Takes two results and returns a tuple of the error pair
  let zipError
    (left: Result<'ok, 'leftError>)
    (right: Result<'ok, 'rightError>)
    : Result<'ok, 'leftError * 'rightError> =
    match left, right with
    | Error x1res, Error x2res -> Error(x1res, x2res)
    | Ok e, _ -> Ok e
    | _, Ok e -> Ok e


[<AutoOpen>]
module ResultCE =

  type ResultBuilder() =
    member inline _.Return(value: 'ok) : Result<'ok, 'error> = Ok value

    member inline _.ReturnFrom(result: Result<'ok, 'error>) : Result<'ok, 'error> = result

    member self.Zero() : Result<unit, 'error> = self.Return()

    member inline _.Bind
      (
        input: Result<'okInput, 'error>,
        [<InlineIfLambda>] binder: 'okInput -> Result<'okOutput, 'error>
      ) : Result<'okOutput, 'error> =
      Result.bind binder input

    member inline _.Delay
      ([<InlineIfLambda>] generator: unit -> Result<'ok, 'error>)
      : unit -> Result<'ok, 'error> =
      generator

    member inline _.Run
      ([<InlineIfLambda>] generator: unit -> Result<'ok, 'error>)
      : Result<'ok, 'error> =
      generator ()

    member inline self.Combine
      (
        result: Result<unit, 'error>,
        [<InlineIfLambda>] binder: unit -> Result<'ok, 'error>
      ) : Result<'ok, 'error> =
      self.Bind(result, binder)

    member inline self.TryWith
      (
        [<InlineIfLambda>] generator: unit -> Result<'T, 'TError>,
        [<InlineIfLambda>] handler: exn -> Result<'T, 'TError>
      ) : Result<'T, 'TError> =
      try
        self.Run generator
      with
      | e -> handler e

    member inline self.TryFinally
      (
        [<InlineIfLambda>] generator: unit -> Result<'ok, 'error>,
        [<InlineIfLambda>] compensation: unit -> unit
      ) : Result<'ok, 'error> =
      try
        self.Run generator
      finally
        compensation ()

    member inline self.Using
      (
        resource: 'disposable :> IDisposable,
        binder: 'disposable -> Result<'ok, 'error>
      ) : Result<'ok, 'error> =
      self.TryFinally(
        (fun () -> binder resource),
        (fun () ->
          if not <| obj.ReferenceEquals(resource, null) then
            resource.Dispose())
      )

    member inline self.While
      (
        [<InlineIfLambda>] guard: unit -> bool,
        [<InlineIfLambda>] generator: unit -> Result<unit, 'error>
      ) : Result<unit, 'error> =
      if guard () then
        let mutable whileBuilder = Unchecked.defaultof<_>

        whileBuilder <-
          fun () ->
            self.Bind(
              self.Run generator,
              (fun () ->
                if guard () then
                  self.Run whileBuilder
                else
                  self.Zero())
            )

        self.Run whileBuilder
      else
        self.Zero()

    member inline self.For
      (
        sequence: #seq<'T>,
        [<InlineIfLambda>] binder: 'T -> Result<unit, 'TError>
      ) : Result<unit, 'TError> =
      self.Using(
        sequence.GetEnumerator(),
        fun enum -> self.While(enum.MoveNext, self.Delay(fun () -> binder enum.Current))
      )

    member inline _.BindReturn
      (
        x: Result<'okInput, 'error>,
        [<InlineIfLambda>] f: 'okInput -> 'okOutput
      ) : Result<'okOutput, 'error> =
      Result.map f x

    member inline _.MergeSources
      (
        left: Result<'left, 'error>,
        right: Result<'right, 'error>
      ) : Result<'left * 'right, 'error> =
      Result.zip left right

    /// <summary>
    /// Method lets us transform data types into our internal representation.  self is the identity method to recognize the self type.
    ///
    /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    member inline _.Source(result: Result<'ok, 'error>) : Result<'ok, 'error> = result

  let result = ResultBuilder()

[<AutoOpen>]
module ResultCEExtensions =

  type ResultBuilder with

    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline _.Source(s: #seq<_>) : #seq<_> = s


// Having Choice<_> members as extensions gives them lower priority in
// overload resolution and allows skipping more type annotations.
[<AutoOpen>]
module ResultCEChoiceExtensions =
  type ResultBuilder with

    /// <summary>
    /// Method lets us transform data types into our internal representation.
    /// </summary>
    /// <returns></returns>
    member inline _.Source(choice: Choice<'ok, 'error>) : Result<'ok, 'error> =
      Result.ofChoice choice

//==============================================
// The `Validation` type is the same as the `Result` type but with a *list* for failures
// rather than a single value. self allows `Validation` types to be combined
// by combining their errors ("applicative-style")
//==============================================

/// Validation<'a, 'err> is defined as Result<'a, 'err list> meaning you can use many of the functions found in the Result module.
type Validation<'Ok, 'Error> = Result<'Ok, 'Error list>

[<RequireQualifiedAccess>]
module Validation =

  let inline ok (value: 'ok) : Validation<'ok, 'error> = Ok value
  let inline error (error: 'error) : Validation<'ok, 'error> = Error [ error ]

  let inline ofResult (result: Result<'ok, 'error>) : Validation<'ok, 'error> =
    Result.mapError List.singleton result

  let inline ofChoice (choice: Choice<'ok, 'error>) : Validation<'ok, 'error> =
    match choice with
    | Choice1Of2 x -> ok x
    | Choice2Of2 e -> error e

  let inline apply
    (applier: Validation<'okInput -> 'okOutput, 'error>)
    (input: Validation<'okInput, 'error>)
    : Validation<'okOutput, 'error> =
    match applier, input with
    | Ok f, Ok x -> Ok(f x)
    | Error errs, Ok _
    | Ok _, Error errs -> Error errs
    | Error errs1, Error errs2 -> Error(errs1 @ errs2)

  let inline retn (value: 'ok) : Validation<'ok, 'error> = ok value

  let inline returnError (error: 'error) : Validation<'ok, 'error> = Error [ error ]


  /// <summary>
  /// Returns <paramref name="result"/> if it is <c>Ok</c>, otherwise returns <paramref name="ifError"/>
  /// </summary>
  /// <param name="ifError">The value to use if <paramref name="result"/> is <c>Error</c></param>
  /// <param name="result">The input result.</param>
  /// <remarks>
  /// </remarks>
  /// <example>
  /// <code>
  ///     Error (["First"]) |> Validation.orElse (Error (["Second"])) // evaluates to Error (["Second"])
  ///     Error (["First"]) |> Validation.orElseWith (Ok ("Second")) // evaluates to Ok ("Second")
  ///     Ok ("First") |> Validation.orElseWith (Error (["Second"])) // evaluates to Ok ("First")
  ///     Ok ("First") |> Validation.orElseWith (Ok ("Second")) // evaluates to Ok ("First")
  /// </code>
  /// </example>
  /// <returns>
  /// The result if the result is Ok, else returns <paramref name="ifError"/>.
  /// </returns>
  let inline orElse
    (ifError: Validation<'ok, 'errorOutput>)
    (result: Validation<'ok, 'errorInput>)
    : Validation<'ok, 'errorOutput> =
    result |> Result.either ok (fun _ -> ifError)



  /// <summary>
  /// Returns <paramref name="result"/> if it is <c>Ok</c>, otherwise executes <paramref name="ifErrorFunc"/> and returns the result.
  /// </summary>
  /// <param name="ifErrorFunc">A function that provides an alternate result when evaluated.</param>
  /// <param name="result">The input result.</param>
  /// <remarks>
  /// <paramref name="ifErrorFunc"/>  is not executed unless <paramref name="result"/> is an <c>Error</c>.
  /// </remarks>
  /// <example>
  /// <code>
  ///     Error (["First"]) |> Validation.orElseWith (fun _ -> Error (["Second"])) // evaluates to Error (["Second"])
  ///     Error (["First"]) |> Validation.orElseWith (fun _ -> Ok ("Second")) // evaluates to Ok ("Second")
  ///     Ok ("First") |> Validation.orElseWith (fun _ -> Error (["Second"])) // evaluates to Ok ("First")
  ///     Ok ("First") |> Validation.orElseWith (fun _ -> Ok ("Second")) // evaluates to Ok ("First")
  /// </code>
  /// </example>
  /// <returns>
  /// The result if the result is Ok, else the result of executing <paramref name="ifErrorFunc"/>.
  /// </returns>
  let inline orElseWith
    ([<InlineIfLambda>] ifErrorFunc: 'errorInput list -> Validation<'ok, 'errorOutput>)
    (result: Validation<'ok, 'errorInput>)
    : Validation<'ok, 'errorOutput> =
    result |> Result.either ok ifErrorFunc


  let inline map
    ([<InlineIfLambda>] mapper: 'okInput -> 'okOutput)
    (input: Validation<'okInput, 'error>)
    : Validation<'okOutput, 'error> =
    Result.map mapper input

  let inline map2
    ([<InlineIfLambda>] mapper: 'okInput1 -> 'okInput2 -> 'okOutput)
    (input1: Validation<'okInput1, 'error>)
    (input2: Validation<'okInput2, 'error>)
    : Validation<'okOutput, 'error> =
    match input1, input2 with
    | Ok x, Ok y -> Ok(mapper x y)
    | Ok _, Error errs -> Error errs
    | Error errs, Ok _ -> Error errs
    | Error errs1, Error errs2 -> Error(errs1 @ errs2)

  let inline map3
    ([<InlineIfLambda>] mapper: 'okInput1 -> 'okInput2 -> 'okInput3 -> 'okOutput)
    (input1: Validation<'okInput1, 'error>)
    (input2: Validation<'okInput2, 'error>)
    (input3: Validation<'okInput3, 'error>)
    : Validation<'okOutput, 'error> =
    match input1, input2, input3 with
    | Ok x, Ok y, Ok z -> Ok(mapper x y z)
    | Error errs, Ok _, Ok _ -> Error errs
    | Ok _, Error errs, Ok _ -> Error errs
    | Ok _, Ok _, Error errs -> Error errs
    | Error errs1, Error errs2, Ok _ -> Error(errs1 @ errs2)
    | Ok _, Error errs1, Error errs2 -> Error(errs1 @ errs2)
    | Error errs1, Ok _, Error errs2 -> Error(errs1 @ errs2)
    | Error errs1, Error errs2, Error errs3 -> Error(errs1 @ errs2 @ errs3)

  let inline mapError
    ([<InlineIfLambda>] errorMapper: 'errorInput -> 'errorOutput)
    (input: Validation<'ok, 'errorInput>)
    : Validation<'ok, 'errorOutput> =
    Result.mapError (List.map errorMapper) input

  let inline mapErrors
    ([<InlineIfLambda>] errorMapper: 'errorInput list -> 'errorOutput list)
    (input: Validation<'ok, 'errorInput>)
    : Validation<'ok, 'errorOutput> =
    Result.mapError (errorMapper) input

  let inline bind
    ([<InlineIfLambda>] binder: 'okInput -> Validation<'okOutput, 'error>)
    (input: Validation<'okInput, 'error>)
    : Validation<'okOutput, 'error> =
    Result.bind binder input

  let inline zip
    (left: Validation<'left, 'error>)
    (right: Validation<'right, 'error>)
    : Validation<'left * 'right, 'error> =
    match left, right with
    | Ok x1res, Ok x2res -> Ok(x1res, x2res)
    | Error e, Ok _ -> Error e
    | Ok _, Error e -> Error e
    | Error e1, Error e2 -> Error(e1 @ e2)


[<AutoOpen>]
module ValidationCE =
  type ValidationBuilder() =
    member inline _.Return(value: 'ok) : Validation<'ok, 'error> = Validation.ok value

    member inline _.ReturnFrom(result: Validation<'ok, 'error>) : Validation<'ok, 'error> = result

    member inline _.Bind
      (
        result: Validation<'okInput, 'error>,
        [<InlineIfLambda>] binder: 'okInput -> Validation<'okOutput, 'error>
      ) : Validation<'okOutput, 'error> =
      Validation.bind binder result

    member inline self.Zero() : Validation<unit, 'error> = self.Return()

    member inline _.Delay
      ([<InlineIfLambda>] generator: unit -> Validation<'ok, 'error>)
      : unit -> Validation<'ok, 'error> =
      generator

    member inline _.Run
      ([<InlineIfLambda>] generator: unit -> Validation<'ok, 'error>)
      : Validation<'ok, 'error> =
      generator ()

    member inline self.Combine
      (
        result: Validation<unit, 'error>,
        [<InlineIfLambda>] binder: unit -> Validation<'ok, 'error>
      ) : Validation<'ok, 'error> =
      self.Bind(result, binder)

    member inline self.TryWith
      (
        [<InlineIfLambda>] generator: unit -> Validation<'ok, 'error>,
        [<InlineIfLambda>] handler: exn -> Validation<'ok, 'error>
      ) : Validation<'ok, 'error> =
      try
        self.Run generator
      with
      | e -> handler e

    member inline self.TryFinally
      (
        [<InlineIfLambda>] generator: unit -> Validation<'ok, 'error>,
        [<InlineIfLambda>] compensation: unit -> unit
      ) : Validation<'ok, 'error> =
      try
        self.Run generator
      finally
        compensation ()

    member inline self.Using
      (
        resource: 'disposable :> IDisposable,
        [<InlineIfLambda>] binder: 'disposable -> Validation<'okOutput, 'error>
      ) : Validation<'okOutput, 'error> =
      self.TryFinally(
        (fun () -> binder resource),
        (fun () ->
          if not <| obj.ReferenceEquals(resource, null) then
            resource.Dispose())
      )

    member inline self.While
      (
        [<InlineIfLambda>] guard: unit -> bool,
        [<InlineIfLambda>] generator: unit -> Validation<unit, 'error>
      ) : Validation<unit, 'error> =
      if guard () then
        let mutable whileBuilder = Unchecked.defaultof<_>

        whileBuilder <-
          fun () ->
            self.Bind(
              self.Run generator,
              (fun () ->
                if guard () then
                  self.Run whileBuilder
                else
                  self.Zero())
            )

        self.Run whileBuilder
      else
        self.Zero()

    member inline self.For
      (
        sequence: #seq<'ok>,
        [<InlineIfLambda>] binder: 'ok -> Validation<unit, 'error>
      ) : Validation<unit, 'error> =
      self.Using(
        sequence.GetEnumerator(),
        fun enum -> self.While(enum.MoveNext, self.Delay(fun () -> binder enum.Current))
      )

    member inline _.BindReturn
      (
        input: Validation<'okInput, 'error>,
        [<InlineIfLambda>] mapper: 'okInput -> 'okOutput
      ) : Validation<'okOutput, 'error> =
      Validation.map mapper input

    member inline _.MergeSources
      (
        left: Validation<'left, 'error>,
        right: Validation<'right, 'error>
      ) : Validation<'left * 'right, 'error> =
      Validation.zip left right

    /// <summary>
    /// Method lets us transform data types into our internal representation.  self is the identity method to recognize the self type.
    ///
    /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    member inline _.Source(result: Validation<'ok, 'error>) : Validation<'ok, 'error> = result

  let validation = ValidationBuilder()

[<AutoOpen>]
module ValidationCEExtensions =

  // Having members as extensions gives them lower priority in
  // overload resolution and allows skipping more type annotations.
  type ValidationBuilder with

    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline _.Source(s: #seq<_>) : #seq<_> = s

    /// <summary>
    /// Method lets us transform data types into our internal representation.
    /// </summary>
    member inline _.Source(s: Result<'ok, 'error>) : Validation<'ok, 'error> = Validation.ofResult s

    /// <summary>
    /// Method lets us transform data types into our internal representation.
    /// </summary>
    /// <returns></returns>
    member inline _.Source(choice: Choice<'ok, 'error>) : Validation<'ok, 'error> =
      Validation.ofChoice choice

#if !FABLE_COMPILER
[<RequireQualifiedAccess>]
module ValueOption =

  let inline ofOption (opt: 'value option) : 'value voption =
    match opt with
    | Some v -> ValueSome v
    | None -> ValueNone

  let inline toOption (vopt: 'value voption) : 'value option =
    match vopt with
    | ValueSome v -> Some v
    | ValueNone -> None

  let inline traverseResult
    ([<InlineIfLambda>] binder: 'okInput -> Result<'okOutput, 'error>)
    (input: 'okInput voption)
    : Result<'okOutput voption, 'error> =
    match input with
    | ValueNone -> Ok ValueNone
    | ValueSome v -> binder v |> Result.map ValueSome

  let inline sequenceResult
    (opt: Result<'okOutput, 'error> voption)
    : Result<'okOutput voption, 'error> =
    traverseResult id opt

  let inline tryParse< ^value when ^value: (static member TryParse: string * byref< ^value > -> bool) and ^value: (new:
    unit -> ^value)>
    (valueToParse: string)
    : ^value voption =
    let mutable output = new ^value ()

    let parsed =
      (^value: (static member TryParse: string * byref< ^value > -> bool) (valueToParse, &output))

    match parsed with
    | true -> ValueSome output
    | _ -> ValueNone

  let inline tryGetValue (key: string) (dictionary: ^Dictionary) : ^value voption =
    let mutable output = Unchecked.defaultof< ^value>

    let parsed =
      (^Dictionary: (member TryGetValue: string * byref< ^value > -> bool) (dictionary, key, &output))

    match parsed with
    | true -> ValueSome output
    | false -> ValueNone

  /// <summary>
  /// Takes two voptions and returns a tuple of the pair or none if either are none
  /// </summary>
  /// <param name="voption1">The input option</param>
  /// <param name="voption2">The input option</param>
  /// <returns></returns>
  let inline zip (left: 'left voption) (right: 'right voption) : ('left * 'right) voption =
    match left, right with
    | ValueSome v1, ValueSome v2 -> ValueSome(v1, v2)
    | _ -> ValueNone


  let inline ofResult (result: Result<'ok, 'error>) : 'ok voption =
    match result with
    | Ok v -> ValueSome v
    | Error _ -> ValueNone


  /// <summary>
  /// Convert a potentially null value to an ValueOption.
  ///
  /// self is different from <see cref="FSharp.Core.ValueOption.ofObj">ValueOption.ofObj</see> where it doesn't require the value to be constrained to null.
  /// self is beneficial where third party APIs may generate a record type using reflection and it can be null.
  /// See <a href="https://latkin.org/blog/2015/05/18/null-checking-considerations-in-f-its-harder-than-you-think/">Null-checking considerations in F#</a> for more details.
  /// </summary>
  /// <param name="value">The potentially null value</param>
  /// <returns>An ValueOption</returns>
  /// <seealso cref="FSharp.Core.ValueOption.ofObj"/>
  let inline ofNull (value: 'nullableValue) =
    if System.Object.ReferenceEquals(value, null) then
      ValueNone
    else
      ValueSome value


  /// <summary>
  ///
  /// <c>bindNull binder voption</c> evaluates to <c>match voption with ValueNone -> ValueNone | ValueSome x -> binder x |> ValueOption.ofNull</c>
  ///
  /// Automatically onverts the result of binder that is pontentially null into an Valueoption.
  /// </summary>
  /// <param name="binder">A function that takes the value of type 'value from an voption and transforms it into
  /// a value of type 'nullableValue.</param>
  /// <param name="voption">The input voption</param>
  /// <typeparam name="'value"></typeparam>
  /// <typeparam name="'nullableValue"></typeparam>
  /// <returns>A voption of the output type of the binder.</returns>
  /// <seealso cref="ofNull"/>
  let inline bindNull
    ([<InlineIfLambda>] binder: 'value -> 'nullableValue)
    (voption: ValueOption<'value>)
    : 'nullableValue voption =
    match voption with
    | ValueSome x -> binder x |> ofNull
    | ValueNone -> ValueNone

#endif

#if !FABLE_COMPILER
[<AutoOpen>]
module ValueOptionCE =
  open System

  type ValueOptionBuilder() =
    member inline _.Return(x: 'value) : 'value voption = ValueSome x

    member inline _.ReturnFrom(m: 'value voption) : 'value voption = m

    member inline _.Bind
      (
        input: 'input voption,
        [<InlineIfLambda>] binder: 'input -> 'output voption
      ) : 'output voption =
      ValueOption.bind binder input

    // Could not get it to work solely with Source. In loop cases it would potentially match the #seq overload and ask for type annotation
    member inline self.Bind
      (
        input: 'input when 'input: null,
        [<InlineIfLambda>] binder: 'input -> 'output voption
      ) : 'output voption =
      self.Bind(ValueOption.ofObj input, binder)

    member inline self.Zero() : unit voption = self.Return()

    member inline _.Combine
      (
        input: 'input voption,
        [<InlineIfLambda>] binder: 'input -> 'output voption
      ) : 'output voption =
      ValueOption.bind binder input

    member inline self.Combine(input: unit voption, output: 'output voption) : 'output voption =
      self.Bind(input, (fun () -> output))

    member inline _.Delay([<InlineIfLambda>] f: unit -> 'a) = f

    member inline _.Run([<InlineIfLambda>] f: unit -> 'v) = f ()

    member inline self.TryWith([<InlineIfLambda>] m, [<InlineIfLambda>] handler) =
      try
        self.Run m
      with
      | e -> handler e

    member inline self.TryFinally([<InlineIfLambda>] m, [<InlineIfLambda>] compensation) =
      try
        self.Run m
      finally
        compensation ()

    member inline self.Using(resource: 'T :> IDisposable, [<InlineIfLambda>] binder) : _ voption =
      self.TryFinally(
        (fun () -> binder resource),
        (fun () ->
          if not <| obj.ReferenceEquals(resource, null) then
            resource.Dispose())
      )

    member inline self.While
      (
        [<InlineIfLambda>] guard: unit -> bool,
        [<InlineIfLambda>] generator: unit -> _ voption
      ) : _ voption =
      if guard () then
        let mutable whileBuilder = Unchecked.defaultof<_>

        whileBuilder <-
          fun () ->
            self.Bind(
              self.Run generator,
              (fun () ->
                if guard () then
                  self.Run whileBuilder
                else
                  self.Zero())
            )

        self.Run whileBuilder
      else
        self.Zero()

    member inline self.For
      (
        sequence: #seq<'T>,
        [<InlineIfLambda>] binder: 'T -> _ voption
      ) : _ voption =
      self.Using(
        sequence.GetEnumerator(),
        fun enum -> self.While(enum.MoveNext, self.Delay(fun () -> binder enum.Current))
      )

    member inline _.BindReturn(x, [<InlineIfLambda>] f) = ValueOption.map f x

    member inline _.BindReturn(x, [<InlineIfLambda>] f) =
      x |> ValueOption.ofObj |> ValueOption.map f

    member inline _.MergeSources(option1, option2) = ValueOption.zip option1 option2

    /// <summary>
    /// Method lets us transform data types into our internal representation.  self is the identity method to recognize the self type.
    ///
    /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
    /// </summary>
    member inline _.Source(result: _ voption) : _ voption = result


    // /// <summary>
    // /// Method lets us transform data types into our internal representation.
    // /// </summary>
    member inline _.Source(vopt: _ option) : _ voption = vopt |> ValueOption.ofOption

  let voption = ValueOptionBuilder()

[<AutoOpen>]
module ValueOptionExtensionsLower =
  type ValueOptionBuilder with

    member inline _.Source(nullableObj: 'a when 'a: null) = nullableObj |> ValueOption.ofObj

    member inline _.Source(m: string) = m |> ValueOption.ofObj

    member inline _.MergeSources(nullableObj1, option2) =
      ValueOption.zip (ValueOption.ofObj nullableObj1) option2

    member inline _.MergeSources(option1, nullableObj2) =
      ValueOption.zip (option1) (ValueOption.ofObj nullableObj2)

    member inline _.MergeSources(nullableObj1, nullableObj2) =
      ValueOption.zip (ValueOption.ofObj nullableObj1) (ValueOption.ofObj nullableObj2)

[<AutoOpen>]
module ValueOptionExtensions =

  type ValueOptionBuilder with

    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline _.Source(s: #seq<_>) = s

    // /// <summary>
    // /// Method lets us transform data types into our internal representation.
    // /// </summary>
    member inline _.Source(nullable: Nullable<'a>) : 'a voption = nullable |> ValueOption.ofNullable
#endif

[<RequireQualifiedAccess>]
module Async =

  let inline singleton (value: 'value) : Async<'value> = value |> async.Return
  let inline retn (value: 'value) : Async<'value> = value |> async.Return

  let inline bind
    ([<InlineIfLambda>] binder: 'input -> Async<'output>)
    (input: Async<'input>)
    : Async<'output> =
    async.Bind(input, binder)

  let inline apply (applier: Async<'input -> 'output>) (input: Async<'input>) : Async<'output> =
    bind (fun f' -> bind (fun x' -> singleton (f' x')) input) applier

  let inline map
    ([<InlineIfLambda>] mapper: 'input -> 'output)
    (input: Async<'input>)
    : Async<'output> =
    bind (mapper >> singleton) input

  let inline map2
    ([<InlineIfLambda>] mapper: 'input1 -> 'input2 -> 'output)
    (input1: Async<'input1>)
    (input2: Async<'input2>)
    : Async<'output> =
    bind (fun x -> bind (mapper x >> singleton) input2) input1

  let inline map3
    ([<InlineIfLambda>] mapper: 'input1 -> 'input2 -> 'input3 -> 'output)
    (input1: Async<'input1>)
    (input2: Async<'input2>)
    (input3: Async<'input3>)
    : Async<'output> =
    bind (fun x -> bind (fun y -> bind (fun z -> mapper x y z |> singleton) input3) input2) input1


  /// Takes two asyncs and returns a tuple of the pair
  let inline zip (left: Async<'left>) (right: Async<'right>) : Async<'left * 'right> =
    bind (fun l -> bind (fun r -> singleton (l, r)) right) left
