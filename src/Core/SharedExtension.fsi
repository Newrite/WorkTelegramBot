
[<AutoOpen>]
module Operators

val inline (^) : f: ('a -> 'b) -> x: 'a -> 'b

[<RequireQualifiedAccess>]
module internal Channel

val tryReadAsync:
  mailbox: System.Threading.Channels.ChannelReader<'Msg> ->
    token: System.Threading.CancellationToken ->
    System.Threading.Tasks.Task<'Msg voption>

[<NoEquality; NoComparison; RequireQualifiedAccess; Struct>]
type internal AgentConstructor<'Msg> =
    | WithAgentInjected of
      BodyInjected: (Agent<'Msg> -> 'Msg -> System.Threading.Tasks.Task<unit>)
    | Default of Body: ('Msg -> System.Threading.Tasks.Task<unit>)

and [<Class>] Agent<'Msg> =
    interface System.IDisposable
    
    private new: agentCtor: AgentConstructor<'Msg> -> Agent<'Msg>
    
    static member
      MakeAndStartDefault: body: ('Msg -> System.Threading.Tasks.Task<unit>) ->
                             Agent<'Msg>
    
    static member
      MakeAndStartInjected: body: (Agent<'Msg> ->
                                     'Msg -> System.Threading.Tasks.Task<unit>) ->
                              Agent<'Msg>
    
    static member
      MakeDefault: body: ('Msg -> System.Threading.Tasks.Task<unit>) ->
                     Agent<'Msg>
    
    static member
      MakeInjected: body: (Agent<'Msg> ->
                             'Msg -> System.Threading.Tasks.Task<unit>) ->
                      Agent<'Msg>
    
    member Post: item: 'Msg -> unit
    
    member
      PostAndReply: buildMessage: (System.Threading.Tasks.TaskCompletionSource<'a> ->
                                     'Msg) -> 'a
    
    member
      PostAndReplyAsync: buildMessage: (System.Threading.Tasks.TaskCompletionSource<'a> ->
                                          'Msg) ->
                           System.Threading.Tasks.Task<'a>
    
    member Start: unit -> unit

[<RequireQualifiedAccess>]
module Agent

val post: item: 'a -> agent: Agent<'a> -> unit

val postAndReply:
  item: (System.Threading.Tasks.TaskCompletionSource<'a> -> 'b) ->
    agent: Agent<'b> -> 'a

val postAndReplyAsync:
  item: (System.Threading.Tasks.TaskCompletionSource<'a> -> 'b) ->
    agent: Agent<'b> -> System.Threading.Tasks.Task<'a>

module ChannelCollections

[<NoComparison; RequireQualifiedAccess; Struct>]
type private ChannelDictionaryMessage<'Key,'Value> =
    | AddOrUpdate of addKey: 'Key * addValue: 'Value
    | Remove of removeKey: 'Key
    | Get of
      key: 'Key * getTcs: System.Threading.Tasks.TaskCompletionSource<'Value>
    | Values of
      valueTcs:
        System.Threading.Tasks.TaskCompletionSource<System.Collections.Generic.Dictionary<'Key,'Value>.ValueCollection>
    | Keys of
      keysTcs:
        System.Threading.Tasks.TaskCompletionSource<System.Collections.Generic.Dictionary<'Key,'Value>.KeyCollection>
    | ContainsKey of
      containKey: 'Key *
      keyTcs: System.Threading.Tasks.TaskCompletionSource<bool>
    | TryGet of
      tryKey: 'Key *
      tryGetTcs:
        System.Threading.Tasks.TaskCompletionSource<ValueOption<'Value>>
    | ToList of
      listTcs: System.Threading.Tasks.TaskCompletionSource<('Key * 'Value) list>

type ChannelDictionary<'Key,'Value when 'Key: equality> =
    interface System.IDisposable
    
    new: unit -> ChannelDictionary<'Key,'Value>
    
    member AddOrUpdate: key: 'Key * value: 'Value -> unit
    
    member GetAsync: key: 'Key -> System.Threading.Tasks.Task<'Value>
    
    member
      KeysAsync: unit ->
                   System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<'Key,'Value>.KeyCollection>
    
    member Remove: key: 'Key -> unit
    
    member
      ToListAsync: unit -> System.Threading.Tasks.Task<('Key * 'Value) list>
    
    member
      TryGetAsync: key: 'Key -> System.Threading.Tasks.Task<ValueOption<'Value>>
    
    member
      ValuesAsync: unit ->
                     System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<'Key,'Value>.ValueCollection>
    
    member Item: key: 'Key -> System.Threading.Tasks.Task<'Value> with get

[<RequireQualifiedAccess>]
module ChannelDictionary =
    
    val remove:
      key: 'a -> dict: ChannelDictionary<'a,'b> -> unit when 'a: equality
    
    val addOrUpdate:
      key: 'a -> value: 'b -> dict: ChannelDictionary<'a,'b> -> unit
        when 'a: equality
    
    val getAsync:
      key: 'a ->
        dict: ChannelDictionary<'a,'b> -> System.Threading.Tasks.Task<'b>
        when 'a: equality
    
    val tryGetAsync:
      key: 'a ->
        dict: ChannelDictionary<'a,'b> ->
        System.Threading.Tasks.Task<ValueOption<'b>> when 'a: equality
    
    val valuesAsync:
      dict: ChannelDictionary<'a,'b> ->
        System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<'a,'b>.ValueCollection>
        when 'a: equality
    
    val keysAsync:
      dict: ChannelDictionary<'a,'b> ->
        System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<'a,'b>.KeyCollection>
        when 'a: equality
    
    val ofPair:
      key: 'a -> value: 'b -> ChannelDictionary<'a,'b> when 'a: equality
    
    val ofList:
      keyValuePairList: ('key * 'value) list -> ChannelDictionary<'key,'value>
        when 'key: equality
    
    val toListAsync:
      dict: ChannelDictionary<'a,'b> ->
        System.Threading.Tasks.Task<('a * 'b) list> when 'a: equality

type ChannelList<'Value> =
    
    new: unit -> ChannelList<'Value>

[<RequireQualifiedAccess>]
module ChannelList =
    
    val a: unit

type ExtBool =
    | True
    | False
    | Partial

type Either<'LeftValue,'RightValue> =
    | Left of 'LeftValue
    | Right of 'RightValue

[<RequireQualifiedAccess>]
module Either

val ifLeft: either: Either<'a,'b> -> bool

val ifRight: either: Either<'a,'b> -> bool

[<RequireQualifiedAccess>]
module Option

val inline string: optionValue: 'a option -> string

val inline ofValueOption: vopt: 'value voption -> 'value option

val inline toValueOption: opt: 'value option -> 'value voption

val inline traverseResult:
  [<InlineIfLambda>] binder: ('input -> Result<'okOutput,'error>) ->
    input: 'input option -> Result<'okOutput option,'error>

val inline sequenceResult:
  opt: Result<'ok,'error> option -> Result<'ok option,'error>

val inline tryParse:
  valueToParse: string -> ^T option
    when ^T: (static member TryParse: string * byref<^T> -> bool) and
         ^T: (new: unit -> ^T)

val inline tryGetValue:
  key: string -> dictionary: ^Dictionary -> ^value option
    when ^Dictionary: (member TryGetValue: string * byref<^value> -> bool)

/// <summary>
/// Takes two options and returns a tuple of the pair or none if either are none
/// </summary>
/// <param name="option1">The input option</param>
/// <param name="option2">The input option</param>
/// <returns></returns>
val inline zip:
  left: 'left option -> right: 'right option -> ('left * 'right) option

val inline ofResult: r: Result<'ok,'error> -> 'ok option

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
val inline ofNull: value: 'nullableValue -> 'nullableValue option

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
val inline bindNull:
  [<InlineIfLambda>] binder: ('value -> 'nullableValue) ->
    option: Option<'value> -> 'nullableValue option

[<AutoOpen>]
module OptionCE

type OptionBuilder =
    
    new: unit -> OptionBuilder
    
    member
      inline Bind: m: 'input *
                   [<InlineIfLambda>] binder: ('input -> 'output option) ->
                     'output option when 'input: null
    
    member
      inline Bind: input: 'input option *
                   [<InlineIfLambda>] binder: ('input -> 'output option) ->
                     'output option
    
    member
      inline BindReturn: x: 'input * [<InlineIfLambda>] f: ('input -> 'output) ->
                           'output option when 'input: null
    
    member
      inline BindReturn: input: 'input option *
                         [<InlineIfLambda>] mapper: ('input -> 'output) ->
                           'output option
    
    member
      inline Combine: m1: unit option * m2: 'output option -> 'output option
    
    member
      inline Combine: m: 'input option *
                      [<InlineIfLambda>] binder: ('input -> 'output option) ->
                        'output option
    
    member
      inline Delay: [<InlineIfLambda>] delayer: (unit -> 'value option) ->
                      (unit -> 'value option)
    
    member
      inline For: sequence: #('value seq) *
                  [<InlineIfLambda>] binder: ('value -> unit option) ->
                    unit option
    
    member
      inline MergeSources: option1: 'left option * option2: 'right option ->
                             ('left * 'right) option
    
    member inline Return: x: 'value -> 'value option
    
    member inline ReturnFrom: m: 'value option -> 'value option
    
    member inline Run: [<InlineIfLambda>] delayed: (unit -> 'c) -> 'c
    
    member inline Source: vopt: 'value voption -> 'value option
    
    /// <summary>
    /// Method lets us transform data types into our internal representation.  self is the identity method to recognize the self type.
    ///
    /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
    /// </summary>
    member inline Source: result: 'value option -> 'value option
    
    member
      inline TryFinally: [<InlineIfLambda>] computation: (unit -> 'b) *
                         compensation: (unit -> unit) -> 'b
    
    member
      inline TryWith: [<InlineIfLambda>] computation: (unit -> 'value) *
                      handler: (exn -> 'value) -> 'value
    
    member
      inline Using: resource: 'disposable *
                    [<InlineIfLambda>] binder: ('disposable -> 'value option) ->
                      'value option when 'disposable :> System.IDisposable
    
    member
      inline While: [<InlineIfLambda>] guard: (unit -> bool) *
                    [<InlineIfLambda>] computation: (unit -> unit option) ->
                      unit option
    
    member inline Zero: unit -> unit option

val option: OptionBuilder

[<AutoOpen>]
module OptionExtensionsLower
type OptionCE.OptionBuilder with
    
    member inline Source: nullableObj: 'value -> 'value option when 'value: null
type OptionCE.OptionBuilder with
    
    member inline Source: m: string -> string option
type OptionCE.OptionBuilder with
    
    member
      inline MergeSources: nullableObj1: 'left * option2: 'right option ->
                             ('left * 'right) option when 'left: null
type OptionCE.OptionBuilder with
    
    member
      inline MergeSources: option1: 'left option * nullableObj2: 'right ->
                             ('left * 'right) option when 'right: null
type OptionCE.OptionBuilder with
    
    member
      inline MergeSources: nullableObj1: 'left * nullableObj2: 'right ->
                             ('left * 'right) option
                             when 'left: null and 'right: null

[<AutoOpen>]
module OptionExtensions
type OptionCE.OptionBuilder with
    
    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline Source: s: 'a -> 'a when 'a :> 'value seq
type OptionCE.OptionBuilder with
    
    member
      inline Source: nullable: System.Nullable<'value> -> 'value option
                       when 'value: (new: unit -> 'value) and 'value: struct and
                            'value :> System.ValueType

[<RequireQualifiedAccess>]
module Result

val inline map:
  [<InlineIfLambda>] mapper: ('okInput -> 'okOutput) ->
    input: Result<'okInput,'error> -> Result<'okOutput,'error>

val inline mapError:
  [<InlineIfLambda>] errorMapper: ('errorInput -> 'errorOutput) ->
    input: Result<'ok,'errorInput> -> Result<'ok,'errorOutput>

val inline bind:
  [<InlineIfLambda>] binder: ('okInput -> Result<'okOutput,'error>) ->
    input: Result<'okInput,'error> -> Result<'okOutput,'error>

val inline isOk: value: Result<'ok,'error> -> bool

val inline isError: value: Result<'ok,'error> -> bool

val inline either:
  [<InlineIfLambda>] onOk: ('okInput -> 'output) ->
    [<InlineIfLambda>] onError: ('errorInput -> 'output) ->
    input: Result<'okInput,'errorInput> -> 'output

val inline eitherMap:
  [<InlineIfLambda>] onOk: ('okInput -> 'okOutput) ->
    [<InlineIfLambda>] onError: ('errorInput -> 'errorOutput) ->
    input: Result<'okInput,'errorInput> -> Result<'okOutput,'errorOutput>

val inline apply:
  applier: Result<('okInput -> 'okOutput),'error> ->
    input: Result<'okInput,'error> -> Result<'okOutput,'error>

val inline map2:
  [<InlineIfLambda>] mapper: ('okInput1 -> 'okInput2 -> 'okOutput) ->
    input1: Result<'okInput1,'error> ->
    input2: Result<'okInput2,'error> -> Result<'okOutput,'error>

val inline map3:
  [<InlineIfLambda>] mapper: ('okInput1 -> 'okInput2 -> 'okInput3 -> 'okOutput) ->
    input1: Result<'okInput1,'error> ->
    input2: Result<'okInput2,'error> ->
    input3: Result<'okInput3,'error> -> Result<'okOutput,'error>

val inline fold:
  [<InlineIfLambda>] onOk: ('okInput -> 'output) ->
    [<InlineIfLambda>] onError: ('errorInput -> 'output) ->
    input: Result<'okInput,'errorInput> -> 'output

val inline ofChoice: input: Choice<'ok,'error> -> Result<'ok,'error>

val inline tryCreate:
  fieldName: string -> x: 'a -> Result<^b,(string * 'c)>
    when ^b: (static member TryCreate: 'a -> Result<^b,'c>)

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
val inline orElse:
  ifError: Result<'ok,'errorOutput> ->
    result: Result<'ok,'error> -> Result<'ok,'errorOutput>

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
val inline orElseWith:
  [<InlineIfLambda>] ifErrorFunc: ('error -> Result<'ok,'errorOutput>) ->
    result: Result<'ok,'error> -> Result<'ok,'errorOutput>

/// Replaces the wrapped value with unit
val inline ignore: result: Result<'ok,'error> -> Result<unit,'error>

/// Returns the specified error if the value is false.
val inline requireTrue: error: 'error -> value: bool -> Result<unit,'error>

/// Returns the specified error if the value is true.
val inline requireFalse: error: 'error -> value: bool -> Result<unit,'error>

/// Converts an Option to a Result, using the given error if None.
val inline requireSome:
  error: 'error -> option: 'ok option -> Result<'ok,'error>

/// Converts an Option to a Result, using the given error if Some.
val inline requireNone:
  error: 'error -> option: 'value option -> Result<unit,'error>

/// Converts a nullable value into a Result, using the given error if null
val inline requireNotNull:
  error: 'error -> value: 'ok -> Result<'ok,'error> when 'ok: null

/// Returns Ok if the two values are equal, or the specified error if not.
/// Same as requireEqual, but with a signature that fits piping better than
/// normal function application.
val inline requireEqualTo:
  other: 'value -> error: 'error -> self: 'value -> Result<unit,'error>
    when 'value: equality

/// Returns Ok if the two values are equal, or the specified error if not.
/// Same as requireEqualTo, but with a signature that fits normal function
/// application better than piping.
val inline requireEqual:
  x1: 'value -> x2: 'value -> error: 'error -> Result<unit,'error>
    when 'value: equality

/// Returns Ok if the sequence is empty, or the specified error if not.
val inline requireEmpty:
  error: 'error -> xs: #('value seq) -> Result<unit,'error>

/// Returns the specified error if the sequence is empty, or Ok if not.
val inline requireNotEmpty:
  error: 'error -> xs: #('value seq) -> Result<unit,'error>

/// Returns the first item of the sequence if it exists, or the specified
/// error if the sequence is empty
val inline requireHead: error: 'error -> xs: #('ok seq) -> Result<'ok,'error>

/// Replaces an error value with a custom error value.
val inline setError:
  error: 'error -> result: Result<'ok,'errorIgnored> -> Result<'ok,'error>

/// Replaces a unit error value with a custom error value. Safer than setError
/// since you're not losing any information.
val inline withError:
  error: 'error -> result: Result<'ok,unit> -> Result<'ok,'error>

/// Returns the contained value if Ok, otherwise returns ifError.
val inline defaultValue: ifError: 'ok -> result: Result<'ok,'error> -> 'ok

val inline defaultError: ifOk: 'error -> result: Result<'ok,'error> -> 'error

/// Returns the contained value if Ok, otherwise evaluates ifErrorThunk and
/// returns the result.
val inline defaultWith:
  [<InlineIfLambda>] ifErrorThunk: (unit -> 'ok) ->
    result: Result<'ok,'error> -> 'ok

/// Same as defaultValue for a result where the Ok value is unit. The name
/// describes better what is actually happening in self case.
val inline ignoreError: result: Result<unit,'error> -> unit

/// If the result is Ok and the predicate returns true, executes the function
/// on the Ok value. Passes through the input value.
val inline teeIf:
  [<InlineIfLambda>] predicate: ('ok -> bool) ->
    [<InlineIfLambda>] inspector: ('ok -> unit) ->
    result: Result<'ok,'error> -> Result<'ok,'error>

/// If the result is Error and the predicate returns true, executes the
/// function on the Error value. Passes through the input value.
val inline teeErrorIf:
  [<InlineIfLambda>] predicate: ('error -> bool) ->
    [<InlineIfLambda>] inspector: ('error -> unit) ->
    result: Result<'ok,'error> -> Result<'ok,'error>

/// If the result is Ok, executes the function on the Ok value. Passes through
/// the input value.
val inline tee:
  [<InlineIfLambda>] inspector: ('ok -> unit) ->
    result: Result<'ok,'error> -> Result<'ok,'error>

/// If the result is Error, executes the function on the Error value. Passes
/// through the input value.
val inline teeError:
  [<InlineIfLambda>] inspector: ('error -> unit) ->
    result: Result<'ok,'error> -> Result<'ok,'error>

/// Converts a Result<Async<_>,_> to an Async<Result<_,_>>
val inline sequenceAsync:
  resAsync: Result<Async<'ok>,'error> -> Async<Result<'ok,'error>>

val inline traverseAsync:
  [<InlineIfLambda>] f: ('okInput -> Async<'okOutput>) ->
    res: Result<'okInput,'error> -> Async<Result<'okOutput,'error>>

/// Returns the Ok value or runs the specified function over the error value.
val inline valueOr:
  [<InlineIfLambda>] f: ('error -> 'ok) -> res: Result<'ok,'error> -> 'ok

/// Takes two results and returns a tuple of the pair
val zip:
  left: Result<'leftOk,'error> ->
    right: Result<'rightOk,'error> -> Result<('leftOk * 'rightOk),'error>

/// Takes two results and returns a tuple of the error pair
val zipError:
  left: Result<'ok,'leftError> ->
    right: Result<'ok,'rightError> -> Result<'ok,('leftError * 'rightError)>

[<AutoOpen>]
module ResultCE

type ResultBuilder =
    
    new: unit -> ResultBuilder
    
    member
      inline Bind: input: Result<'okInput,'error> *
                   [<InlineIfLambda>] binder: ('okInput ->
                                                 Result<'okOutput,'error>) ->
                     Result<'okOutput,'error>
    
    member
      inline BindReturn: x: Result<'okInput,'error> *
                         [<InlineIfLambda>] f: ('okInput -> 'okOutput) ->
                           Result<'okOutput,'error>
    
    member
      inline Combine: result: Result<unit,'error> *
                      [<InlineIfLambda>] binder: (unit -> Result<'ok,'error>) ->
                        Result<'ok,'error>
    
    member
      inline Delay: [<InlineIfLambda>] generator: (unit -> Result<'ok,'error>) ->
                      (unit -> Result<'ok,'error>)
    
    member
      inline For: sequence: #('T seq) *
                  [<InlineIfLambda>] binder: ('T -> Result<unit,'TError>) ->
                    Result<unit,'TError>
    
    member
      inline MergeSources: left: Result<'left,'error> *
                           right: Result<'right,'error> ->
                             Result<('left * 'right),'error>
    
    member inline Return: value: 'ok -> Result<'ok,'error>
    
    member inline ReturnFrom: result: Result<'ok,'error> -> Result<'ok,'error>
    
    member
      inline Run: [<InlineIfLambda>] generator: (unit -> Result<'ok,'error>) ->
                    Result<'ok,'error>
    
    /// <summary>
    /// Method lets us transform data types into our internal representation.  self is the identity method to recognize the self type.
    ///
    /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    member inline Source: result: Result<'ok,'error> -> Result<'ok,'error>
    
    member
      inline TryFinally: [<InlineIfLambda>] generator: (unit ->
                                                          Result<'ok,'error>) *
                         [<InlineIfLambda>] compensation: (unit -> unit) ->
                           Result<'ok,'error>
    
    member
      inline TryWith: [<InlineIfLambda>] generator: (unit -> Result<'T,'TError>) *
                      [<InlineIfLambda>] handler: (exn -> Result<'T,'TError>) ->
                        Result<'T,'TError>
    
    member
      inline Using: resource: 'disposable *
                    binder: ('disposable -> Result<'ok,'error>) ->
                      Result<'ok,'error> when 'disposable :> System.IDisposable
    
    member
      inline While: [<InlineIfLambda>] guard: (unit -> bool) *
                    [<InlineIfLambda>] generator: (unit -> Result<unit,'error>) ->
                      Result<unit,'error>
    
    member Zero: unit -> Result<unit,'error>

val result: ResultBuilder

[<AutoOpen>]
module ResultCEExtensions
type ResultCE.ResultBuilder with
    
    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline Source: s: 'a -> 'a when 'a :> 'b seq

[<AutoOpen>]
module ResultCEChoiceExtensions
type ResultCE.ResultBuilder with
    
    /// <summary>
    /// Method lets us transform data types into our internal representation.
    /// </summary>
    /// <returns></returns>
    member inline Source: choice: Choice<'ok,'error> -> Result<'ok,'error>

/// Validation<'a, 'err> is defined as Result<'a, 'err list> meaning you can use many of the functions found in the Result module.
type Validation<'Ok,'Error> = Result<'Ok,'Error list>

[<RequireQualifiedAccess>]
module Validation

val inline ok: value: 'ok -> Validation<'ok,'error>

val inline error: error: 'error -> Validation<'ok,'error>

val inline ofResult: result: Result<'ok,'error> -> Validation<'ok,'error>

val inline ofChoice: choice: Choice<'ok,'error> -> Validation<'ok,'error>

val inline apply:
  applier: Validation<('okInput -> 'okOutput),'error> ->
    input: Validation<'okInput,'error> -> Validation<'okOutput,'error>

val inline retn: value: 'ok -> Validation<'ok,'error>

val inline returnError: error: 'error -> Validation<'ok,'error>

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
val inline orElse:
  ifError: Validation<'ok,'errorOutput> ->
    result: Validation<'ok,'errorInput> -> Validation<'ok,'errorOutput>

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
val inline orElseWith:
  [<InlineIfLambda>] ifErrorFunc: ('errorInput list ->
                                     Validation<'ok,'errorOutput>) ->
    result: Validation<'ok,'errorInput> -> Validation<'ok,'errorOutput>

val inline map:
  [<InlineIfLambda>] mapper: ('okInput -> 'okOutput) ->
    input: Validation<'okInput,'error> -> Validation<'okOutput,'error>

val inline map2:
  [<InlineIfLambda>] mapper: ('okInput1 -> 'okInput2 -> 'okOutput) ->
    input1: Validation<'okInput1,'error> ->
    input2: Validation<'okInput2,'error> -> Validation<'okOutput,'error>

val inline map3:
  [<InlineIfLambda>] mapper: ('okInput1 -> 'okInput2 -> 'okInput3 -> 'okOutput) ->
    input1: Validation<'okInput1,'error> ->
    input2: Validation<'okInput2,'error> ->
    input3: Validation<'okInput3,'error> -> Validation<'okOutput,'error>

val inline mapError:
  [<InlineIfLambda>] errorMapper: ('errorInput -> 'errorOutput) ->
    input: Validation<'ok,'errorInput> -> Validation<'ok,'errorOutput>

val inline mapErrors:
  [<InlineIfLambda>] errorMapper: ('errorInput list -> 'errorOutput list) ->
    input: Validation<'ok,'errorInput> -> Validation<'ok,'errorOutput>

val inline bind:
  [<InlineIfLambda>] binder: ('okInput -> Validation<'okOutput,'error>) ->
    input: Validation<'okInput,'error> -> Validation<'okOutput,'error>

val inline zip:
  left: Validation<'left,'error> ->
    right: Validation<'right,'error> -> Validation<('left * 'right),'error>

[<AutoOpen>]
module ValidationCE

type ValidationBuilder =
    
    new: unit -> ValidationBuilder
    
    member
      inline Bind: result: Validation<'okInput,'error> *
                   [<InlineIfLambda>] binder: ('okInput ->
                                                 Validation<'okOutput,'error>) ->
                     Validation<'okOutput,'error>
    
    member
      inline BindReturn: input: Validation<'okInput,'error> *
                         [<InlineIfLambda>] mapper: ('okInput -> 'okOutput) ->
                           Validation<'okOutput,'error>
    
    member
      inline Combine: result: Validation<unit,'error> *
                      [<InlineIfLambda>] binder: (unit -> Validation<'ok,'error>) ->
                        Validation<'ok,'error>
    
    member
      inline Delay: [<InlineIfLambda>] generator: (unit ->
                                                     Validation<'ok,'error>) ->
                      (unit -> Validation<'ok,'error>)
    
    member
      inline For: sequence: #('ok seq) *
                  [<InlineIfLambda>] binder: ('ok -> Validation<unit,'error>) ->
                    Validation<unit,'error>
    
    member
      inline MergeSources: left: Validation<'left,'error> *
                           right: Validation<'right,'error> ->
                             Validation<('left * 'right),'error>
    
    member inline Return: value: 'ok -> Validation<'ok,'error>
    
    member
      inline ReturnFrom: result: Validation<'ok,'error> ->
                           Validation<'ok,'error>
    
    member
      inline Run: [<InlineIfLambda>] generator: (unit -> Validation<'ok,'error>) ->
                    Validation<'ok,'error>
    
    /// <summary>
    /// Method lets us transform data types into our internal representation.  self is the identity method to recognize the self type.
    ///
    /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    member
      inline Source: result: Validation<'ok,'error> -> Validation<'ok,'error>
    
    member
      inline TryFinally: [<InlineIfLambda>] generator: (unit ->
                                                          Validation<'ok,'error>) *
                         [<InlineIfLambda>] compensation: (unit -> unit) ->
                           Validation<'ok,'error>
    
    member
      inline TryWith: [<InlineIfLambda>] generator: (unit ->
                                                       Validation<'ok,'error>) *
                      [<InlineIfLambda>] handler: (exn -> Validation<'ok,'error>) ->
                        Validation<'ok,'error>
    
    member
      inline Using: resource: 'disposable *
                    [<InlineIfLambda>] binder: ('disposable ->
                                                  Validation<'okOutput,'error>) ->
                      Validation<'okOutput,'error>
                      when 'disposable :> System.IDisposable
    
    member
      inline While: [<InlineIfLambda>] guard: (unit -> bool) *
                    [<InlineIfLambda>] generator: (unit ->
                                                     Validation<unit,'error>) ->
                      Validation<unit,'error>
    
    member inline Zero: unit -> Validation<unit,'error>

val validation: ValidationBuilder

[<AutoOpen>]
module ValidationCEExtensions
type ValidationCE.ValidationBuilder with
    
    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline Source: s: 'a -> 'a when 'a :> 'b seq
type ValidationCE.ValidationBuilder with
    
    /// <summary>
    /// Method lets us transform data types into our internal representation.
    /// </summary>
    member inline Source: s: Result<'ok,'error> -> Validation<'ok,'error>
type ValidationCE.ValidationBuilder with
    
    /// <summary>
    /// Method lets us transform data types into our internal representation.
    /// </summary>
    /// <returns></returns>
    member inline Source: choice: Choice<'ok,'error> -> Validation<'ok,'error>

[<RequireQualifiedAccess>]
module ValueOption

val inline ofOption: opt: 'value option -> 'value voption

val inline toOption: vopt: 'value voption -> 'value option

val inline traverseResult:
  [<InlineIfLambda>] binder: ('okInput -> Result<'okOutput,'error>) ->
    input: 'okInput voption -> Result<'okOutput voption,'error>

val inline sequenceResult:
  opt: Result<'okOutput,'error> voption -> Result<'okOutput voption,'error>

val inline tryParse:
  valueToParse: string -> ^value voption
    when ^value: (static member TryParse: string * byref<^value> -> bool) and
         ^value: (new: unit -> ^value)

val inline tryGetValue:
  key: string -> dictionary: ^Dictionary -> ^value voption
    when ^Dictionary: (member TryGetValue: string * byref<^value> -> bool)

/// <summary>
/// Takes two voptions and returns a tuple of the pair or none if either are none
/// </summary>
/// <param name="voption1">The input option</param>
/// <param name="voption2">The input option</param>
/// <returns></returns>
val inline zip:
  left: 'left voption -> right: 'right voption -> ('left * 'right) voption

val inline ofResult: result: Result<'ok,'error> -> 'ok voption

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
val inline ofNull: value: 'nullableValue -> 'nullableValue voption

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
val inline bindNull:
  [<InlineIfLambda>] binder: ('value -> 'nullableValue) ->
    voption: ValueOption<'value> -> 'nullableValue voption

[<AutoOpen>]
module ValueOptionCE

type ValueOptionBuilder =
    
    new: unit -> ValueOptionBuilder
    
    member
      inline Bind: input: 'input *
                   [<InlineIfLambda>] binder: ('input -> 'output voption) ->
                     'output voption when 'input: null
    
    member
      inline Bind: input: 'input voption *
                   [<InlineIfLambda>] binder: ('input -> 'output voption) ->
                     'output voption
    
    member
      inline BindReturn: x: 'e * [<InlineIfLambda>] f: ('e -> 'f) -> 'f voption
                           when 'e: null
    
    member
      inline BindReturn: x: 'g voption * [<InlineIfLambda>] f: ('g -> 'h) ->
                           'h voption
    
    member
      inline Combine: input: unit voption * output: 'output voption ->
                        'output voption
    
    member
      inline Combine: input: 'input voption *
                      [<InlineIfLambda>] binder: ('input -> 'output voption) ->
                        'output voption
    
    member inline Delay: [<InlineIfLambda>] f: (unit -> 'a) -> (unit -> 'a)
    
    member
      inline For: sequence: #('T seq) *
                  [<InlineIfLambda>] binder: ('T -> unit voption) ->
                    unit voption
    
    member
      inline MergeSources: option1: 'c voption * option2: 'd voption ->
                             ('c * 'd) voption
    
    member inline Return: x: 'value -> 'value voption
    
    member inline ReturnFrom: m: 'value voption -> 'value voption
    
    member inline Run: [<InlineIfLambda>] f: (unit -> 'v) -> 'v
    
    member inline Source: vopt: 'a option -> 'a voption
    
    /// <summary>
    /// Method lets us transform data types into our internal representation.  self is the identity method to recognize the self type.
    ///
    /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
    /// </summary>
    member inline Source: result: 'b voption -> 'b voption
    
    member
      inline TryFinally: [<InlineIfLambda>] m: (unit -> 'k) *
                         [<InlineIfLambda>] compensation: (unit -> unit) -> 'k
    
    member
      inline TryWith: [<InlineIfLambda>] m: (unit -> 'l) *
                      [<InlineIfLambda>] handler: (exn -> 'l) -> 'l
    
    member
      inline Using: resource: 'T * [<InlineIfLambda>] binder: ('T -> 'j voption) ->
                      'j voption when 'T :> System.IDisposable
    
    member
      inline While: [<InlineIfLambda>] guard: (unit -> bool) *
                    [<InlineIfLambda>] generator: (unit -> unit voption) ->
                      unit voption
    
    member inline Zero: unit -> unit voption

val voption: ValueOptionBuilder

[<AutoOpen>]
module ValueOptionExtensionsLower
type ValueOptionCE.ValueOptionBuilder with
    
    member inline Source: nullableObj: 'a -> 'a voption when 'a: null
type ValueOptionCE.ValueOptionBuilder with
    
    member inline Source: m: string -> string voption
type ValueOptionCE.ValueOptionBuilder with
    
    member
      inline MergeSources: nullableObj1: 'e * option2: 'f voption ->
                             ('e * 'f) voption when 'e: null
type ValueOptionCE.ValueOptionBuilder with
    
    member
      inline MergeSources: option1: 'c voption * nullableObj2: 'd ->
                             ('c * 'd) voption when 'd: null
type ValueOptionCE.ValueOptionBuilder with
    
    member
      inline MergeSources: nullableObj1: 'a * nullableObj2: 'b ->
                             ('a * 'b) voption when 'a: null and 'b: null

[<AutoOpen>]
module ValueOptionExtensions
type ValueOptionCE.ValueOptionBuilder with
    
    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline Source: s: 'a -> 'a when 'a :> 'b seq
type ValueOptionCE.ValueOptionBuilder with
    
    member
      inline Source: nullable: System.Nullable<'a> -> 'a voption
                       when 'a: (new: unit -> 'a) and 'a: struct and
                            'a :> System.ValueType

[<RequireQualifiedAccess>]
module Async

val inline singleton: value: 'value -> Async<'value>

val inline retn: value: 'value -> Async<'value>

val inline bind:
  [<InlineIfLambda>] binder: ('input -> Async<'output>) ->
    input: Async<'input> -> Async<'output>

val inline apply:
  applier: Async<('input -> 'output)> -> input: Async<'input> -> Async<'output>

val inline map:
  [<InlineIfLambda>] mapper: ('input -> 'output) ->
    input: Async<'input> -> Async<'output>

val inline map2:
  [<InlineIfLambda>] mapper: ('input1 -> 'input2 -> 'output) ->
    input1: Async<'input1> -> input2: Async<'input2> -> Async<'output>

val inline map3:
  [<InlineIfLambda>] mapper: ('input1 -> 'input2 -> 'input3 -> 'output) ->
    input1: Async<'input1> ->
    input2: Async<'input2> -> input3: Async<'input3> -> Async<'output>

/// Takes two asyncs and returns a tuple of the pair
val inline zip:
  left: Async<'left> -> right: Async<'right> -> Async<'left * 'right>

