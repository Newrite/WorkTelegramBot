let emptyList: int list = []
let listOfNumber = [ 1; 2; 3; 4; 5 ]

let selectEmpty =
  query {
    for e in emptyList do
      find (e = 20)
  }

let selectNumber =
  query {
    for number in listOfNumber do
      select number
  }
