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

!ifndef LicensePath
  !error "LicensePath define is required."
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
!define MUI_BGCOLOR "FBFDFF"

!ifdef BannerBitmapPath
  !define MUI_HEADERIMAGE
  !define MUI_HEADERIMAGE_RIGHT
  !define MUI_HEADERIMAGE_BITMAP "${BannerBitmapPath}"
  !define MUI_HEADERIMAGE_UNBITMAP "${BannerBitmapPath}"
!endif

!ifdef DialogBitmapPath
  !define MUI_WELCOMEFINISHPAGE_BITMAP "${DialogBitmapPath}"
!endif

; ---------------------------------------------------------------------------
; Variables
; ---------------------------------------------------------------------------
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
Var CreateDesktopShortcut
Var CreateStartMenuShortcut
Var PreviousInstallPage
Var UpgradeRadio
Var UninstallRadio
Var FoundInstallDir
Var UpgradeMode
Var WelcomeAutoAdvanced
Var LicenseAutoAdvanced
Var PreviousInstallAutoAdvanced
Var InstallerMutex

; ---------------------------------------------------------------------------
; Auto-advance callbacks for elevation restart
; ---------------------------------------------------------------------------
Function WelcomeSkipShow
  ${If} $RelaunchedElevated == "1"
  ${AndIf} $WelcomeAutoAdvanced == "0"
    StrCpy $WelcomeAutoAdvanced "1"
    SendMessage $HWNDPARENT 0x408 1 0
  ${EndIf}
FunctionEnd

Function LicenseSkipShow
  ${If} $RelaunchedElevated == "1"
  ${AndIf} $LicenseAutoAdvanced == "0"
    StrCpy $LicenseAutoAdvanced "1"
    SendMessage $HWNDPARENT 0x408 1 0
  ${EndIf}
FunctionEnd

; ---------------------------------------------------------------------------
; Page definitions
; ---------------------------------------------------------------------------
!define MUI_PAGE_CUSTOMFUNCTION_SHOW WelcomeSkipShow
!define MUI_WELCOMEPAGE_TEXT  "$(WELCOME_TEXT)"
!insertmacro MUI_PAGE_WELCOME

!define MUI_PAGE_CUSTOMFUNCTION_SHOW LicenseSkipShow
!insertmacro MUI_PAGE_LICENSE "${LicensePath}"

Page custom PreviousInstallPageCreate PreviousInstallPageLeave
Page custom InstallModePageCreate InstallModePageLeave
!insertmacro MUI_PAGE_DIRECTORY
Page custom ShortcutOptionsPageCreate ShortcutOptionsPageLeave

!define MUI_PAGE_CUSTOMFUNCTION_PRE InstFilesPre
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_TITLE "$(FINISH_TITLE)"
!define MUI_FINISHPAGE_TEXT  "$(FINISH_TEXT)"
!define MUI_FINISHPAGE_RUN "WinCraft.exe"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\README.md"
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "SimpChinese"

!include "strings.nsh"

BrandingText "WinCraft"

VIProductVersion "${Version}.0"
VIAddVersionKey "CompanyName"      "YeahOSS"
VIAddVersionKey "FileDescription"  "WinCraft Installer"
VIAddVersionKey "LegalCopyright"   "Copyright ${U+00a9} YeahOSS 2026"
VIAddVersionKey "ProductName"      "WinCraft"
VIAddVersionKey "ProductVersion"   "${Version}"
VIAddVersionKey "FileVersion"      "${Version}"

; ---------------------------------------------------------------------------
; Previous Install page
; ---------------------------------------------------------------------------
Function PreviousInstallPageCreate
  ${If} $FoundInstallDir == ""
    ; After elevation restart with no prior install, create a placeholder
    ; page so Back navigation from InstallMode works correctly.
    ${If} $RelaunchedElevated == "1"
      ${If} $PreviousInstallAutoAdvanced == "0"
        ; First forward pass — auto-advance.
        StrCpy $PreviousInstallAutoAdvanced "1"
        nsDialogs::Create 1018
        Pop $PreviousInstallPage
        System::Call 'user32::PostMessage(p $HWNDPARENT, i 0x408, i 1, i 0)'
        nsDialogs::Show
      ${Else}
        ; Back navigation — page never shown; skip cleanly.
        Abort
      ${EndIf}
      Return
    ${EndIf}
    Abort
  ${EndIf}

  ; After elevation restart with a previous install, auto-advance past
  ; this page just like the placeholder path does.  The full UI is still
  ; created so the page stays in the wizard stack for Back navigation.
  ${If} $RelaunchedElevated == "1"
  ${AndIf} $PreviousInstallAutoAdvanced == "0"
    StrCpy $PreviousInstallAutoAdvanced "1"
    !insertmacro MUI_HEADER_TEXT "$(PREVIOUSINSTALL_PAGE_TITLE)" "$(PREVIOUSINSTALL_PAGE_SUBTITLE)"
    nsDialogs::Create 1018
    Pop $PreviousInstallPage
    ${If} $PreviousInstallPage == error
      Abort
    ${EndIf}
    ${NSD_CreateRadioButton} 20u 10u 260u 12u "$(PREVIOUSINSTALL_UPGRADE)"
    Pop $UpgradeRadio
    ${NSD_OnClick} $UpgradeRadio PreviousInstallOptionChanged
    ${NSD_CreateRadioButton} 20u 30u 260u 12u "$(PREVIOUSINSTALL_UNINSTALL)"
    Pop $UninstallRadio
    ${NSD_OnClick} $UninstallRadio PreviousInstallOptionChanged
    ${NSD_Check} $UpgradeRadio
    SendMessage $HWNDPARENT 0x408 1 0
    nsDialogs::Show
    Return
  ${EndIf}

  !insertmacro MUI_HEADER_TEXT "$(PREVIOUSINSTALL_PAGE_TITLE)" "$(PREVIOUSINSTALL_PAGE_SUBTITLE)"

  nsDialogs::Create 1018
  Pop $PreviousInstallPage
  ${If} $PreviousInstallPage == error
    Abort
  ${EndIf}

  ${NSD_CreateRadioButton} 20u 10u 260u 12u "$(PREVIOUSINSTALL_UPGRADE)"
  Pop $UpgradeRadio
  ${NSD_OnClick} $UpgradeRadio PreviousInstallOptionChanged

  ${NSD_CreateRadioButton} 20u 30u 260u 12u "$(PREVIOUSINSTALL_UNINSTALL)"
  Pop $UninstallRadio
  ${NSD_OnClick} $UninstallRadio PreviousInstallOptionChanged

  ${NSD_Check} $UpgradeRadio

  nsDialogs::Show
FunctionEnd

Function PreviousInstallOptionChanged
  Pop $0
  ${If} $0 == $UpgradeRadio
    StrCpy $UpgradeMode "Upgrade"
  ${Else}
    StrCpy $UpgradeMode "Uninstall"
  ${EndIf}
FunctionEnd

Function PreviousInstallPageLeave
  ${If} $UpgradeMode != "Uninstall"
    Return
  ${EndIf}

  ; Run the uninstaller and quit — use UninstallString so the full
  ; command line (including the per-user / all-users flag) is respected.
  ReadRegStr $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "UninstallString"
  ${If} $0 != ""
    ExecWait '$0' $R2
    ${If} $R2 != 0
      MessageBox MB_ICONSTOP|MB_OK "$(UNINSTALL_FAILED)"
      Abort
    ${EndIf}
    Quit
  ${EndIf}
  ReadRegStr $0 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "UninstallString"
  ${If} $0 != ""
    ExecWait '$0' $R2
    ${If} $R2 != 0
      MessageBox MB_ICONSTOP|MB_OK "$(UNINSTALL_FAILED)"
      Abort
    ${EndIf}
    Quit
  ${EndIf}
FunctionEnd

; ---------------------------------------------------------------------------
; Elevation
; ---------------------------------------------------------------------------
Function EnsureElevated
  ${If} $InstallMode == "AllUsers"
  ${AndIf} $RelaunchedElevated != "1"
  ${AndIf} $IsAdmin != "1"
    ClearErrors
    ${If} ${Silent}
      SetErrorLevel 740
    ${Else}
      ; Release single-instance mutex so the elevated instance can acquire it.
      ${If} $InstallerMutex != 0
        System::Call 'kernel32::ReleaseMutex(i $InstallerMutex)'
        System::Call 'kernel32::CloseHandle(i $InstallerMutex)'
        StrCpy $InstallerMutex 0
      ${EndIf}
      ExecShell "runas" "$EXEPATH" '/allusers /elevated /D="$INSTDIR"'
      ${If} ${Errors}
        MessageBox MB_ICONSTOP "$(INSTALLMODE_ELEVATION_FAILED)"
        Abort
      ${EndIf}
    ${EndIf}
    Quit
  ${EndIf}
FunctionEnd

; ---------------------------------------------------------------------------
; .onInit
; ---------------------------------------------------------------------------
Function .onInit
  ; Single-instance check using a named mutex.
  ; Uses the canonical NSIS pattern: ?e captures GetLastError() inside the
  ; same System::Call invocation, avoiding any race with LogicLib expansion.
  ; CreateMutexW is explicit — Unicode NSIS passes t as wchar_t*.
  System::Call 'kernel32::CreateMutexW(i 0, i 0, t "WinCraft-Setup-Mutex") i.r1 ?e'
  Pop $R0
  ${If} $1 != 0
    ${If} $R0 != 0  ; ERROR_ALREADY_EXISTS (183) — another instance is running
      System::Call 'kernel32::CloseHandle(i $1)'
      ; Find and activate the existing installer window.
      StrLen $R0 "$(^Name)"
      IntOp $R0 $R0 + 1
      FindWindow $R1 '#32770' '' 0 $R1
      ${DoWhile} $R1 != 0
        System::Call 'user32::GetWindowTextW(i $R1, t .r2, i $R0) i.'
        ${If} $2 == "$(^Name)"
          System::Call 'user32::ShowWindow(i $R1, i 9) i.'   ; SW_RESTORE
          System::Call 'user32::SetForegroundWindow(i $R1) i.'
          Abort
        ${EndIf}
        FindWindow $R1 '#32770' '' 0 $R1
      ${Loop}
      ; Window not found — fall back to a message.
      MessageBox MB_ICONSTOP|MB_OK "$(INSTALLER_ALREADY_RUNNING)"
      Abort
    ${EndIf}
    StrCpy $InstallerMutex $1
  ${EndIf}

  StrCpy $InstallMode "CurrentUser"
  StrCpy $RelaunchedElevated "0"
  StrCpy $HasCustomInstallDir "0"
  StrCpy $InstallModePageVisited "0"
  StrCpy $WelcomeAutoAdvanced "0"
  StrCpy $LicenseAutoAdvanced "0"
  StrCpy $PreviousInstallAutoAdvanced "0"
  StrCpy $UpgradeMode "Upgrade"
  StrCpy $CreateDesktopShortcut ${BST_CHECKED}
  StrCpy $CreateStartMenuShortcut ${BST_CHECKED}

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

  ; Store any previous install location for the upgrade/overwrite page.
  ReadRegStr $FoundInstallDir HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
  ${If} $FoundInstallDir == ""
    ReadRegStr $FoundInstallDir HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
  ${EndIf}

  ; Default install mode to match the previous installation scope.
  ; Skip on elevation restart — the user already explicitly chose AllUsers
  ; before the restart, and /allusers was passed on the command line.
  ${If} $FoundInstallDir != ""
  ${AndIf} $RelaunchedElevated == "0"
    ; Determine previous install scope from registry hive.
    ClearErrors
    ReadRegStr $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
    ${IfNot} ${Errors}
    ${AndIf} $0 != ""
      StrCpy $InstallMode "CurrentUser"
    ${Else}
      ClearErrors
      ReadRegStr $0 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation"
      ${IfNot} ${Errors}
      ${AndIf} $0 != ""
        StrCpy $InstallMode "AllUsers"
      ${EndIf}
    ${EndIf}
    Call ApplyInstallMode
  ${EndIf}

  ; Default install directory to the previous location unless overridden by /D=.
  ${If} $FoundInstallDir != ""
  ${AndIf} $HasCustomInstallDir == "0"
    StrCpy $INSTDIR $FoundInstallDir
  ${EndIf}
FunctionEnd

; ---------------------------------------------------------------------------
; Silently remove any previous installation before copying new files.
; Called from the Install section, so cancellation before this point
; preserves the old installation.
; ---------------------------------------------------------------------------
Function UninstallPreviousVersion
  ReadRegStr $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "UninstallString"
  ${If} $0 != ""
    DetailPrint "Removing previous installation..."
    ExecWait '$0 /upgrade /S' $R2
  ${EndIf}

  ReadRegStr $0 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "UninstallString"
  ${If} $0 != ""
    ${If} $RelaunchedElevated == "1"
    ${OrIf} $IsAdmin == "1"
      DetailPrint "Removing previous installation..."
      ExecWait '$0 /upgrade /S' $R2
    ${EndIf}
  ${EndIf}
FunctionEnd

; ---------------------------------------------------------------------------
; Pre-flight checks
; ---------------------------------------------------------------------------
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

; ---------------------------------------------------------------------------
; Install-mode helpers
; ---------------------------------------------------------------------------
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

; ---------------------------------------------------------------------------
; Install Mode page
; ---------------------------------------------------------------------------
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

; ---------------------------------------------------------------------------
; Install directory — ensure path ends with "WinCraft"
; ---------------------------------------------------------------------------
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

; ---------------------------------------------------------------------------
; Install
; ---------------------------------------------------------------------------
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

  ${If} $CreateDesktopShortcut == ${BST_CHECKED}
    CreateShortCut "$DESKTOP\WinCraft.lnk" "$INSTDIR\WinCraft.exe" "" "" "" "" "" "$(SHORTCUTOPTIONS_DESCRIPTION)"
  ${EndIf}
  ${If} $CreateStartMenuShortcut == ${BST_CHECKED}
    CreateShortCut "$SMPROGRAMS\WinCraft.lnk" "$INSTDIR\WinCraft.exe" "" "" "" "" "" "$(SHORTCUTOPTIONS_DESCRIPTION)"
  ${EndIf}

SectionEnd

; ---------------------------------------------------------------------------
; Shortcut Options page
; ---------------------------------------------------------------------------
Function ShortcutOptionsPageCreate
  !insertmacro MUI_HEADER_TEXT "$(SHORTCUTOPTIONS_PAGE_TITLE)" "$(SHORTCUTOPTIONS_PAGE_SUBTITLE)"

  nsDialogs::Create 1018
  Pop $0
  ${If} $0 == error
    Abort
  ${EndIf}

  ${NSD_CreateCheckbox} 20u 28u 260u 12u "$(SHORTCUTOPTIONS_DESKTOP)"
  Pop $DesktopShortcutCheckbox
  ${NSD_CreateCheckbox} 20u 48u 260u 12u "$(SHORTCUTOPTIONS_STARTMENU)"
  Pop $StartMenuShortcutCheckbox

  ${NSD_Check} $DesktopShortcutCheckbox
  ${NSD_Check} $StartMenuShortcutCheckbox

  nsDialogs::Show
FunctionEnd

Function ShortcutOptionsPageLeave
  ${NSD_GetState} $DesktopShortcutCheckbox $CreateDesktopShortcut
  ${NSD_GetState} $StartMenuShortcutCheckbox $CreateStartMenuShortcut
FunctionEnd
