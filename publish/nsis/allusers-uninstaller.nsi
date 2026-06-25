Unicode True
CRCCheck on
SetCompressor /SOLID lzma
SetCompressorDictSize 32

!ifndef OutFile
  !error "OutFile define is required."
!endif

!ifndef Version
  !define Version "0.0.0"
!endif

!ifndef IconPath
  !error "IconPath define is required."
!endif

!ifndef SourceDir
  !error "SourceDir define is required."
!endif

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"
!include "nsDialogs.nsh"
!define WINCRAFT_UNINSTALL_ALL_USERS_RUNTIME_DATA
!include "uninstall-common.nsh"

Name "WinCraft ${Version}"
OutFile "${OutFile}"
InstallDir "$PROGRAMFILES\WinCraft"
RequestExecutionLevel admin

!define MUI_ICON "${IconPath}"
!define MUI_UNICON "${IconPath}"
!define WINCRAFT_UNINSTALL_SET_BUTTON_TEXT
Page custom RuntimeDataPageCreate RuntimeDataPageLeave
!define MUI_TEXT_INSTALLING_TITLE "$(UNINSTALLING_TITLE)"
!define MUI_TEXT_INSTALLING_SUBTITLE "$(UNINSTALLING_SUBTITLE)"
!define MUI_TEXT_FINISH_TITLE "$(UNINSTALL_FINISH_TITLE)"
!define MUI_TEXT_FINISH_SUBTITLE "$(UNINSTALL_FINISH_SUBTITLE)"
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "SimpChinese"

LangString UNCONFIRM_SUBTITLE ${LANG_ENGLISH} "Remove WinCraft for all users on this computer."
LangString RUNTIMEDATA_PAGE_TITLE ${LANG_ENGLISH} "Keep Data"
LangString RUNTIMEDATA_PAGE_SUBTITLE ${LANG_ENGLISH} "Choose runtime data to keep."
LangString RUNTIMEDATA_HEADER ${LANG_ENGLISH} "Keep these items after uninstall:"
LangString RUNTIMEDATA_CONFIG ${LANG_ENGLISH} "Configuration"
LangString RUNTIMEDATA_LOGS_DUMPS ${LANG_ENGLISH} "Logs and crash dumps"
LangString UNINSTALL_BUTTON ${LANG_ENGLISH} "Uninstall"
LangString APP_RUNNING_WARNING ${LANG_ENGLISH} "WinCraft is currently running.$\nPlease close WinCraft before uninstalling."
LangString UNINSTALLING_TITLE ${LANG_ENGLISH} "Uninstalling"
LangString UNINSTALLING_SUBTITLE ${LANG_ENGLISH} "Please wait while WinCraft is being removed."
LangString UNINSTALL_FINISH_TITLE ${LANG_ENGLISH} "Uninstallation Complete"
LangString UNINSTALL_FINISH_SUBTITLE ${LANG_ENGLISH} "WinCraft was successfully removed from all users."

LangString UNCONFIRM_SUBTITLE ${LANG_SIMPCHINESE} "从这台计算机的所有用户中移除 WinCraft。"
LangString RUNTIMEDATA_PAGE_TITLE ${LANG_SIMPCHINESE} "保留数据"
LangString RUNTIMEDATA_PAGE_SUBTITLE ${LANG_SIMPCHINESE} "选择卸载后要保留的运行时数据。"
LangString RUNTIMEDATA_HEADER ${LANG_SIMPCHINESE} "卸载后保留以下项目："
LangString RUNTIMEDATA_CONFIG ${LANG_SIMPCHINESE} "配置"
LangString RUNTIMEDATA_LOGS_DUMPS ${LANG_SIMPCHINESE} "日志和崩溃 Dump"
LangString UNINSTALL_BUTTON ${LANG_SIMPCHINESE} "卸载"
LangString APP_RUNNING_WARNING ${LANG_SIMPCHINESE} "WinCraft 正在运行。$\n请先关闭 WinCraft 再继续。"
LangString UNINSTALLING_TITLE ${LANG_SIMPCHINESE} "正在卸载"
LangString UNINSTALLING_SUBTITLE ${LANG_SIMPCHINESE} "请稍候，正在移除 WinCraft。"
LangString UNINSTALL_FINISH_TITLE ${LANG_SIMPCHINESE} "卸载完成"
LangString UNINSTALL_FINISH_SUBTITLE ${LANG_SIMPCHINESE} "WinCraft 已从所有用户中成功移除。"

BrandingText "WinCraft"

VIProductVersion "${Version}.0"
VIAddVersionKey "CompanyName"      "YeahOSS"
VIAddVersionKey "FileDescription"  "WinCraft All-Users Uninstaller"
VIAddVersionKey "LegalCopyright"   "Copyright ${U+00a9} YeahOSS 2026"
VIAddVersionKey "ProductName"      "WinCraft"
VIAddVersionKey "ProductVersion"   "${Version}"
VIAddVersionKey "FileVersion"      "${Version}"

Var FromTemp
Var UpgradeMode
Var KeepConfig
Var KeepLogs
Var KeepDumps
Var KeepConfigCheckbox
Var KeepLogsDumpsCheckbox

Function .onInit
  StrCpy $FromTemp "0"
  StrCpy $UpgradeMode "0"
  StrCpy $KeepConfig "1"
  StrCpy $KeepLogs "0"
  StrCpy $KeepDumps "0"

  ${GetParameters} $R0
  ClearErrors
  ${GetOptions} $R0 "/fromtemp" $1
  ${IfNot} ${Errors}
    StrCpy $FromTemp "1"
  ${EndIf}

  ClearErrors
  ${GetOptions} $R0 "/upgrade" $1
  ${IfNot} ${Errors}
    StrCpy $UpgradeMode "1"
  ${EndIf}

  ; Resolve install directory: registry → /D= parameter → EXE location
  ReadRegStr $0 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
  ${If} $0 != ""
    StrCpy $INSTDIR "$0"
  ${ElseIf} $INSTDIR == "$PROGRAMFILES\WinCraft"
    StrCpy $INSTDIR "$EXEDIR"
  ${EndIf}

  ; Upgrade mode: run directly from the install directory (no copy-to-temp).
  ${If} $UpgradeMode == "1"
    Return
  ${EndIf}

  ${If} $FromTemp != "1"
    CopyFiles /SILENT "$EXEPATH" "$TEMP\WinCraft-AllUsers-Uninstall.exe"
    ${If} ${Silent}
      Exec '"$TEMP\WinCraft-AllUsers-Uninstall.exe" /fromtemp /S /D=$INSTDIR'
    ${Else}
      Exec '"$TEMP\WinCraft-AllUsers-Uninstall.exe" /fromtemp /D=$INSTDIR'
    ${EndIf}
    SetErrorLevel 0
    Quit
  ${EndIf}

  ; Brief delay in silent mode so the original process can exit before
  ; we try to remove its EXE from the install directory.
  ${If} ${Silent}
    Sleep 1000
  ${EndIf}

  IfFileExists "$INSTDIR\WinCraft.exe" 0 +4
  System::Call 'kernel32::CreateFile(t "$INSTDIR\WinCraft.exe", i 0x40000000, i 0, i 0, i 3, i 0, i 0) i .r0 ?e'
  ${If} $0 == -1
    MessageBox MB_ICONSTOP|MB_OK "$(APP_RUNNING_WARNING)"
    Abort
  ${EndIf}
  System::Call 'kernel32::CloseHandle(i $0)'
FunctionEnd

!insertmacro WINCRAFT_UNINSTALL_RUNTIME_DATA_PAGE ""
!insertmacro WINCRAFT_UNINSTALL_DELETE_FUNCTIONS ""

Section "Remove"
  SetShellVarContext all

  Delete "$DESKTOP\WinCraft.lnk"
  Delete "$SMPROGRAMS\WinCraft.lnk"

  SetOutPath "$TEMP"
  File "/oname=wincraft-manifest.txt" "${SourceDir}\merged-manifest.txt"
  Call DeleteInstalledFiles
  Call DeleteRuntimeData
  RMDir "$INSTDIR"

  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft"
SectionEnd
