using System.Threading;
using System.Windows.Controls;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class DesignWatermarkTests
    {
        [Test]
        public void Watermark_TextBox_TracksTextAndDetachesWhenCleared()
        {
            var textBox = new TextBox();

            TextInput.SetWatermark(textBox, "Name");
            Assert.That(TextInput.GetHasText(textBox), Is.False);

            textBox.Text = "WinCraft";
            Assert.That(TextInput.GetHasText(textBox), Is.True);

            textBox.Text = string.Empty;
            Assert.That(TextInput.GetHasText(textBox), Is.False);

            TextInput.SetWatermark(textBox, null);
            textBox.Text = "Detached";
            Assert.That(TextInput.GetHasText(textBox), Is.False);
        }

        [Test]
        public void Watermark_PasswordBox_TracksPassword()
        {
            var passwordBox = new PasswordBox();

            TextInput.SetWatermark(passwordBox, "Password");
            Assert.That(TextInput.GetHasText(passwordBox), Is.False);

            passwordBox.Password = "secret";
            Assert.That(TextInput.GetHasText(passwordBox), Is.True);

            passwordBox.Password = string.Empty;
            Assert.That(TextInput.GetHasText(passwordBox), Is.False);
        }

        [Test]
        public void Watermark_EditableComboBox_TracksTypedTextAndSelection()
        {
            var comboBox = new ComboBox { IsEditable = true };
            comboBox.Items.Add(new ComboBoxItem { Content = "Theme" });

            TextInput.SetWatermark(comboBox, "Choose a theme");
            Assert.That(TextInput.GetHasText(comboBox), Is.False);

            comboBox.Text = "Dark";
            Assert.That(TextInput.GetHasText(comboBox), Is.True);

            comboBox.Text = string.Empty;
            Assert.That(TextInput.GetHasText(comboBox), Is.False);

            comboBox.SelectedIndex = 0;
            Assert.That(TextInput.GetHasText(comboBox), Is.True);
        }
    }
}
