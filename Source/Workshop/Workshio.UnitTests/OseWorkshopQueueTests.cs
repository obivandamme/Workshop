using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Workshio.UnitTests
{
    using Workshop;

    [TestClass]
    public class OseWorkshopQueueTests
    {
        [TestMethod]
        public void Pop_TwoElements_ReturnsTheElementFirstAdded()
        {
            var queue = new WorkshopQueue();
            queue.Add(new WorkshopItem(new AvailablePart { cost = 1000 }));
            queue.Add(new WorkshopItem(new AvailablePart { cost = 1 }));

            var first = queue.Pop();
            Assert.AreEqual(1000, first.Part.cost);
            Assert.AreEqual(1, queue.Count);

            var second = queue.Pop();
            Assert.AreEqual(1, second.Part.cost);
            Assert.AreEqual(0, queue.Count);

            var invalid = queue.Pop();
            Assert.IsNull(invalid);
        }
    }
}
