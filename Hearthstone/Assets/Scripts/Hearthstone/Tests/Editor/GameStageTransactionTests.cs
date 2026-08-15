using System;
using System.Linq;
using BbxCommon;
using NUnit.Framework;

namespace Hearthstone.Tests
{
    public sealed class GameStageLifecycleTests
    {
        [Test]
        public void StageLoadContractContainsOnlyLoadAndUnload()
        {
            var methodNames = typeof(IStageLoad)
                .GetMethods()
                .Select(method => method.Name)
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEqual(new[] { "Load", "Unload" }, methodNames);
        }

        [Test]
        public void SetActiveGameStageUsesSimpleVoidCommandContract()
        {
            var method = typeof(GameEngineBase<HearthstoneGameEngine>.EngineStageWp)
                .GetMethod(nameof(GameEngineBase<HearthstoneGameEngine>.EngineStageWp.SetActiveGameStage));

            Assert.NotNull(method);
            Assert.AreEqual(typeof(void), method.ReturnType);
        }
    }
}
