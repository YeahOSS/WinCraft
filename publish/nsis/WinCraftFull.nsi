Unicode True

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

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "WinVer.nsh"

Name "WinCraft ${Version}"
OutFile "${OutFile}"
InstallDir "$LOCALAPPDATA\WinCraft"
InstallDirRegKey HKCU "Software\WinCraft" "InstallDir"
RequestExecutionLevel user

!define MUI_ABORTWARNING
!define MUI_ICON "${IconPath}"
!define MUI_UNICON "${IconPath}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "Install"
  SetOutPath "$INSTDIR"

  ${If} ${AtLeastWin8}
    File /r "${SourceDir}\Standard\*.*"
    WriteRegStr HKLM "Software\WinCraft" "BuildLine" "Standard"
  ${Else}
    File /r "${SourceDir}\Legacy\*.*"
    WriteRegStr HKCU "Software\WinCraft" "BuildLine" "Legacy"
  ${EndIf}

  WriteRegStr HKCU "Software\WinCraft" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "DisplayName" "WinCraft"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "DisplayVersion" "${Version}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "Publisher" "WinCraft"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft" "NoRepair" 1
  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Uninstall"
  Delete "$INSTDIR\Uninstall.exe"
  Delete "$INSTDIR\WinCraft.exe"
  Delete "$INSTDIR\WinCraft.exe.config"
  Delete "$INSTDIR\WinCraft.Core.dll"
  Delete "$INSTDIR\System.Runtime.CompilerServices.Unsafe.dll"
  Delete "$INSTDIR\System.Threading.Tasks.Extensions.dll"
  Delete "$INSTDIR\System.ValueTuple.dll"
  Delete "$INSTDIR\Theraot.Core.dll"
  RMDir "$INSTDIR"

  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft"
  DeleteRegKey HKCU "Software\WinCraft"
SectionEnd
