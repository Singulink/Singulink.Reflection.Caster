using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Singulink.Reflection;

namespace Singulink.CoreLib.Tests
{
    [TestClass]
    public class CasterTests
    {
        [TestMethod]
        public void ByteToInt()
        {
            Assert.AreEqual(Caster.Cast<byte, int>(200), 200);
            Assert.AreEqual(Caster.CheckedCast<byte, int>(200), 200);
        }

        [TestMethod]
        public void DynamicByteToInt()
        {
            Assert.AreEqual(Caster.DynamicCast((byte)200, typeof(int)), 200);
            Assert.AreEqual(Caster.DynamicCheckedCast((byte)200, typeof(int)), 200);
        }

        [TestMethod]
        public void IntToByte()
        {
            Assert.AreEqual(Caster.Cast<int, byte>(200), 200);
            Assert.AreEqual(Caster.CheckedCast<int, byte>(200), 200);
        }

        [TestMethod]
        public void DynamicIntToByte()
        {
            Assert.AreEqual(Caster.DynamicCast(200, typeof(byte)), (byte)200);
            Assert.AreEqual(Caster.DynamicCheckedCast(200, typeof(byte)), (byte)200);
        }

        [TestMethod]
        public void IntToByteWithOverflow()
        {
            Assert.AreEqual(Caster.Cast<int, byte>(2000), unchecked((byte)2000));
            Assert.ThrowsException<OverflowException>(() => Caster.CheckedCast<int, byte>(2000));
        }

        [TestMethod]
        public void DynamicIntToByteWithOverflow()
        {
            Assert.AreEqual(Caster.DynamicCast(2000, typeof(byte)), unchecked((byte)2000));
            Assert.ThrowsException<OverflowException>(() => Caster.DynamicCheckedCast(2000, typeof(byte)));
        }
    }
}
