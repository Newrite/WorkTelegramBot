namespace WorkTelegram.Core

module Resources = 

  module Keyboard =
    
    let [<Literal>] BackTxt              = "Назад"
    let [<Literal>] DeletionTxt          = "Списать"
    let [<Literal>] WithoutSerialTxt     = "Без серийника"
    let [<Literal>] EnterSerialTxt       = "Ввести"
    let [<Literal>] WithoutMacAddressTxt = "Без мака"
    let [<Literal>] EnterMacAddressTxt   = "Ввести"
    let [<Literal>] EnterLocationTxt     = "Ввести"
    let [<Literal>] WithoutLocationTxt   = "Не указывать"
    let [<Literal>] CancelTxt            = "Отмена"
  
  module Callback =
    
    let [<Literal>] BackCD               = "back"
    let [<Literal>] DeletionCD           = "deletion"
    let [<Literal>] WithoutSerialCD      = "without_serial"
    let [<Literal>] EnterSerialCD        = "enter_serial"
    let [<Literal>] WithoutMacAddressCD  = "without_macaddress"
    let [<Literal>] EnterMacAddressCD    = "enter_macaddress"
    let [<Literal>] EnterLocationCD      = "enterl_location"
    let [<Literal>] WithoutLocationCD    = "without_location"
    let [<Literal>] CancelCD             = "cancel"
  
  module Commands =
  
    let [<Literal>] StartTxt = "/start"