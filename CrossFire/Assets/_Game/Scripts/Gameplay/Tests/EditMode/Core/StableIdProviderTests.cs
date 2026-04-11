using CrossFire.Core;
using NUnit.Framework;

namespace CrossFire.Tests.EditMode
{
	public class StableIdProviderTests
	{
		[SetUp]
		public void SetUp()
		{
			StableIdProvider.Reset();
		}

		[Test]
		public void Next_FirstCall_ReturnsZero()
		{
			int id = StableIdProvider.Next();

			Assert.AreEqual(0, id);
		}

		[Test]
		public void Next_SequentialCalls_ReturnIncreasingValues()
		{
			int first = StableIdProvider.Next();
			int second = StableIdProvider.Next();
			int third = StableIdProvider.Next();

			Assert.AreEqual(0, first);
			Assert.AreEqual(1, second);
			Assert.AreEqual(2, third);
		}

		[Test]
		public void Next_SequentialCalls_AllValuesUnique()
		{
			int[] ids = new int[100];
			for (int i = 0; i < ids.Length; i++)
			{
				ids[i] = StableIdProvider.Next();
			}

			System.Collections.Generic.HashSet<int> unique = new System.Collections.Generic.HashSet<int>(ids);
			Assert.AreEqual(ids.Length, unique.Count);
		}

		[Test]
		public void Reset_AfterSeveralCalls_NextReturnsZeroAgain()
		{
			StableIdProvider.Next();
			StableIdProvider.Next();
			StableIdProvider.Next();

			StableIdProvider.Reset();

			int id = StableIdProvider.Next();
			Assert.AreEqual(0, id);
		}

		[Test]
		public void Reset_ResetsSequenceFromBeginning()
		{
			StableIdProvider.Next();
			StableIdProvider.Next();
			StableIdProvider.Reset();

			int first = StableIdProvider.Next();
			int second = StableIdProvider.Next();

			Assert.AreEqual(0, first);
			Assert.AreEqual(1, second);
		}
	}
}
