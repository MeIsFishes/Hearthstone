using System.Reflection;
using NUnit.Framework;

namespace Hearthstone.Tests
{
    public sealed class PreparationContinueTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void ContinueLineup_DefensivelyCapturesSparseSlots(int occupiedCount)
        {
            var source = new RunCardInstanceData[RunCardRules.BattleSlotCount];
            for (var slot = 0; slot < occupiedCount; slot++)
                source[slot] = new RunCardInstanceData(slot + 1, slot + 2, slot + 4);

            var lineup = new BattlePlayerLineupStartupData(source);
            source[0] = default;

            for (var slot = 0; slot < RunCardRules.BattleSlotCount; slot++)
            {
                Assert.AreEqual(slot < occupiedCount, lineup.GetSlot(slot).IsValid);
                if (slot < occupiedCount)
                    Assert.AreEqual(slot + 1, lineup.GetSlot(slot).CardNumber);
            }
        }

        [Test]
        public void NormalContinue_UsesNullScenarioAndCanonicalLineupKey()
        {
            var lineup = new BattlePlayerLineupStartupData(new[]
            {
                new RunCardInstanceData(1, 3, 4),
                default,
                new RunCardInstanceData(4, 2, 5),
            });
            var startup = new BattleStageStartupData(2, CreateRewardBatch(), null, lineup);
            Assert.IsNull(startup.Scenario);
            Assert.AreEqual(1, startup.ContinuePlayerLineup.GetSlot(0).CardNumber);
            Assert.IsFalse(startup.ContinuePlayerLineup.GetSlot(1).IsValid);

            var method = typeof(HearthstoneGameEngine).GetMethod(
                "CreateBattleRequestKey",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var key = (string)method.Invoke(null, new object[] { startup });
            StringAssert.Contains("Battle=2", key);
            StringAssert.Contains("Scenario=Default", key);
            StringAssert.Contains("P0=1:3:4", key);
            StringAssert.Contains("P1=Empty", key);
            StringAssert.Contains("P2=4:2:5", key);
        }

        [Test]
        public void Coordinator_DeduplicatesPendingRequestUntilCompletion()
        {
            var coordinator = new HearthstoneStageGroupTransitionCoordinator();
            Assert.IsTrue(coordinator.Request(EHearthstoneStageGroup.Preparation, "prep"));
            Assert.IsTrue(coordinator.TryBeginTransition(out var group, out var key));
            coordinator.CompleteTransition(group, key);

            Assert.IsTrue(coordinator.Request(EHearthstoneStageGroup.Battle, "battle-2"));
            Assert.IsFalse(coordinator.Request(EHearthstoneStageGroup.Battle, "battle-2"));
            Assert.IsTrue(coordinator.TryBeginTransition(out group, out key));
            coordinator.CompleteTransition(group, key);

            Assert.AreEqual(EHearthstoneStageGroup.Battle, coordinator.ActiveGroup);
            Assert.AreEqual(EStageGroupTransitionPhase.Active, coordinator.Phase);
            Assert.IsFalse(coordinator.Request(EHearthstoneStageGroup.Battle, "battle-2"));
            Assert.IsFalse(coordinator.TryBeginTransition(out _, out _));
        }

        private static PreparationRewardBatchStartupData CreateRewardBatch()
        {
            return new PreparationRewardBatchStartupData("battle-002-reward-001", new[]
            {
                new RewardCardGrantStartupData(8, 3, 4),
                new RewardCardGrantStartupData(9, 2, 5),
                new RewardCardGrantStartupData(10, 3, 6),
                new RewardCardGrantStartupData(11, 4, 7),
                new RewardCardGrantStartupData(12, 4, 2),
            });
        }
    }
}
