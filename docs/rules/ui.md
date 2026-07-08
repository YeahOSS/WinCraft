# UI

Scope: `src/WinCraft/UI/`.

## Use Rules

| Target | Required use | Avoid |
|---|---|---|
| View | Bind to ViewModel; code-behind only for WPF wiring | State, commands, or business logic in code-behind |
| ViewModel | `ObservableObject`, `RelayCommand`, `AsyncRelayCommand` | Ad-hoc `ICommand`, new base classes, `Visibility` properties for pure view state |
| Converter | Existing converter types from `WinCraft.UI` in XAML | Inline conversion logic or duplicate view-only state |
| Control/style | `UI/Controls/` for base controls; `UI/Styles/` for styles/colors | Business-specific controls or styles in shared UI folders |

All C# files under `UI/` use namespace `WinCraft.UI`; declare one XAML alias for UI types.

## ViewModel API

- Property: `GetValue<T>()` / `SetValue(value)`.
- Dependent notification: `if (SetValue(value)) RaisePropertyChanged(nameof(OtherProperty));`.
- Async command: `AsyncRelayCommand`; `IsExecuting` blocks re-entry.
- Typed command: `RelayCommand<T>` / `AsyncRelayCommand<T>`.

## Converter Use

- Namespace is always `WinCraft.UI`; subdirectories are grouping only.
- Use existing converters before adding ViewModel-only state or custom XAML conversion.
- Find converters by `{Condition}{To}{Output}Cvt`; when none fits, add one converter type in a matching file.
