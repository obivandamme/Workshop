using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Workshio.UnitTests
{
    using System.Collections.Generic;
    using System.Management.Instrumentation;

    using Workshop;

    [TestClass]
    public class ListExtensionsTests
    {
        private AvailablePart _first;

        private AvailablePart _second;

        private List<AvailablePart> _list;
        
        [TestInitialize]
        public void Setup()
        {
            _list = new List<AvailablePart> { _first, _second };
        }

        [TestMethod]
        public void Next_FirstElement_ReturnsSecondElement()
        {
            var actual = _list.NextOf(_first);
            Assert.AreSame(_second, actual);
        }

        [TestMethod]
        public void Next_LastElement_ReturnsFirstElement()
        {
            var actual = _list.NextOf(_second);
            Assert.AreSame(_first, actual);
        }

        [TestMethod]
        public void Previous_SecondElement_ReturnsFirstElement()
        {
            var actual = _list.PreviousOf(_second);
            Assert.AreSame(_first, actual);
        }

        [TestMethod]
        public void Previous_FirstElement_ReturnsLastElement()
        {
            var actual = _list.PreviousOf(_first);
            Assert.AreSame(_second, actual);
        }
    }
}
