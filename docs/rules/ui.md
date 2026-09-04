# UI

Scope: `src/WinCraft/UI/`.  See `source-layout.md` for file placement.

## Architecture

| Target | Required | Avoid |
|---|---|---|
| View | Bind state and commands to a ViewModel; limit code-behind to WPF wiring | State, commands, and business logic in code-behind |
| ViewModel | `ObservableObject` with `RelayCommand` / `AsyncRelayCommand` | Ad-hoc `ICommand`, new base classes, view-only `Visibility` state |
| Converter | Reuse the matching `WinCraft.UI` converter inline in XAML | Code-behind conversion or a ViewModel property for presentation-only conversion |
| Control/style | Reuse shared controls, styles, and tokens | Product-specific controls or styles in shared UI folders |

- Declare one XAML alias for UI types (`xmlns:ui="clr-namespace:WinCraft.UI"`).
- Prefer existing `UIHelper` and visual-tree helpers over duplicate dispatcher or traversal code.

## ViewModel

### ObservableObject

ViewModel properties are dictionary-backed (not auto-properties), resolved via `[CallerMemberName]`:

```csharp
public string Name
{
    get => GetValue<string>();
    set => SetValue(value);
}
```

| Member | Signature | Description |
|------|------|------|
| `GetValue<T>` | `GetValue<T>([CallerMemberName] string name = null)` | Reads from dictionary; returns `default(T)` when unset |
| `SetValue<T>` | `bool SetValue<T>(T value, [CallerMemberName] string name = null)` | Writes to dictionary; returns `true` and raises `PropertyChanged` when the value changes |
| `RaisePropertyChanged` | `void RaisePropertyChanged([CallerMemberName] string name = null)` | Manually raises property-changed notification |

- Chained notification: `if (SetValue(value)) RaisePropertyChanged(nameof(OtherProperty));`
- Thread-safe: dictionary operations protected by a lock.

### RelayCommand

| Type | Purpose |
|------|------|
| `RelayCommand` | Parameterless command; `ExecuteAction` is replaceable at runtime |
| `RelayCommand<T>` | Accepts a typed parameter `T`; silently skipped when the parameter cannot be converted |

`CanExecuteFunc` is a read/write property — setting it automatically calls `CommandManager.InvalidateRequerySuggested()`. `CanExecute` refreshes via `CommandManager.RequerySuggested`.

```csharp
public ICommand SaveCommand { get; }
    = new RelayCommand(() => Save(), () => IsDirty);
```

### AsyncRelayCommand

| Type | Purpose |
|------|------|
| `AsyncRelayCommand` | Async parameterless command |
| `AsyncRelayCommand<T>` | Async typed command |

All re-entrant calls are silently ignored while `IsExecuting` is `true` — the `finally` block guarantees `IsExecuting` is always reset. When no `canExecute` is provided, only `IsExecuting` gates executability.

```csharp
public ICommand LoadDataCommand { get; }
    = new AsyncRelayCommand(async () => await LoadDataAsync());
```

## Converters

All converters inherit from `MarkupExtension` for inline XAML usage without resource declarations.

### Naming Conventions

`{Condition}{To}{Output}Cvt` — behaviour is inferred from the type name:

| Pattern | Example | Meaning |
|------|------|------|
| `{Condition}Cvt` | `IsTrueCvt` | Returns `true` when condition holds |
| `{Condition}To{Output}Cvt` | `TrueToCollapsedCvt` | Returns the named output when condition holds |
| `{Condition}ToBoolCvt` | `StringEqualsToBoolCvt` | Returns `true` when condition holds |

### Base Classes

| Base Class | Purpose |
|------|------|
| `ValueConverterBase` | Single-value conversion; `ConvertBack` defaults to `Binding.DoNothing`; also a `MarkupExtension` |
| `MultiValueConverterBase` | Multi-value conversion |
| `StateConverterBase` | State-driven conversion |
| `MultiStateConverterBase` | Multi-value state conversion |

### Categories

| Directory | Files | Contents |
|------|--------|------|
| `Converters/Bool/` | 11 | `IsTrueCvt`, `IsFalseCvt`, `IsNullCvt`, `IsNullOrEmptyCvt`, `IsNullOrWhiteSpaceCvt`, `IsAllTrueCvt`, `IsAllFalseCvt`, `IsAnyTrueCvt`, `IsAnyFalseCvt`, `InvertBoolCvt`, `StringEqualsToBoolCvt` |
| `Converters/Visibility/` | 30 | `{True,False,Null,NullOrEmpty,NullOrWhiteSpace,Object,Objects,AllTrue,AllFalse,AnyTrue,AnyFalse}{To}{Collapsed,Hidden,Visible}Cvt` |
| `Converters/` (root) | 2 | `ConditionalCvt` (ternary if/then/else), `StringFormatCvt` |

### Usage

```xaml
<Button IsEnabled="{Binding IsDirty, Converter={ui:IsTrueCvt}}" />
<TextBlock Visibility="{Binding Count, Converter={ui:TrueToCollapsedCvt}}" />
```

## Controls

Spacing-series controls carry their own `Padding`, `BorderBrush`, `BorderThickness`, and `CornerRadius` properties,
rendering a superellipse background — do **not** wrap with an extra `SuperellipseBorder` or `Border` just for rounded corners, backgrounds, or borders.

### Window

| Class | Description |
|-----|------|
| `ChromeWindow` | Custom title-bar window |
| `SystemWindow` | Native title-bar window (DWM color enhancement) |
| `TabWindow` | Browser-style tabbed window |
| `TabShoulderShape` | Shoulder-curve shape for tab-window tabs |
| `TitleBar` / `TitleBarButton` / `TitleBarPanel` | Title-bar components |

### Layout

`SpacingStackPanel`, `SpacingGrid`, `SpacingDockPanel`, `SpacingWrapPanel`, `SpacingUniformGrid`,
`SpacingItemsControl`, `Form` / `FormItem`,
`VirtualizingSpacingStackPanel`, `VirtualizingSpacingWrapPanel`, `VirtualizingUniformGrid`

`SpacingGrid` supports string syntax for row/column definitions: `Columns="Auto,*,100"`, `Rows="Auto,*"`.

### Input

`NumericBox`, `ColorPicker`

### Buttons

`IconButton`, `IconToggleButton`, `Switch`

### Feedback

`MessageBar`, `MessageTip`, `LoadingIndicator`

### Primitives

`IconBlock`, `SuperellipseBorder`

### Adorners

`FocusAdorner`, `TextInputCaretAdorner`

## Tokens and Layout

- Reuse existing tokens for brushes, dimensions, padding, margins, spacing, corner radii, and offsets.
- Use `{DynamicResource}` for theme-sensitive values; do not hard-code colours in views or styles.
- Use `{StaticResource}` for immutable tokens from `Tokens.xaml`.
- Add a generic shared token when a repeated layout value has no fit; keep literals only for control-specific functional geometry.
- Prefer `SpacingStackPanel`, `SpacingGrid`, and `SpacingDockPanel` for spacing, padding, backgrounds, and borders.
- Prefer `SuperellipseBorder` for rounded surfaces; use `Border` only when an enhanced layout control cannot provide the behavior.
- Keep layout shallow: do not add a wrapper solely for spacing, background, border, or rounded corners when an enhanced layout control already supplies it.

## Design and Style

### Attached Property Providers

| Provider | Properties | Description |
|--------|------|------|
| `ui:Design` | `Size`, `VisualRole`, `Variant`, `Icon`, `CornerRadius`, `IsIconFilled` | Control size / role / variant / icon / corner radius |
| `ui:LayoutTokens` | `Spacing` | Spacing token (None → XLarge), mapped to `Tokens.xaml` |
| `ui:TextInput` | `Watermark`, `HasText` (read-only), `UseThemedCaret`, `IsClearButtonEnabled` | Text input adornments |
| `ui:PasswordReveal` | `IsEnabled`, `IsRevealed` | PasswordBox plain-text toggle |
| `ui:FocusVisual` | `OutlineShape`, `NormalizedOutlineGeometry` | Keyboard focus indicator |
| `ui:SeparatorLayout` | `Orientation` | MenuItem / ToolBar separator layout |
| `ui:MenuLayout` | `IsIconOnly` | Collapse top-level Menu items to centered icons only |
| `ui:TabChrome` | `ShowUnselectedTabSeparators`, `IsSeparatorVisible` (read-only) | Tab appearance |
| `ContextMenuTracker` | `TrackContextMenu`, `IsContextMenuOpenWithin` (read-only) | Keep hover / focus visuals while a menu is open |
| `KeyboardFocus` | `IsFocusVisible` (read-only) | Keyboard focus visibility |
| `ChromeWindow` | `HitTestRole`, `IsNonClientHovered`, `IsNonClientPressed` | Non-client hit testing |

- When adding or changing a shared attached property, audit every applicable base-control style and add the required triggers; do not leave a property effective for only some equivalent controls.
- Put control-level shared attached-property triggers in the narrowest compatible keyed base `Style`; keep template-part triggers in their owning `ControlTemplate`.
- Size, role, spacing, and corner-radius choices must remain consistent across equivalent controls and light/dark themes.
- Prefer neutral, non-glaring light and dark colours; verify text, selected, disabled, hover, and focus states together.

## Theme

`ThemeService` (singleton) handles dynamic switching between `Brushes.Light.xaml` / `Brushes.Dark.xaml`.
`BackdropThemeScope` applies the matching backdrop palette below an active window template.

| Component | Description |
|------|------|
| `ThemeMode` | `Light`, `Dark`, `Auto` — `Auto` follows the system setting (reads the `AppsUseLightTheme` registry value) |
| `ThemeService.Instance.SetMode(mode)` | Switches the theme at runtime |
| `ThemeService.Instance.EffectiveTheme` | The currently active theme |
| `Brushes.Light.xaml` / `Brushes.Dark.xaml` | Light / dark brush dictionaries — key sets must be identical |
| `Brushes.*.Backdrop.xaml` | Semi-transparent backdrop variants (replaces opaque brushes when Mica/Acrylic is active) |

Brush naming conventions:

| Prefix | Purpose |
|------|------|
| `Bg*` | Surface / background |
| `Text*` | Text foreground |
| `Border*` | Border / separator |
| `Control*` | Interactive control states (hover, pressed, disabled) |
| `Primary*` / `Info*` / `Success*` / `Warning*` / `Error*` | Semantic colours |

## Icons

- Use `IconGlyph` member names directly in XAML and style triggers; do not use redundant `x:Static` lookups.
- Use inheritable `ui:Design.IsIconFilled="True"` for filled icons.
- `IconBlock` handles font fallback and Regular→Filled mapping automatically; do not set `FontFamily` manually.
- `IconGlyphMap.RegularToFilled` maintains a Regular→Filled mapping; reference only the Regular name when adding icons.

## Resource Loading

```
App.xaml
  └─ Controls.xaml
       ├─ Controls.Common.xaml
       ├─ Controls.Buttons.xaml
       ├─ Controls.Input.xaml
       ├─ Controls.ColorPicker.xaml
       ├─ Controls.Selection.xaml
       ├─ Controls.Navigation.xaml
       ├─ Controls.Menus.xaml
       ├─ Controls.Feedback.xaml
       ├─ Controls.Form.xaml
       ├─ Controls.Scrolling.xaml
       ├─ Controls.Window.xaml
       └─ Controls.Extended.xaml

ThemeService (runtime)
  └─ Brushes.{Light|Dark}.xaml  ← dynamic swap

BackdropThemeScope (active window template)
  └─ Brushes.{Light|Dark}.Backdrop.xaml
```

Immutable tokens in `Tokens.xaml` (font sizes, spacing values, thicknesses, corner radii) are referenced
via `{StaticResource}` and do not go through `ThemeService`.
