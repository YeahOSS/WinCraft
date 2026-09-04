using System.Threading;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class FormItemTests
    {
        [Test]
        public void Form_LabelWidth_InheritsToChildFormItem()
        {
            var form = new Form { LabelWidth = 120 };
            var item = new FormItem();
            form.Items.Add(item);

            var inheritedWidth = Form.GetLabelWidth(item);
            Assert.That(inheritedWidth, Is.EqualTo(120));
        }

        [Test]
        public void FormItem_CanOverrideInheritedLabelWidth()
        {
            var form = new Form { LabelWidth = 120 };
            var item = new FormItem();
            Form.SetLabelWidth(item, 200);
            form.Items.Add(item);

            Assert.That(Form.GetLabelWidth(item), Is.EqualTo(200));
        }

        [Test]
        public void Form_Spacing_UsesDesignToken()
        {
            var form = new Form();
            form.Resources["SpacingLarge"] = 20.0;

            LayoutTokens.SetSpacing(form, Spacing.Large);

            Assert.That(form.Spacing, Is.EqualTo(20));

            LayoutTokens.SetSpacing(form, Spacing.None);

            Assert.That(form.Spacing, Is.EqualTo(0));
        }
    }
}
