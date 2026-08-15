using System;
using System.Collections.Generic;
using System.Reflection;
using BbxCommon;
using NUnit.Framework;

namespace Hearthstone.Tests
{
    public sealed class GameStageTransactionTests
    {
        [Test]
        public void StrictValidationRejectsLegacyItemBeforeLoadSideEffects()
        {
            using var world = CreateWorld(nameof(StrictValidationRejectsLegacyItemBeforeLoadSideEffects));
            var stage = CreateStage(world.Instance);
            var legacy = new RecordingLegacyItem(1, null, false);
            stage.AddLoadItem(legacy);
            var context = CreateContext(1, true, EGameStageTransitionPhase.Validate);

            var exception = Assert.Throws<InvalidOperationException>(
                () => Invoke(stage, "ValidateTransition", context));

            StringAssert.Contains(nameof(ITransactionalStageLoad), exception.Message);
            Assert.That(legacy.LoadCount, Is.Zero);
            Assert.That(legacy.UnloadCount, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StrictValidationRejectsUnsupportedSceneAndDataAdapters(bool scene)
        {
            using var world = CreateWorld(nameof(StrictValidationRejectsUnsupportedSceneAndDataAdapters));
            var stage = CreateStage(world.Instance);
            if (scene)
                stage.AddScene("TransactionTestScene");
            else
                stage.AddDataGroup("TransactionTestData");
            var context = CreateContext(10, true, EGameStageTransitionPhase.Validate);

            var exception = Assert.Throws<NotSupportedException>(
                () => Invoke(stage, "ValidateTransition", context));

            StringAssert.Contains(scene ? "inactive-scene staging adapter" : "DataApi overlay adapter", exception.Message);
        }

        [Test]
        public void PartialPrepareRollsBackOnlyCompletedItems()
        {
            using var world = CreateWorld(nameof(PartialPrepareRollsBackOnlyCompletedItems));
            var stage = CreateStage(world.Instance);
            var state = new SideEffectState();
            var completed = new RecordingTransactionalItem(state, false);
            var failing = new RecordingTransactionalItem(state, true);
            stage.AddLoadItem(completed);
            stage.AddLoadItem(failing);
            var context = CreateContext(2, true, EGameStageTransitionPhase.Validate);

            Invoke(stage, "ValidateTransition", context);
            SetPhase(context, EGameStageTransitionPhase.Prepare);
            Assert.Throws<InvalidOperationException>(() => Invoke(stage, "PrepareTransition", context));

            var rollbackErrors = new List<Exception>();
            Invoke(stage, "RollbackTransition", context, rollbackErrors);

            Assert.That(completed.PrepareCount, Is.EqualTo(1));
            Assert.That(completed.RollbackCount, Is.EqualTo(1));
            Assert.That(failing.PrepareCount, Is.EqualTo(1));
            Assert.That(failing.RollbackCount, Is.Zero,
                "The item that threw before Prepare returned owns its own partial cleanup.");
            Assert.That(state.Value, Is.Zero);
            Assert.That(rollbackErrors, Is.Empty);
        }

        [Test]
        public void CompensationRunsInReverseAndAggregatesCleanupFailure()
        {
            var context = CreateContext(3, true, EGameStageTransitionPhase.CommitTargetHidden);
            var calls = new List<int>();
            context.RegisterCompensation(() => calls.Add(1));
            context.RegisterCompensation(() =>
            {
                calls.Add(2);
                throw new InvalidOperationException("cleanup");
            });
            context.RegisterCompensation(() => calls.Add(3));
            var errors = new List<Exception>();

            Invoke(context, "RollbackCompensations", errors);

            Assert.That(calls, Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Is.EqualTo("cleanup"));
        }

        [Test]
        public void OldUnloadContinuesAfterMiddleFailureAndMarksStageUnloaded()
        {
            using var world = CreateWorld(nameof(OldUnloadContinuesAfterMiddleFailureAndMarksStageUnloaded));
            var stage = CreateStage(world.Instance);
            var unloadOrder = new List<int>();
            stage.AddLoadItem(new RecordingLegacyItem(1, unloadOrder, false));
            stage.AddLoadItem(new RecordingLegacyItem(2, unloadOrder, true));
            stage.AddLoadItem(new RecordingLegacyItem(3, unloadOrder, false));
            SetField(stage, "m_Loaded", true);
            var errors = new List<Exception>();

            Invoke(stage, "UnloadBestEffort", errors);

            Assert.That(unloadOrder, Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Is.EqualTo("unload-2"));
            Assert.That(GetField<bool>(stage, "m_Loaded"), Is.False);
        }

        private static GameStage CreateStage(object world)
        {
            return (GameStage)Activator.CreateInstance(
                typeof(GameStage),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { "TransactionTest", world },
                null);
        }

        private static DisposableWorld CreateWorld(string name)
        {
            var worldType = Type.GetType("Unity.Entities.World, Unity.Entities", true);
            var worldFlagsType = Type.GetType("Unity.Entities.WorldFlags, Unity.Entities", true);
            var simulationFlags = Enum.Parse(worldFlagsType, "Simulation");
            return new DisposableWorld(Activator.CreateInstance(worldType, name, simulationFlags));
        }

        private static GameStageTransitionContext CreateContext(
            long attemptId,
            bool strict,
            EGameStageTransitionPhase phase)
        {
            var context = (GameStageTransitionContext)Activator.CreateInstance(
                typeof(GameStageTransitionContext),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { attemptId, strict },
                null);
            SetPhase(context, phase);
            return context;
        }

        private static void SetPhase(GameStageTransitionContext context, EGameStageTransitionPhase phase)
        {
            typeof(GameStageTransitionContext)
                .GetProperty(nameof(GameStageTransitionContext.Phase))
                .SetValue(context, phase);
        }

        private static void Invoke(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName}.");
            try
            {
                method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw exception.InnerException;
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            return (T)target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);
        }

        private sealed class SideEffectState
        {
            public int Value;
        }

        private sealed class DisposableWorld : IDisposable
        {
            public object Instance { get; }

            public DisposableWorld(object instance)
            {
                Instance = instance;
            }

            public void Dispose()
            {
                (Instance as IDisposable)?.Dispose();
            }
        }

        private sealed class RecordingTransactionalItem : ITransactionalStageLoad
        {
            private readonly SideEffectState m_State;
            private readonly bool m_FailPrepare;

            public int PrepareCount { get; private set; }
            public int RollbackCount { get; private set; }

            public RecordingTransactionalItem(SideEffectState state, bool failPrepare)
            {
                m_State = state;
                m_FailPrepare = failPrepare;
            }

            public void Validate(GameStage stage, GameStageTransitionContext context) { }

            public void Prepare(GameStage stage, GameStageTransitionContext context)
            {
                PrepareCount++;
                if (m_FailPrepare)
                    throw new InvalidOperationException("prepare");
                m_State.Value++;
            }

            public void Load(GameStage stage) { }
            public void Unload(GameStage stage) { }

            public void Rollback(GameStage stage, GameStageTransitionContext context)
            {
                RollbackCount++;
                m_State.Value--;
            }
        }

        private sealed class RecordingLegacyItem : IStageLoad
        {
            private readonly int m_Id;
            private readonly List<int> m_UnloadOrder;
            private readonly bool m_FailUnload;

            public int LoadCount { get; private set; }
            public int UnloadCount { get; private set; }

            public RecordingLegacyItem(int id, List<int> unloadOrder, bool failUnload)
            {
                m_Id = id;
                m_UnloadOrder = unloadOrder;
                m_FailUnload = failUnload;
            }

            public void Load(GameStage stage) => LoadCount++;

            public void Unload(GameStage stage)
            {
                UnloadCount++;
                m_UnloadOrder?.Add(m_Id);
                if (m_FailUnload)
                    throw new InvalidOperationException($"unload-{m_Id}");
            }
        }
    }
}
