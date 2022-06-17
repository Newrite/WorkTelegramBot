let date = System.DateTime.Parse("14.06.2022")
let date2 = System.DateTime.Parse("13.06.2022")
printfn "%A" (date.Ticks - date2.Ticks)
