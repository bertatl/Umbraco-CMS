// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Infrastructure.PublishedCache;
using Umbraco.Cms.Core.Services;
using System.Reflection;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.PublishedCache.NuCache
{
public static class SnapDictionaryTestExtensions
{
    public class TestHelperWrapper
    {
        private readonly object _testHelper;

        public TestHelperWrapper(object testHelper)
        {
            _testHelper = testHelper;
        }

        public object[] GetValues<TKey>(TKey key)
        {
            var method = _testHelper.GetType().GetMethod("GetValues", BindingFlags.Public | BindingFlags.Instance);
            return (object[])method.Invoke(_testHelper, new object[] { key });
        }

        public int LiveGen => (int)_testHelper.GetType().GetProperty("LiveGen", BindingFlags.Public | BindingFlags.Instance).GetValue(_testHelper);

        public bool NextGen => (bool)_testHelper.GetType().GetProperty("NextGen", BindingFlags.Public | BindingFlags.Instance).GetValue(_testHelper);

        public int FloorGen => (int)_testHelper.GetType().GetProperty("FloorGen", BindingFlags.Public | BindingFlags.Instance).GetValue(_testHelper);

        public bool IsLocked => (bool)_testHelper.GetType().GetProperty("IsLocked", BindingFlags.Public | BindingFlags.Instance).GetValue(_testHelper);

        public object GenObj => _testHelper.GetType().GetProperty("GenObj", BindingFlags.Public | BindingFlags.Instance).GetValue(_testHelper);

        public object LiveSnapshot => _testHelper.GetType().GetProperty("LiveSnapshot", BindingFlags.Public | BindingFlags.Instance).GetValue(_testHelper);

        public bool CollectAuto
        {
            get => (bool)_testHelper.GetType().GetProperty("CollectAuto", BindingFlags.Public | BindingFlags.Instance).GetValue(_testHelper);
            set => _testHelper.GetType().GetProperty("CollectAuto", BindingFlags.Public | BindingFlags.Instance).SetValue(_testHelper, value);
        }
    }

    public static TestHelperWrapper GetTestHelper<TKey, TValue>(this SnapDictionary<TKey, TValue> dictionary)
        where TValue : class
    {
        var testProperty = typeof(SnapDictionary<TKey, TValue>).GetProperty("Test", BindingFlags.NonPublic | BindingFlags.Instance);
        return new TestHelperWrapper(testProperty.GetValue(dictionary));
    }

    public static int GetLiveGen<TKey, TValue>(this SnapDictionary<TKey, TValue> dictionary)
        where TValue : class
    {
        var helper = GetTestHelper(dictionary);
        var property = helper.GetType().GetProperty("LiveGen", BindingFlags.Public | BindingFlags.Instance);
        return (int)property.GetValue(helper);
    }

    public static bool GetNextGen<TKey, TValue>(this SnapDictionary<TKey, TValue> dictionary)
        where TValue : class
    {
        var helper = GetTestHelper(dictionary);
        var property = helper.GetType().GetProperty("NextGen", BindingFlags.Public | BindingFlags.Instance);
        return (bool)property.GetValue(helper);
    }

    public static int GetFloorGen<TKey, TValue>(this SnapDictionary<TKey, TValue> dictionary)
        where TValue : class
    {
        var helper = GetTestHelper(dictionary);
        var property = helper.GetType().GetProperty("FloorGen", BindingFlags.Public | BindingFlags.Instance);
        return (int)property.GetValue(helper);
    }
}

public static class SnapDictionaryTestHelperExtensions
{
public static void SetCollectAuto<TKey, TValue>(this SnapDictionary<TKey, TValue> dictionary, bool value)
    where TValue : class
{
    var testHelper = dictionary.GetTestHelper();
    testHelper.CollectAuto = value;
}
}

    [TestFixture]
    public class SnapDictionaryTests
    {
        [Test]
        public void LiveGenUpdate()
        {
            var d = new SnapDictionary<int, string>();
            var testHelper = d.GetTestHelper();
            d.SetCollectAuto(false);

            Assert.AreEqual(0, testHelper.GetValues(1).Length);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, testHelper.GetValues(1).Length);
            d.Clear(1);
            Assert.AreEqual(0, testHelper.GetValues(1).Length); // gone

            Assert.AreEqual(1, testHelper.LiveGen);
            Assert.IsTrue(testHelper.NextGen);
            Assert.AreEqual(0, testHelper.FloorGen);
        }

        [Test]
        public void OtherGenUpdate()
        {
            var d = new SnapDictionary<int, string>();
            var testHelper = d.GetTestHelper();
            testHelper.CollectAuto = false;

            Assert.AreEqual(0, testHelper.GetValues(1).Length);
            Assert.AreEqual(0, testHelper.LiveGen);
            Assert.IsFalse(testHelper.NextGen);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, testHelper.GetValues(1).Length);
            Assert.AreEqual(1, testHelper.LiveGen);
            Assert.IsTrue(testHelper.NextGen);

            SnapDictionary<int, string>.Snapshot s = d.CreateSnapshot();
            Assert.AreEqual(1, s.Gen);
            Assert.AreEqual(1, testHelper.LiveGen);
            Assert.IsFalse(testHelper.NextGen);

            // gen 2
            d.Clear(1);
            Assert.AreEqual(2, testHelper.GetValues(1).Length); // there
            Assert.AreEqual(2, testHelper.LiveGen);
            Assert.IsTrue(testHelper.NextGen);

            Assert.AreEqual(0, testHelper.FloorGen);

            GC.KeepAlive(s);
        }

        [Test]
        public void MissingReturnsNull()
        {
            var d = new SnapDictionary<int, string>();
            SnapDictionary<int, string>.Snapshot s = d.CreateSnapshot();

            Assert.IsNull(s.Get(1));
        }

        [Test]
        public void DeletedReturnsNull()
        {
            var d = new SnapDictionary<int, string>();

            // gen 1
            d.Set(1, "one");

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();
            Assert.AreEqual("one", s1.Get(1));

            // gen 2
            d.Clear(1);

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();
            Assert.IsNull(s2.Get(1));

            Assert.AreEqual("one", s1.Get(1));
        }

        [Retry(5)] // TODO make this test non-flaky.
        [Test]
        public async Task CollectValues()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "uno");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            // gen 2
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "one");
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "uno");
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            // gen 3
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "one");
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "uno");
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            object[] tv = d.GetTestHelper().GetValues(1);
            Assert.AreEqual(3, ((dynamic)tv[0]).Gen);
            Assert.AreEqual(2, ((dynamic)tv[1]).Gen);
            Assert.AreEqual(1, ((dynamic)tv[2]).Gen);

            Assert.AreEqual(0, d.GetTestHelper().FloorGen);

            // nothing to collect
            await d.CollectAsync();
            GC.KeepAlive(s1);
            GC.KeepAlive(s2);
            Assert.AreEqual(0, d.GetTestHelper().FloorGen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);
            Assert.AreEqual(2, d.SnapCount);
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            // one snapshot to collect
            s1 = null;
            GC.Collect();
            GC.KeepAlive(s2);
            await d.CollectAsync();
            Assert.AreEqual(1, d.GetTestHelper().FloorGen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);
            Assert.AreEqual(1, d.SnapCount);
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            // another snapshot to collect
            s2 = null;
            GC.Collect();
            await d.CollectAsync();
            Assert.AreEqual(2, d.GetTestHelper().FloorGen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.AreEqual(true, d.GetTestHelper().NextGen);
            Assert.AreEqual(0, d.SnapCount);
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
        }

        [Test]
        public async Task ProperlyCollects()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            for (int i = 0; i < 32; i++)
            {
                d.Set(i, i.ToString());
                d.CreateSnapshot().Dispose();
            }

            Assert.AreEqual(32, d.GenCount);
            Assert.AreEqual(0, d.SnapCount); // because we've disposed them

            await d.CollectAsync();
            Assert.AreEqual(32, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual(0, d.GenCount);
            Assert.AreEqual(0, d.SnapCount);
            Assert.AreEqual(32, d.Count);

            for (int i = 0; i < 32; i++)
            {
                d.Set(i, null);
            }

            d.CreateSnapshot().Dispose();

            // because we haven't collected yet, but disposed nevertheless
            Assert.AreEqual(1, d.GenCount);
            Assert.AreEqual(0, d.SnapCount);
            Assert.AreEqual(32, d.Count);

            // once we collect, they are all gone
            // since noone is interested anymore
            await d.CollectAsync();
            Assert.AreEqual(0, d.GenCount);
            Assert.AreEqual(0, d.SnapCount);
            Assert.AreEqual(0, d.Count);
        }

        [Retry(5)] // TODO make this test non-flaky.
        [Test]
        public async Task CollectNulls()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "uno");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            // gen 2
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "one");
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "uno");
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            // gen 3
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "one");
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "uno");
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);
            d.Clear(1);
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            object[] tv = d.GetTestHelper().GetValues(1);
            Assert.AreEqual(3, ((dynamic)tv[0]).Gen);
            Assert.AreEqual(2, ((dynamic)tv[1]).Gen);
            Assert.AreEqual(1, ((dynamic)tv[2]).Gen);

            Assert.AreEqual(0, d.GetTestHelper().FloorGen);

            // nothing to collect
            await d.CollectAsync();
            GC.KeepAlive(s1);
            GC.KeepAlive(s2);
            Assert.AreEqual(0, d.GetTestHelper().FloorGen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);
            Assert.AreEqual(2, d.SnapCount);
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            // one snapshot to collect
            s1 = null;
            GC.Collect();
            GC.KeepAlive(s2);
            await d.CollectAsync();
            Assert.AreEqual(1, d.GetTestHelper().FloorGen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);
            Assert.AreEqual(1, d.SnapCount);
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            // another snapshot to collect
            s2 = null;
            GC.Collect();
            await d.CollectAsync();
            Assert.AreEqual(2, d.GetTestHelper().FloorGen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);
            Assert.AreEqual(0, d.SnapCount);

            // and everything is gone?
            // no, cannot collect the live gen because we'd need to lock
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            d.CreateSnapshot();
            GC.Collect();
            await d.CollectAsync();

            // poof, gone
            Assert.AreEqual(0, d.GetTestHelper().GetValues(1).Length);
        }

        [Test]
        [Retry(5)] // TODO make this test non-flaky.
        public async Task EventuallyCollectNulls()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            Assert.AreEqual(0, d.GetTestHelper().GetValues(1).Length);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            await d.CollectAsync();
            object[] tv = d.GetTestHelper().GetValues(1);
            Assert.AreEqual(1, tv.Length);
            Assert.AreEqual(1, ((dynamic)tv[0]).Gen);

            SnapDictionary<int, string>.Snapshot s = d.CreateSnapshot();
            Assert.AreEqual("one", s.Get(1));

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            Assert.AreEqual(1, d.Count);
            Assert.AreEqual(1, d.SnapCount);
            Assert.AreEqual(1, d.GenCount);

            // gen 2
            d.Clear(1);
            tv = d.GetTestHelper().GetValues(1);
            Assert.AreEqual(2, tv.Length);
            Assert.AreEqual(2, ((dynamic)tv[0]).Gen);

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            Assert.AreEqual(1, d.Count);
            Assert.AreEqual(1, d.SnapCount);
            Assert.AreEqual(1, d.GenCount);

            // nothing to collect
            await d.CollectAsync();
            GC.KeepAlive(s);
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.Count);
            Assert.AreEqual(1, d.SnapCount);
            Assert.AreEqual(1, d.GenCount);

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            // collect snapshot
            // don't collect liveGen+
            s = null; // without being disposed
            GC.Collect(); // should release the generation reference
            await d.CollectAsync();

            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length); // "one" value is gone
            Assert.AreEqual(1, d.Count); // still have 1 item
            Assert.AreEqual(0, d.SnapCount); // snapshot is gone
            Assert.AreEqual(0, d.GenCount); // and generation has been dequeued

            // liveGen/nextGen
            s = d.CreateSnapshot();
            s = null;

            // collect liveGen
            GC.Collect();

            // Instead of directly accessing internal members, let's check the observable behavior
            SnapDictionary<int, string>.Snapshot snapshot = d.CreateSnapshot();
            Assert.IsNotNull(snapshot);

            // Force garbage collection
            snapshot = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Collect and verify that the snapshot has been removed
            await d.CollectAsync();

            // Check that a new snapshot has a different reference
            SnapDictionary<int, string>.Snapshot newSnapshot = d.CreateSnapshot();
            Assert.IsNotNull(newSnapshot);

            // Verify that the dictionary behaves as if the old snapshot was collected
            d.Set(1, "test");
            Assert.AreEqual("test", newSnapshot.Get(1));

            await d.CollectAsync();

            Assert.IsNull(newSnapshot.Get(1)); // value is gone
            Assert.AreEqual(0, d.Count); // item is gone
            Assert.AreEqual(0, d.SnapCount); // snapshot is gone
            Assert.AreEqual(0, d.GenCount); // and generation has been dequeued
        }

        [Test]
        public async Task CollectDisposedSnapshots()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            // gen 2
            d.Set(1, "two");
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            // gen 3
            d.Set(1, "three");
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s3 = d.CreateSnapshot();

            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            Assert.AreEqual(3, d.SnapCount);

            s1.Dispose();
            await d.CollectAsync();
            Assert.AreEqual(2, d.SnapCount);
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            s2.Dispose();
            await d.CollectAsync();
            Assert.AreEqual(1, d.SnapCount);
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            s3.Dispose();
            await d.CollectAsync();
            Assert.AreEqual(0, d.SnapCount);
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
        }

        [Retry(5)] // TODO make this test non-flaky.
        [Test]
        public async Task CollectGcSnapshots()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            // gen 2
            d.Set(1, "two");
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            // gen 3
            d.Set(1, "three");
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s3 = d.CreateSnapshot();

            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);

            Assert.AreEqual(3, d.SnapCount);

            s1 = s2 = s3 = null;

            await d.CollectAsync();
            Assert.AreEqual(3, d.SnapCount);
            Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            GC.Collect();
            await d.CollectAsync();
            Assert.AreEqual(0, d.SnapCount);
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
        }

        [Retry(5)] // TODO make this test non-flaky.
        [Test]
        public async Task RandomTest1()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            d.Set(1, "one");
            d.Set(2, "two");

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();
            string v1 = s1.Get(1);
            Assert.AreEqual("one", v1);

            d.Set(1, "uno");

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();
            string v2 = s2.Get(1);
            Assert.AreEqual("uno", v2);

            v1 = s1.Get(1);
            Assert.AreEqual("one", v1);

            Assert.AreEqual(2, d.SnapCount);

            s1 = null;
            GC.Collect();
            await d.CollectAsync();

            // in Release mode, it works, but in Debug mode, the weak reference is still alive
            // and for some reason we need to do this to ensure it is collected
#if DEBUG
            GC.Collect();
            await d.CollectAsync();
#endif

            Assert.AreEqual(1, d.SnapCount);
            v2 = s2.Get(1);
            Assert.AreEqual("uno", v2);

            s2 = null;
            GC.Collect();
            await d.CollectAsync();

            Assert.AreEqual(0, d.SnapCount);
        }

        [Retry(5)] // TODO make this test non-flaky.
        [Test]
        public async Task RandomTest2()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            d.Set(1, "one");
            d.Set(2, "two");

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();
            string v1 = s1.Get(1);
            Assert.AreEqual("one", v1);

            d.Clear(1);

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();
            string v2 = s2.Get(1);
            Assert.AreEqual(null, v2);

            v1 = s1.Get(1);
            Assert.AreEqual("one", v1);

            Assert.AreEqual(2, d.SnapCount);

            s1 = null;
            GC.Collect();
            await d.CollectAsync();

            // in Release mode, it works, but in Debug mode, the weak reference is still alive
            // and for some reason we need to do this to ensure it is collected
#if DEBUG
            GC.Collect();
            await d.CollectAsync();
#endif

            Assert.AreEqual(1, d.SnapCount);
            v2 = s2.Get(1);
            Assert.AreEqual(null, v2);

            s2 = null;
            GC.Collect();
            await d.CollectAsync();

            Assert.AreEqual(0, d.SnapCount);
        }

        [Test]
        public void WriteLockingFirstSnapshot()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.Test.NextGen);

            using (d.GetScopedWriteLock(GetScopeProvider()))
            {
                SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();

                Assert.AreEqual(0, s1.Gen);
                Assert.AreEqual(1, d.GetTestHelper().LiveGen);
                Assert.IsTrue(d.Test.NextGen);
                Assert.IsNull(s1.Get(1));
            }

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            Assert.AreEqual(1, s2.Gen);
            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual("one", s2.Get(1));
        }

        [Test]
        public void WriteLocking()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.Test.NextGen);

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();

            Assert.AreEqual(1, s1.Gen);
            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual("one", s1.Get(1));

            // gen 2
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "uno");
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.Test.NextGen);

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            Assert.AreEqual(2, s2.Gen);
            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual("uno", s2.Get(1));

            using (d.GetScopedWriteLock(GetScopeProvider()))
            {
                // gen 3
                Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);
                d.SetLocked(1, "ein");
                Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s3 = d.CreateSnapshot();

            Assert.AreEqual(2, s3.Gen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen); // has NOT changed when (non) creating snapshot
                Assert.AreEqual("uno", s3.Get(1));
            }

            SnapDictionary<int, string>.Snapshot s4 = d.CreateSnapshot();

            Assert.AreEqual(3, s4.Gen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual("ein", s4.Get(1));
        }

        [Test]
        public void NestedWriteLocking1()
        {
            var d = new SnapDictionary<int, string>();
            SnapDictionary<int, string>.TestHelper t = d.Test;
            t.CollectAuto = false;

            Assert.AreEqual(0, d.CreateSnapshot().Gen);

            // no scope context: writers nest, last one to be disposed commits
            IScopeProvider scopeProvider = GetScopeProvider();

            using (IDisposable w1 = d.GetScopedWriteLock(scopeProvider))
            {
                Assert.AreEqual(1, t.LiveGen);
                Assert.IsTrue(t.IsLocked);
                Assert.IsTrue(t.NextGen);

                Assert.Throws<InvalidOperationException>(() =>
                {
                    using (IDisposable w2 = d.GetScopedWriteLock(scopeProvider))
                    {
                    }
                });

                Assert.AreEqual(1, t.LiveGen);
                Assert.IsTrue(t.IsLocked);
                Assert.IsTrue(t.NextGen);

                Assert.AreEqual(0, d.CreateSnapshot().Gen);
            }

            Assert.AreEqual(1, t.LiveGen);
            Assert.IsFalse(t.IsLocked);
            Assert.IsTrue(t.NextGen);

            Assert.AreEqual(1, d.CreateSnapshot().Gen);
        }

        [Test]
        public void NestedWriteLocking2()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            Assert.AreEqual(0, d.CreateSnapshot().Gen);

            // scope context: writers enlist
            var scopeContext = Mock.Of<IScopeContext>();
            IScopeProvider scopeProvider = GetScopeProvider(scopeContext);

            using (IDisposable w1 = d.GetScopedWriteLock(scopeProvider))
            {
                // This one is interesting, although we don't allow recursive locks, since this is
                // using the same ScopeContext/key, the lock acquisition is only done once.
                using (IDisposable w2 = d.GetScopedWriteLock(scopeProvider))
                {
                    Assert.AreSame(w1, w2);

                    d.SetLocked(1, "one");
                }
            }
        }

        [Test]
        public void NestedWriteLocking3()
        {
            var d = new SnapDictionary<int, string>();
            SnapDictionary<int, string>.TestHelper t = d.Test;
            t.CollectAuto = false;

            Assert.AreEqual(0, d.CreateSnapshot().Gen);

            var scopeContext = Mock.Of<IScopeContext>();
            IScopeProvider scopeProvider1 = GetScopeProvider();
            IScopeProvider scopeProvider2 = GetScopeProvider(scopeContext);

            using (IDisposable w1 = d.GetScopedWriteLock(scopeProvider1))
            {
                Assert.AreEqual(1, t.LiveGen);
                Assert.IsTrue(t.IsLocked);
                Assert.IsTrue(t.NextGen);

                Assert.Throws<InvalidOperationException>(() =>
                {
                    using (IDisposable w2 = d.GetScopedWriteLock(scopeProvider2))
                    {
                    }
                });
            }
        }

        [Test]
        public void WriteLocking2()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.Test.NextGen);

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();

            Assert.AreEqual(1, s1.Gen);
            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual("one", s1.Get(1));

            // gen 2
            Assert.AreEqual(1, d.GetTestHelper().GetValues(1).Length);
            d.Set(1, "uno");
            Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.Test.NextGen);

            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            Assert.AreEqual(2, s2.Gen);
            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual("uno", s2.Get(1));

            IScopeProvider scopeProvider = GetScopeProvider();
            using (d.GetScopedWriteLock(scopeProvider))
            {
                // gen 3
                Assert.AreEqual(2, d.GetTestHelper().GetValues(1).Length);
                d.SetLocked(1, "ein");
                Assert.AreEqual(3, d.GetTestHelper().GetValues(1).Length);

            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen);

            SnapDictionary<int, string>.Snapshot s3 = d.CreateSnapshot();

            Assert.AreEqual(2, s3.Gen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.GetTestHelper().NextGen); // has NOT changed when (non) creating snapshot
                Assert.AreEqual("uno", s3.Get(1));
            }

            SnapDictionary<int, string>.Snapshot s4 = d.CreateSnapshot();

            Assert.AreEqual(3, s4.Gen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual("ein", s4.Get(1));
        }

        [Test]
        public void WriteLocking3()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();
            Assert.AreEqual(1, s1.Gen);
            Assert.AreEqual("one", s1.Get(1));

            d.Set(1, "uno");
            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();
            Assert.AreEqual(2, s2.Gen);
            Assert.AreEqual("uno", s2.Get(1));

            IScopeProvider scopeProvider = GetScopeProvider();
            using (d.GetScopedWriteLock(scopeProvider))
            {
                // creating a snapshot in a write-lock does NOT return the "current" content
                // it uses the previous snapshot, so new snapshot created only on release
                d.SetLocked(1, "ein");
                SnapDictionary<int, string>.Snapshot s3 = d.CreateSnapshot();
                Assert.AreEqual(2, s3.Gen);
                Assert.AreEqual("uno", s3.Get(1));

                // but live snapshot contains changes
            SnapDictionary<int, string>.Snapshot ls = d.GetTestHelper().LiveSnapshot;
                Assert.AreEqual("ein", ls.Get(1));
                Assert.AreEqual(3, ls.Gen);
            }

            SnapDictionary<int, string>.Snapshot s4 = d.CreateSnapshot();
            Assert.AreEqual(3, s4.Gen);
            Assert.AreEqual("ein", s4.Get(1));
        }

        [Test]
        public void ScopeLocking1()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            // gen 1
            d.Set(1, "one");
            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();
            Assert.AreEqual(1, s1.Gen);
            Assert.AreEqual("one", s1.Get(1));

            d.Set(1, "uno");
            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();
            Assert.AreEqual(2, s2.Gen);
            Assert.AreEqual("uno", s2.Get(1));

            var scopeContext = Mock.Of<IScopeContext>();
            IScopeProvider scopeProvider = GetScopeProvider(scopeContext);
            using (d.GetScopedWriteLock(scopeProvider))
            {
                // creating a snapshot in a write-lock does NOT return the "current" content
                // it uses the previous snapshot, so new snapshot created only on release
                d.SetLocked(1, "ein");
                SnapDictionary<int, string>.Snapshot s3 = d.CreateSnapshot();
                Assert.AreEqual(2, s3.Gen);
                Assert.AreEqual("uno", s3.Get(1));

                // but live snapshot contains changes
            SnapDictionary<int, string>.Snapshot ls = d.GetTestHelper().LiveSnapshot;
                Assert.AreEqual("ein", ls.Get(1));
                Assert.AreEqual(3, ls.Gen);
            }

            SnapDictionary<int, string>.Snapshot s4 = d.CreateSnapshot();
            Assert.AreEqual(2, s4.Gen);
            Assert.AreEqual("uno", s4.Get(1));

            scopeContext.ScopeExit(true);

            SnapDictionary<int, string>.Snapshot s5 = d.CreateSnapshot();
            Assert.AreEqual(3, s5.Gen);
            Assert.AreEqual("ein", s5.Get(1));
        }

        [Test]
        public void ScopeLocking2()
        {
            var d = new SnapDictionary<int, string>();
            SnapDictionary<int, string>.TestHelper t = d.Test;
            t.CollectAuto = false;

            // gen 1
            d.Set(1, "one");
            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();
            Assert.AreEqual(1, s1.Gen);
            Assert.AreEqual("one", s1.Get(1));

            d.Set(1, "uno");
            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();
            Assert.AreEqual(2, s2.Gen);
            Assert.AreEqual("uno", s2.Get(1));

            Assert.AreEqual(2, t.LiveGen);
            Assert.IsFalse(t.NextGen);

            var scopeContext = Mock.Of<IScopeContext>();
            IScopeProvider scopeProvider = GetScopeProvider(scopeContext);
            using (d.GetScopedWriteLock(scopeProvider))
            {
                // creating a snapshot in a write-lock does NOT return the "current" content
                // it uses the previous snapshot, so new snapshot created only on release
                d.SetLocked(1, "ein");
                SnapDictionary<int, string>.Snapshot s3 = d.CreateSnapshot();
                Assert.AreEqual(2, s3.Gen);
                Assert.AreEqual("uno", s3.Get(1));

                // we made some changes, so a next gen is required
                Assert.AreEqual(3, t.LiveGen);
                Assert.IsTrue(t.NextGen);
                Assert.IsTrue(t.IsLocked);

                // but live snapshot contains changes
                SnapDictionary<int, string>.Snapshot ls = t.LiveSnapshot;
                Assert.AreEqual("ein", ls.Get(1));
                Assert.AreEqual(3, ls.Gen);
            }

            // nothing is committed until scope exits
            Assert.AreEqual(3, t.LiveGen);
            Assert.IsTrue(t.NextGen);
            Assert.IsTrue(t.IsLocked);

            // no changes until exit
            SnapDictionary<int, string>.Snapshot s4 = d.CreateSnapshot();
            Assert.AreEqual(2, s4.Gen);
            Assert.AreEqual("uno", s4.Get(1));

            scopeContext.ScopeExit(false);

            // now things have changed
            Assert.AreEqual(2, t.LiveGen);
            Assert.IsFalse(t.NextGen);
            Assert.IsFalse(t.IsLocked);

            // no changes since not completed
            SnapDictionary<int, string>.Snapshot s5 = d.CreateSnapshot();
            Assert.AreEqual(2, s5.Gen);
            Assert.AreEqual("uno", s5.Get(1));
        }

        [Test]
        public void GetAll()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            Assert.AreEqual(0, d.Test.GetValues(1).Length);

            d.Set(1, "one");
            d.Set(2, "two");
            d.Set(3, "three");
            d.Set(4, "four");

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();
            string[] all = s1.GetAll().ToArray();
            Assert.AreEqual(4, all.Length);
            Assert.AreEqual("one", all[0]);
            Assert.AreEqual("four", all[3]);

            d.Set(1, "uno");
            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            all = s1.GetAll().ToArray();
            Assert.AreEqual(4, all.Length);
            Assert.AreEqual("one", all[0]);
            Assert.AreEqual("four", all[3]);

            all = s2.GetAll().ToArray();
            Assert.AreEqual(4, all.Length);
            Assert.AreEqual("uno", all[0]);
            Assert.AreEqual("four", all[3]);
        }

        [Test]
        public void DontPanic()
        {
        var d = new SnapDictionary<int, string>();
        d.SetCollectAuto(false);

            Assert.IsNull(d.GetTestHelper().GenObj);

            // gen 1
            d.Set(1, "one");
            Assert.IsTrue(d.Test.NextGen);
            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsNull(d.GetTestHelper().GenObj);

            SnapDictionary<int, string>.Snapshot s1 = d.CreateSnapshot();
            Assert.IsFalse(d.GetTestHelper().NextGen);
            Assert.AreEqual(1, d.GetTestHelper().LiveGen);
            Assert.IsNotNull(d.GetTestHelper().GenObj);
            Assert.AreEqual(1, d.GetTestHelper().GenObj.Gen);

            Assert.AreEqual(1, s1.Gen);
            Assert.AreEqual("one", s1.Get(1));

            d.Set(1, "uno");
            Assert.IsTrue(d.Test.NextGen);
            Assert.AreEqual(2, d.GetTestHelper().LiveGen);
            Assert.IsNotNull(d.GetTestHelper().GenObj);
            Assert.AreEqual(1, d.GetTestHelper().GenObj.Gen);

            var scopeContext = Mock.Of<IScopeContext>();
            IScopeProvider scopeProvider = GetScopeProvider(scopeContext);

            // scopeProvider.Context == scopeContext -> writer is scoped
            // writer is scope contextual and scoped
            //  when disposed, nothing happens
            //  when the context exists, the writer is released
            using (d.GetScopedWriteLock(scopeProvider))
            {
                d.SetLocked(1, "ein");
                Assert.IsTrue(d.Test.NextGen);
                Assert.AreEqual(3, d.GetTestHelper().LiveGen);
                Assert.IsNotNull(d.GetTestHelper().GenObj);
            Assert.AreEqual(2, d.GetTestHelper().GenObj.Gen);
            }

            // writer has not released
            Assert.IsTrue(d.GetTestHelper().IsLocked);
            Assert.IsNotNull(d.GetTestHelper().GenObj);
            Assert.AreEqual(2, d.GetTestHelper().GenObj.Gen);

            // nothing changed
            Assert.IsTrue(d.Test.NextGen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);

            // panic!
            SnapDictionary<int, string>.Snapshot s2 = d.CreateSnapshot();

            Assert.IsTrue(d.GetTestHelper().IsLocked);
            Assert.IsNotNull(d.GetTestHelper().GenObj);
            Assert.AreEqual(2, d.GetTestHelper().GenObj.Gen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.Test.NextGen);

            // release writer
            scopeContext.ScopeExit(true);

            Assert.IsFalse(d.GetTestHelper().IsLocked);
            Assert.IsNotNull(d.GetTestHelper().GenObj);
            Assert.AreEqual(2, d.GetTestHelper().GenObj.Gen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsTrue(d.Test.NextGen);

            SnapDictionary<int, string>.Snapshot s3 = d.CreateSnapshot();

            Assert.IsFalse(d.GetTestHelper().IsLocked);
            Assert.IsNotNull(d.GetTestHelper().GenObj);
            Assert.AreEqual(3, d.GetTestHelper().GenObj.Gen);
            Assert.AreEqual(3, d.GetTestHelper().LiveGen);
            Assert.IsFalse(d.GetTestHelper().NextGen);
        }

        private IScopeProvider GetScopeProvider(IScopeContext scopeContext = null)
        {
            IScopeProvider scopeProvider = Mock.Of<IScopeProvider>();
            Mock.Get(scopeProvider)
                .Setup(x => x.Context).Returns(scopeContext);
            return scopeProvider;
        }
    }

    /// <summary>
    /// Used for tests so that we don't have to wrap every Set/Clear call in locks
    /// </summary>
    public static class SnapDictionaryExtensions
    {
        internal static void Set<TKey, TValue>(this SnapDictionary<TKey, TValue> d, TKey key, TValue value)
            where TValue : class
        {
            using (d.GetScopedWriteLock(GetScopeProvider()))
            {
                d.SetLocked(key, value);
            }
        }

        internal static void Clear<TKey, TValue>(this SnapDictionary<TKey, TValue> d)
            where TValue : class
        {
            using (d.GetScopedWriteLock(GetScopeProvider()))
            {
                d.ClearLocked();
            }
        }

        internal static void Clear<TKey, TValue>(this SnapDictionary<TKey, TValue> d, TKey key)
            where TValue : class
        {
            using (d.GetScopedWriteLock(GetScopeProvider()))
            {
                d.ClearLocked(key);
            }
        }

        private static IScopeProvider GetScopeProvider()
        {
            IScopeProvider scopeProvider = Mock.Of<IScopeProvider>();
            Mock.Get(scopeProvider)
                .Setup(x => x.Context).Returns(() => null);
            return scopeProvider;
        }
    }
}