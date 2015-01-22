using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Workshio.UnitTests
{
    using System.Collections.Generic;

    using Workshop;

    [TestClass]
    public class ListExtensionsTests
    {
        [TestMethod]
        public void Next_FirstElement_ReturnsSecondElement()
        {
            var first = new AvailablePart();
            var second = new AvailablePart();
            var list = new List<AvailablePart> { first, second };

            var actual = list.NextOf(first);
            Assert.AreSame(second, actual);
        }

        [TestMethod]
        public void Next_LastElement_ReturnsFirstElement()
        {
            var first = new AvailablePart();
            var second = new AvailablePart();
            var list = new List<AvailablePart> { first, second };

            var actual = list.NextOf(second);
            Assert.AreSame(first, actual);
        }
    }
}
