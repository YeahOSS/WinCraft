using System.Windows;
using System.Windows.Input;

namespace WinCraft.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.F12)
                    new TestBench.TestBenchWindow { Owner = this }.Show();
            };
#endif
        }
    }
}
