!ifndef WINCRAFT_UNINSTALL_COMMON_NSH
!define WINCRAFT_UNINSTALL_COMMON_NSH

; Shared uninstall logic for standalone current-user and all-users uninstallers.
;
; Compile-time switches:
;   WINCRAFT_UNINSTALL_SET_BUTTON_TEXT  — change Next button from "Install" to "Uninstall"
;   WINCRAFT_UNINSTALL_ALL_USERS_RUNTIME_DATA — scan all user profiles for runtime data

!macro WINCRAFT_UNINSTALL_RUNTIME_DATA_PAGE FUNCTION_PREFIX
Function ${FUNCTION_PREFIX}RuntimeDataPageCreate
  !insertmacro MUI_HEADER_TEXT "$(RUNTIMEDATA_PAGE_TITLE)" "$(RUNTIMEDATA_PAGE_SUBTITLE)"

  nsDialogs::Create 1018
  Pop $0
  ${If} $0 == error
    Abort
  ${EndIf}

  !ifdef WINCRAFT_UNINSTALL_SET_BUTTON_TEXT
    GetDlgItem $0 $HWNDPARENT 1
    SendMessage $0 ${WM_SETTEXT} 0 "STR:$(UNINSTALL_BUTTON)"
  !endif

  ${NSD_CreateLabel} 0 0 100% 18u "$(RUNTIMEDATA_HEADER)"
  Pop $0

  ${NSD_CreateCheckbox} 20u 28u 260u 12u "$(RUNTIMEDATA_CONFIG)"
  Pop $KeepConfigCheckbox
  ${NSD_Check} $KeepConfigCheckbox

  ${NSD_CreateCheckbox} 20u 48u 260u 12u "$(RUNTIMEDATA_LOGS_DUMPS)"
  Pop $KeepLogsDumpsCheckbox
  ${NSD_Check} $KeepLogsDumpsCheckbox

  nsDialogs::Show
FunctionEnd

Function ${FUNCTION_PREFIX}RuntimeDataPageLeave
  ${NSD_GetState} $KeepConfigCheckbox $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $KeepConfig "1"
  ${Else}
    StrCpy $KeepConfig "0"
  ${EndIf}

  ${NSD_GetState} $KeepLogsDumpsCheckbox $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $KeepLogs "1"
    StrCpy $KeepDumps "1"
  ${Else}
    StrCpy $KeepLogs "0"
    StrCpy $KeepDumps "0"
  ${EndIf}
FunctionEnd
!macroend

!macro WINCRAFT_UNINSTALL_DELETE_FUNCTIONS FUNCTION_PREFIX
Function ${FUNCTION_PREFIX}DeleteInstalledFiles
  IfFileExists "$TEMP\wincraft-manifest.txt" 0 done
  FileOpen $0 "$TEMP\wincraft-manifest.txt" r
  IfErrors done
  fileLoop:
    ClearErrors
    FileRead $0 $2
    IfErrors fileClose
    StrCpy $2 $2 -2
    ${If} $2 != ""
      Delete "$INSTDIR\$2"
    ${EndIf}
    Goto fileLoop
  fileClose:
    FileClose $0
    Delete "$TEMP\wincraft-manifest.txt"
  done:
FunctionEnd

Function ${FUNCTION_PREFIX}DeleteRuntimeData
  ; For all-users installs, iterate every user profile to clean up per-user
  ; runtime data (config, logs, dumps) under %LOCALAPPDATA%\WinCraft.
  ; For per-user installs, only the current user's data is removed.
  !ifdef WINCRAFT_UNINSTALL_ALL_USERS_RUNTIME_DATA
    FindFirst $3 $4 "$PROFILE\..\*"
    userLoop:
      StrCmp $4 "" userDone
      StrCmp $4 "." nextUser
      StrCmp $4 ".." nextUser

      StrCpy $5 "$PROFILE\..\$4\AppData\Local\WinCraft"
      ${If} $KeepConfig != "1"
        RMDir /r "$5\Config"
      ${EndIf}

      ${If} $KeepLogs != "1"
        RMDir /r "$5\Logs"
      ${EndIf}

      ${If} $KeepDumps != "1"
        RMDir /r "$5\Dumps"
      ${EndIf}

      RMDir "$5"

      nextUser:
        FindNext $3 $4
        Goto userLoop

    userDone:
      FindClose $3
  !else
    ${If} $KeepConfig != "1"
      RMDir /r "$LOCALAPPDATA\WinCraft\Config"
    ${EndIf}

    ${If} $KeepLogs != "1"
      RMDir /r "$LOCALAPPDATA\WinCraft\Logs"
    ${EndIf}

    ${If} $KeepDumps != "1"
      RMDir /r "$LOCALAPPDATA\WinCraft\Dumps"
    ${EndIf}

    RMDir "$LOCALAPPDATA\WinCraft"
  !endif
FunctionEnd
!macroend

!endif
