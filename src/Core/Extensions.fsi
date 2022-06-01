namespace WorkTelegram.Core
    
    module Ext =
        
        val inline (^) : f: ('a -> 'b) -> x: 'a -> 'b
    
    module Funogram =
        
        module Telegram =
            
            module Types =
                
                /// This object represents a bot command.
                type BotCommand =
                    {
                      
                      /// Text of the command; 1-32 characters. Can contain only lowercase English letters, digits and underscores.
                      Command: string
                      
                      /// Description of the command; 1-256 characters.
                      Description: string
                    }
            
            module RequestsTypes =
                
                type SetMyCommandsReq =
                    {
                      Commands: Types.BotCommand array
                      LaungeageCode: string option
                    }
                    interface Funogram.Types.IRequestBase<bool>
                
                type DeleteMyCommandsReq =
                    { LaungeageCode: string option }
                    interface Funogram.Types.IRequestBase<bool>
                
                type GetMyCommandsReq =
                    { LaungeageCode: string option }
                    interface
                        Funogram.Types.IRequestBase<Types.BotCommand array>
            
            module Api =
                
                val private setMyCommandsBase:
                  commands: Types.BotCommand array
                  -> languageCode: string option
                    -> RequestsTypes.SetMyCommandsReq
                
                val setMyCommands:
                  commands: Types.BotCommand array
                    -> RequestsTypes.SetMyCommandsReq
                
                val setMyCommandsWithLanguageCode:
                  commands: Types.BotCommand array -> languageCode: string
                    -> RequestsTypes.SetMyCommandsReq
                
                val private getMyCommandsBase:
                  languageCode: string option -> RequestsTypes.GetMyCommandsReq
                
                val getMyCommands: unit -> RequestsTypes.GetMyCommandsReq
                
                val getMyCommandsWithLanguageCode:
                  languageCode: string -> RequestsTypes.GetMyCommandsReq
                
                val private deleteMyCommandsBase:
                  languageCode: string option
                    -> RequestsTypes.DeleteMyCommandsReq
                
                val deleteMyCommands: unit -> RequestsTypes.DeleteMyCommandsReq
                
                val deleteMyCommandsWithLanguageCode:
                  languageCode: string -> RequestsTypes.DeleteMyCommandsReq

