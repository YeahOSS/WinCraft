using System;
using System.Globalization;
using System.Windows.Data;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Converters
{
    [TestFixture]
    internal sealed class ValueConverterTests
    {
        [Test]
        public void InvertBoolCvt_ConvertBack_WithBool_ReturnsInverse()
        {
            var converter = new InvertBoolCvt();

            Assert.That(converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(false));
            Assert.That(converter.ConvertBack(false, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(true));
        }

        [Test]
        public void InvertBoolCvt_ConvertBack_WithNonBool_ReturnsDoNothing()
        {
            var converter = new InvertBoolCvt();

            Assert.That(converter.ConvertBack("true", typeof(bool), null, CultureInfo.InvariantCulture), Is.SameAs(Binding.DoNothing));
        }

        [Test]
        public void StringEqualsToBoolCvt_ConvertBack_WhenChecked_ReturnsParameter()
        {
            var converter = new StringEqualsToBoolCvt();
            var parameter = "compact";

            Assert.That(converter.ConvertBack(true, typeof(string), parameter, CultureInfo.InvariantCulture), Is.SameAs(parameter));
        }

        [Test]
        public void StringEqualsToBoolCvt_ConvertBack_WhenUnchecked_ReturnsDoNothing()
        {
            var converter = new StringEqualsToBoolCvt();

            Assert.That(converter.ConvertBack(false, typeof(string), "compact", CultureInfo.InvariantCulture), Is.SameAs(Binding.DoNothing));
        }
    }
}
