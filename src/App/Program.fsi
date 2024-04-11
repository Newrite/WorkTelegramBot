
module Main

type A =
    
    new: unit -> A
    
    member Printer: unit -> string

type B =
    inherit A
    
    new: unit -> B
    
    member Printer: unit -> string

val main: string array -> int

