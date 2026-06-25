Unicode True
CRCCheck on
SetCompressor /SOLID lzma
SetCompressorDictSize 32

!ifndef SourceDir
  !error "SourceDir define is required."
!endif

!ifndef OutFile
  !error "OutFile define is required."
!endif

!ifndef Version
  !define Version "0.0.0"
!endif

!ifndef IconPath
  !error "IconPath define is required."
!endif

!ifndef AllUsersUninstallerPath
  !error "AllUsersUninstallerPath define is required."
!endif

!ifndef CurrentUserUninstallerPath
  !error "CurrentUserUninstallerPath define is required."
!endif

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"
!include "nsDialogs.nsh"
!include "x64.nsh"

!define DOTNET45_RELEASE 378389

Name "WinCraft"
OutFile "${OutFile}"
InstallDir "$LOCALAPPDATA\WinCraft"
RequestExecutionLevel user

!define MUI_ABORTWARNING
!define MUI_ICON "${IconPath}"
!define MUI_UNICON "${IconPath}"

Page custom InstallModePageCreate InstallModePageLeave
!insertmacro MUI_PAGE_DIRECTORY
!define MUI_PAGE_CUSTOMFUNCTION_PRE InstFilesPre
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "WinCraft.exe"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\README.md"
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_PAGE_CUSTOMFUNCTION_SHOW FinishPageShow
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE FinishPageLeave
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "SimpChinese"

BrandingText "WinCraft"

VIProductVersion "${Version}.0"
VIAddVersionKey "CompanyName"      "YeahOSS"
VIAddVersionKey "FileDescription"  "WinCraft Installer"
VIAddVersionKey "LegalCopyright"   "Copyright ${U+00a9} YeahOSS 2026"
VIAddVersionKey "ProductName"      "WinCraft"
VIAddVersionKey "ProductVersion"   "${Version}"
VIAddVersionKey "FileVersion"      "${Version}"

Var InstallMode
Var InstallModePage
Var CurrentUserRadio
Var AllUsersRadio
Var RelaunchedElevated
Var HasCustomInstallDir
Var IsAdmin
Var InstallModePageVisited
Var DesktopShortcutCheckbox
Var StartMenuShortcutCheckbox

LangString INSTALLMODE_PAGE_TITLE ${LANG_ENGLISH} "Choose Users"
LangString INSTALLMODE_PAGE_SUBTITLE ${LANG_ENGLISH} "Choose who can use WinCraft."
LangString INSTALLMODE_HEADER ${LANG_ENGLISH} "Install WinCraft for:"
LangString INSTALLMODE_CURRENT_USER ${LANG_ENGLISH} "Current user only"
LangString INSTALLMODE_ALL_USERS ${LANG_ENGLISH} "All users"
LangString INSTALLMODE_ALL_USERS_NOTE ${LANG_ENGLISH} "Administrator credentials are required for all-users installation."
LangString INSTALLMODE_ELEVATION_FAILED ${LANG_ENGLISH} "Administrator elevation was cancelled or failed."
LangString FINISH_DESKTOP_SHORTCUT ${LANG_ENGLISH} "Create &desktop shortcut"
LangString FINISH_STARTMENU_SHORTCUT ${LANG_ENGLISH} "Create &Start Menu shortcut"
LangString SHORTCUT_DESCRIPTION ${LANG_ENGLISH} "WinCraft ✨ Craft Windows your way"
LangString APP_RUNNING_WARNING ${LANG_ENGLISH} "WinCraft is currently running.$\nPlease close WinCraft before continuing."
LangString ALLUSERS_CONFLICT_WARNING ${LANG_ENGLISH} "An all-users installation of WinCraft already exists on this computer.$\nPlease run this installer as administrator to upgrade it."

LangString INSTALLMODE_PAGE_TITLE ${LANG_SIMPCHINESE} "选择用户"
LangString INSTALLMODE_PAGE_SUBTITLE ${LANG_SIMPCHINESE} "选择可以使用 WinCraft 的用户。"
LangString INSTALLMODE_HEADER ${LANG_SIMPCHINESE} "为以下用户安装 WinCraft："
LangString INSTALLMODE_CURRENT_USER ${LANG_SIMPCHINESE} "仅当前用户"
LangString INSTALLMODE_ALL_USERS ${LANG_SIMPCHINESE} "所有用户"
LangString INSTALLMODE_ALL_USERS_NOTE ${LANG_SIMPCHINESE} "为所有用户安装需要管理员凭据。"
LangString INSTALLMODE_ELEVATION_FAILED ${LANG_SIMPCHINESE} "管理员提权已取消或失败。"
LangString FINISH_DESKTOP_SHORTCUT ${LANG_SIMPCHINESE} "创建桌面快捷方式"
LangString FINISH_STARTMENU_SHORTCUT ${LANG_SIMPCHINESE} "创建开始菜单快捷方式"
LangString SHORTCUT_DESCRIPTION ${LANG_SIMPCHINESE} "WinCraft ✨ 雕琢 Windows，如你所愿"
LangString APP_RUNNING_WARNING ${LANG_SIMPCHINESE} "WinCraft 正在运行。$\n请先关闭 WinCraft 再继续。"
LangString ALLUSERS_CONFLICT_WARNING ${LANG_SIMPCHINESE} "此计算机上已存在为所有用户安装的 WinCraft。$\n请以管理员身份运行此安装程序以进行升级。"

; Relaunch the installer as administrator when the user selects an
; all-users installation.  Skipped if already elevated or started as admin.
Function EnsureElevated
  ${If} $InstallMode == "AllUsers"
  ${AndIf} $RelaunchedElevated != "1"
  ${AndIf} $IsAdmin != "1"
    ClearErrors
    ${If} ${Silent}
      SetErrorLevel 740
    ${Else}
      ExecShell "runas" "$EXEPATH" "/allusers /elevated /D=$INSTDIR"
      ${If} ${Errors}
        MessageBox MB_ICONSTOP "$(INSTALLMODE_ELEVATION_FAILED)"
        Abort
      ${EndIf}
    ${EndIf}
    Quit
  ${EndIf}
FunctionEnd

Function .onInit
  StrCpy $InstallMode "CurrentUser"
  StrCpy $RelaunchedElevated "0"
  StrCpy $HasCustomInstallDir "0"
  StrCpy $InstallModePageVisited "0"

  ; Detect whether the installer is already running elevated (right-click
  ; Run as administrator) and default to an all-users install without a
  ; second elevation relaunch.  The user can still switch back on the
  ; install-mode page.
  UserInfo::GetAccountType
  Pop $0
  ${If} $0 == "Admin"
    StrCpy $IsAdmin "1"
    StrCpy $InstallMode "AllUsers"
  ${EndIf}

  ${If} $INSTDIR != "$LOCALAPPDATA\WinCraft"
    StrCpy $HasCustomInstallDir "1"
  ${EndIf}

  ${GetParameters} $0
  ClearErrors
  ${GetOptions} $0 "/allusers" $1
  ${IfNot} ${Errors}
    StrCpy $InstallMode "AllUsers"
  ${EndIf}

  ClearErrors
  ${GetOptions} $0 "/elevated" $1
  ${IfNot} ${Errors}
    StrCpy $RelaunchedElevated "1"
  ${EndIf}

  Call ApplyInstallMode

  ${If} ${Silent}
    Call EnsureElevated
  ${EndIf}

  Call CheckPreviousInstall
FunctionEnd

; Silently remove any previous installation before copying new files.
; Called from the Install section, so cancellation before this point
; preserves the old installation.
Function UninstallPreviousVersion
  ReadRegStr $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
  ${If} $0 != ""
    DetailPrint "Removing previous per-user installation..."
    ExecWait '"$0\Uninstall.exe" /S /currentuser /upgrade /D=$0' $R2
  ${EndIf}

  ; Remove any existing all-users installation.
  ReadRegStr $0 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
  ${If} $0 != ""
    ${If} $RelaunchedElevated == "1"
    ${OrIf} $IsAdmin == "1"
      DetailPrint "Removing previous all-users installation..."
      ExecWait '"$0\Uninstall.exe" /allusers /S /upgrade /D=$0' $R2
    ${EndIf}
  ${EndIf}
FunctionEnd

Function CheckPreviousInstall
  ; Warn if the application is running before we begin.
  ReadRegStr $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
  ${If} $0 != ""
    Call CheckAppRunningAt
  ${EndIf}
  ReadRegStr $0 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
  ${If} $0 != ""
    ${If} $RelaunchedElevated == "1"
    ${OrIf} $IsAdmin == "1"
      Call CheckAppRunningAt
    ${ElseIf} $InstallMode == "CurrentUser"
      MessageBox MB_ICONSTOP|MB_OK "$(ALLUSERS_CONFLICT_WARNING)"
      Abort
    ${EndIf}
  ${EndIf}
FunctionEnd

; Check whether a running instance of the application would block this
; installer.  Uses CreateFile with exclusive-write to detect a locked EXE.
Function CheckAppRunningAt
  IfFileExists "$0\WinCraft.exe" 0 done
  System::Call 'kernel32::CreateFile(t "$0\WinCraft.exe", i 0x40000000, i 0, i 0, i 3, i 0, i 0) i .r1 ?e'
  ${If} $1 == -1
    MessageBox MB_ICONSTOP|MB_OK "$(APP_RUNNING_WARNING)"
    Abort
  ${EndIf}
  System::Call 'kernel32::CloseHandle(i $1)'
  done:
FunctionEnd

Function ApplyInstallMode
  ${If} $HasCustomInstallDir == "1"
    ${If} $InstallMode == "AllUsers"
      SetShellVarContext all
    ${Else}
      SetShellVarContext current
    ${EndIf}

    Return
  ${EndIf}

  ${If} $InstallMode == "AllUsers"
    SetShellVarContext all
    ${If} ${RunningX64}
      StrCpy $INSTDIR "$PROGRAMFILES64\WinCraft"
    ${Else}
      StrCpy $INSTDIR "$PROGRAMFILES32\WinCraft"
    ${EndIf}
  ${Else}
    SetShellVarContext current
    StrCpy $INSTDIR "$LOCALAPPDATA\WinCraft"
  ${EndIf}
FunctionEnd

Function InstallModePageCreate
  !insertmacro MUI_HEADER_TEXT "$(INSTALLMODE_PAGE_TITLE)" "$(INSTALLMODE_PAGE_SUBTITLE)"

  nsDialogs::Create 1018
  Pop $InstallModePage
  ${If} $InstallModePage == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel} 0 0 100% 18u "$(INSTALLMODE_HEADER)"
  Pop $0

  ${NSD_CreateRadioButton} 20u 28u 260u 12u "$(INSTALLMODE_CURRENT_USER)"
  Pop $CurrentUserRadio
  ${NSD_OnClick} $CurrentUserRadio InstallModeOptionChanged

  ${NSD_CreateRadioButton} 20u 48u 260u 12u "$(INSTALLMODE_ALL_USERS)"
  Pop $AllUsersRadio
  ${NSD_OnClick} $AllUsersRadio InstallModeOptionChanged

  ${NSD_CreateLabel} 40u 64u 260u 24u "$(INSTALLMODE_ALL_USERS_NOTE)"
  Pop $0

  ${If} $InstallMode == "AllUsers"
    ${NSD_Check} $AllUsersRadio
  ${Else}
    ${NSD_Check} $CurrentUserRadio
  ${EndIf}

  ; After an elevation restart the user already chose All Users — auto-advance
  ; on the first visit.  If they go Back later the page stays visible so they
  ; can still change their mind.
  ${If} $RelaunchedElevated == "1"
  ${AndIf} $InstallModePageVisited == "0"
    StrCpy $InstallModePageVisited "1"
    SendMessage $HWNDPARENT 0x408 1 0
  ${EndIf}

  nsDialogs::Show
FunctionEnd

Function InstallModeOptionChanged
  Pop $0
  ${If} $0 == $AllUsersRadio
    StrCpy $InstallMode "AllUsers"
  ${Else}
    StrCpy $InstallMode "CurrentUser"
  ${EndIf}

  Call ApplyInstallMode
FunctionEnd

Function InstallModePageLeave
  Call ApplyInstallMode
  Call EnsureElevated
FunctionEnd

; Ensure the install directory ends with "WinCraft".  Walks backwards
; through $INSTDIR to extract the last path component for comparison.
Function InstFilesPre
  StrLen $0 $INSTDIR
  findBackslash:
    IntOp $0 $0 - 1
    ${If} $0 < 0
      StrCpy $1 $INSTDIR
      Goto done
    ${EndIf}
    StrCpy $1 $INSTDIR 1 $0
    ${If} $1 != "\"
      Goto findBackslash
    ${EndIf}
    IntOp $0 $0 + 1
    StrCpy $1 $INSTDIR "" $0
  done:
  ${If} $1 != "WinCraft"
    StrCpy $INSTDIR "$INSTDIR\WinCraft"
  ${EndIf}
FunctionEnd

Section "Install"
  Call UninstallPreviousVersion
  SetOutPath "$INSTDIR"

  !ifdef HasCommon
    File /r "${SourceDir}\Common\*.*"
  !endif

  ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
  ${If} $0 >= ${DOTNET45_RELEASE}
    File /r "${SourceDir}\Standard\*.*"
  ${Else}
    File /r "${SourceDir}\Legacy\*.*"
  ${EndIf}

  WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "DisplayName" "WinCraft"
  WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "DisplayVersion" "${Version}"
  WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "Publisher" "YeahOSS"
  WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation" "$INSTDIR"
  WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "DisplayIcon" "$INSTDIR\WinCraft.exe,0"
  ${If} $InstallMode == "AllUsers"
    File /oname=Uninstall.exe "${AllUsersUninstallerPath}"
    WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "UninstallString" '"$INSTDIR\Uninstall.exe" /allusers'
    WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /allusers /S'
  ${Else}
    File /oname=Uninstall.exe "${CurrentUserUninstallerPath}"
    WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "UninstallString" '"$INSTDIR\Uninstall.exe" /currentuser'
    WriteRegStr SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /currentuser /S'
  ${EndIf}
  WriteRegDWORD SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "NoModify" 1
  WriteRegDWORD SHCTX "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "NoRepair" 1

SectionEnd

Function FinishPageShow
  ${NSD_CreateCheckbox} 120u 140u 260u 12u "$(FINISH_DESKTOP_SHORTCUT)"
  Pop $DesktopShortcutCheckbox
  SetCtlColors $DesktopShortcutCheckbox "" 0xFFFFFF   ; match MUI2 white background
  ${NSD_CreateCheckbox} 120u 160u 260u 12u "$(FINISH_STARTMENU_SHORTCUT)"
  Pop $StartMenuShortcutCheckbox
  SetCtlColors $StartMenuShortcutCheckbox "" 0xFFFFFF
  SendMessage $DesktopShortcutCheckbox ${BM_SETCHECK} ${BST_CHECKED} 0
  SendMessage $StartMenuShortcutCheckbox ${BM_SETCHECK} ${BST_CHECKED} 0
FunctionEnd

Function FinishPageLeave
  ${NSD_GetState} $DesktopShortcutCheckbox $0
  ${If} $0 == ${BST_CHECKED}
    CreateShortCut "$DESKTOP\WinCraft.lnk" "$INSTDIR\WinCraft.exe" "" "" "" "" "" "$(SHORTCUT_DESCRIPTION)"
  ${EndIf}

  ${NSD_GetState} $StartMenuShortcutCheckbox $0
  ${If} $0 == ${BST_CHECKED}
    CreateShortCut "$SMPROGRAMS\WinCraft.lnk" "$INSTDIR\WinCraft.exe" "" "" "" "" "" "$(SHORTCUT_DESCRIPTION)"
  ${EndIf}
FunctionEnd
