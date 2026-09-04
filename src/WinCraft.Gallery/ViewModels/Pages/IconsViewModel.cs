using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using WinCraft.Compatibility;
using WinCraft.Infrastructure;
using WinCraft.UI;

namespace WinCraft.Gallery.ViewModels.Pages;

public sealed class IconsViewModel : ObservableObject
{
    private readonly DispatcherTimer _searchTimer;

    public IconsViewModel()
    {
        Glyphs = CollectionViewSource.GetDefaultView(new ObservableCollection<IconModel>());
        Glyphs.Filter = FilterGlyph;
        SizeOptions = [new(null)];
        SelectedSizeOption = SizeOptions[0];
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _searchTimer.Tick += (_, __) =>
        {
            _searchTimer.Stop();
            RefreshFilter();
        };

        CopyNameCommand = new RelayCommand<IconModel>(CopyName);

        var dispatcher = Dispatcher.CurrentDispatcher;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            var icons = new List<IconModel>();
            var sizes = new HashSet<int?>();

            foreach (IconGlyph value in Enum.GetValues(typeof(IconGlyph)))
            {
                if (value == IconGlyph.None)
                    continue;

                var name = value.ToString();
                var codePoint = (int)value;
                var size = ParseSize(name);

                icons.Add(new IconModel(name, codePoint, size, value));
                if (size.HasValue)
                    sizes.Add(size.Value);
            }

            icons.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                Glyphs = CollectionViewSource.GetDefaultView(new ObservableCollection<IconModel>(icons));
                Glyphs.Filter = FilterGlyph;

                var selectedSize = SelectedSizeOption?.Size;
                SizeOptions.Clear();
                SizeOptions.Add(new GlyphSizeFilterOption(null));
                foreach (var size in sizes.OrderBy(s => s))
                    SizeOptions.Add(new GlyphSizeFilterOption(size));

                SelectedSizeOption = SizeOptions.FirstOrDefault(o => o.Size == selectedSize) ?? SizeOptions[0];
                RefreshFilter();
            }));
        });
    }

    public RelayCommand<IconModel> CopyNameCommand { get; }

    public ICollectionView Glyphs
    {
        get => GetValue<ICollectionView>();
        private set => SetValue(value);
    }

    public ObservableCollection<GlyphSizeFilterOption> SizeOptions { get; }

    public GlyphSizeFilterOption SelectedSizeOption
    {
        get => GetValue<GlyphSizeFilterOption>();
        set
        {
            if (value == null && SizeOptions.Count > 0)
                value = SizeOptions[0];
            if (!SetValue(value))
                return;
            RefreshFilter();
        }
    }

    public string SearchText
    {
        get => GetValue<string>();
        set
        {
            if (!SetValue(value ?? string.Empty))
                return;
            _searchTimer.Stop();
            _searchTimer.Start();
        }
    }

    public bool IsFilled
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public string GlyphSummary => Glyphs == null
        ? string.Empty
        : string.Format(
            CultureInfo.CurrentCulture,
            "Showing {0:N0} / {1:N0} icons",
            Glyphs.Cast<IconModel>().Count(),
            GetTotalGlyphCount());

    private static int? ParseSize(string name)
    {
        var digits = 0;
        for (var i = name.Length - 1; i >= 0 && char.IsDigit(name[i]); i--)
            digits++;

        if (digits == 0)
            return null;

        var sizeStr = name.Substring(name.Length - digits);
        if (int.TryParse(sizeStr, NumberStyles.None, CultureInfo.InvariantCulture, out var size)
            && size >= 8 && size <= 256)
            return size;

        return null;
    }

    private bool FilterGlyph(object candidate)
    {
        if (candidate is not IconModel model)
            return false;

        var selectedSize = SelectedSizeOption?.Size;
        if (selectedSize.HasValue && model.Size != selectedSize.Value)
            return false;

        return StringCompat.IsNullOrWhiteSpace(SearchText)
            || model.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0
            || model.CodePointText.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RefreshFilter()
    {
        Glyphs?.Refresh();
        RaisePropertyChanged(nameof(GlyphSummary));
    }

    private int GetTotalGlyphCount() =>
        Glyphs?.SourceCollection is Collection<IconModel> c ? c.Count : 0;

    private static void CopyName(IconModel model)
    {
        if (model == null)
            return;

        if (ClipboardAccess.TrySetText(model.Name))
            MessageTip.Show("Copied to clipboard", VisualRole.Success);
        else
            MessageTip.Show("Clipboard unavailable", VisualRole.Error);
    }
}

public sealed class IconModel
{
    public IconModel(string name, int codePoint, int? size, IconGlyph glyph)
    {
        Name = name;
        CodePoint = codePoint;
        Size = size;
        Glyph = glyph;
        CodePointText = "U+" + codePoint.ToString("X4", CultureInfo.InvariantCulture);

        var displayName = name;
        if (size.HasValue)
        {
            var sizeStr = size.Value.ToString(CultureInfo.InvariantCulture);
            if (displayName.EndsWith(sizeStr, StringComparison.Ordinal))
                displayName = displayName.Substring(0, displayName.Length - sizeStr.Length);
        }

        DisplayName = string.IsNullOrEmpty(displayName) ? name : displayName;
    }

    public string Name { get; }

    public int CodePoint { get; }

    public int? Size { get; }

    public IconGlyph Glyph { get; }

    public string CodePointText { get; }

    public string DisplayName { get; }
}

public sealed class GlyphSizeFilterOption(int? size)
{
    public int? Size { get; } = size;

    public string DisplayText { get; } = size.HasValue ? size.Value.ToString(CultureInfo.InvariantCulture) : "All sizes";

    public override string ToString() => DisplayText;
}
