using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Edit Mode tests for InputBuffer — pure logic, no engine loop required.
    /// Tests define the contract for how buffered input works.
    /// </summary>
    [TestFixture]
    public class InputBufferTests
    {
        private InputBuffer _buffer;

        [SetUp]
        public void SetUp()
        {
            _buffer = new InputBuffer();
        }

        [Test]
        public void RecordInput_StoresActionWithCorrectTimestamp()
        {
            // Arrange
            float timestamp = 1.5f;

            // Act
            _buffer.RecordInput(InputAction.Punch, timestamp);
            var buffered = _buffer.GetAllBuffered();

            // Assert
            Assert.AreEqual(1, buffered.Count);
            Assert.AreEqual(InputAction.Punch, buffered[0].Action);
            Assert.AreEqual(timestamp, buffered[0].Timestamp, 0.001f);
        }

        [Test]
        public void PurgeStaleInputs_RemovesInputsOlderThanMaxAge()
        {
            // Arrange
            _buffer.RecordInput(InputAction.Punch, 1.0f);
            _buffer.RecordInput(InputAction.Kick, 2.0f);
            _buffer.RecordInput(InputAction.Jump, 3.0f);

            // Act — purge anything older than 0.5s relative to time 3.0
            _buffer.PurgeStaleInputs(3.0f, 0.5f);
            var remaining = _buffer.GetAllBuffered();

            // Assert — only the Jump at 3.0 should survive
            Assert.AreEqual(1, remaining.Count);
            Assert.AreEqual(InputAction.Jump, remaining[0].Action);
        }

        [Test]
        public void ConsumeInput_ReturnsMatchingUnconsumedInput()
        {
            // Arrange
            _buffer.RecordInput(InputAction.Punch, 1.0f);

            // Act
            var consumed = _buffer.ConsumeInput(InputAction.Punch);

            // Assert
            Assert.IsTrue(consumed.HasValue);
            Assert.AreEqual(InputAction.Punch, consumed.Value.Action);
        }

        [Test]
        public void ConsumeInput_ReturnsNullWhenNoMatch()
        {
            // Arrange
            _buffer.RecordInput(InputAction.Punch, 1.0f);

            // Act
            var consumed = _buffer.ConsumeInput(InputAction.Kick);

            // Assert
            Assert.IsFalse(consumed.HasValue);
        }

        [Test]
        public void ConsumeInput_DoesNotReturnAlreadyConsumedInput()
        {
            // Arrange
            _buffer.RecordInput(InputAction.Punch, 1.0f);
            _buffer.ConsumeInput(InputAction.Punch); // consume it

            // Act
            var secondConsume = _buffer.ConsumeInput(InputAction.Punch);

            // Assert — already consumed, should return null
            Assert.IsFalse(secondConsume.HasValue);
        }

        [Test]
        public void ConsumeInput_ReturnsMostRecentUnconsumedMatch()
        {
            // Arrange
            _buffer.RecordInput(InputAction.Punch, 1.0f);
            _buffer.RecordInput(InputAction.Kick, 1.1f);
            _buffer.RecordInput(InputAction.Punch, 1.2f);

            // Act
            var consumed = _buffer.ConsumeInput(InputAction.Punch);

            // Assert
            Assert.IsTrue(consumed.HasValue);
            Assert.AreEqual(1.2f, consumed.Value.Timestamp, 0.001f);
        }

        [Test]
        public void HasBufferedInput_ReturnsTrueWithinWindow()
        {
            // Arrange — record a punch at time 1.0
            _buffer.RecordInput(InputAction.Punch, 1.0f);

            // Act — check within a generous window
            // Design intent: HasBufferedInput checks relative to the most recent
            // input's timestamp. Implementation must track "current time" or accept it.
            bool hasPunch = _buffer.HasBufferedInput(InputAction.Punch, 0.5f);

            // Assert
            Assert.IsTrue(hasPunch);
        }

        [Test]
        public void HasBufferedInput_ReturnsFalseOutsideWindow()
        {
            // Arrange
            _buffer.RecordInput(InputAction.Punch, 1.0f);
            _buffer.RecordInput(InputAction.Kick, 2.0f);

            // Act
            bool hasPunch = _buffer.HasBufferedInput(InputAction.Punch, 0.5f);

            // Assert
            Assert.IsFalse(hasPunch);
        }

        [Test]
        public void Clear_RemovesAllBufferedInputs()
        {
            // Arrange
            _buffer.RecordInput(InputAction.Punch, 1.0f);
            _buffer.RecordInput(InputAction.Kick, 1.1f);
            _buffer.RecordInput(InputAction.Jump, 1.2f);

            // Act
            _buffer.Clear();

            // Assert
            Assert.AreEqual(0, _buffer.GetAllBuffered().Count);
        }

        [Test]
        public void GetMoveAxis_ReturnsLatestAxisValue()
        {
            // Arrange — simulate stick movement
            _buffer.RecordInput(InputAction.MoveRight, 1.0f, new Vector2(1f, 0f));
            _buffer.RecordInput(InputAction.MoveUp, 1.1f, new Vector2(1f, 1f));

            // Act
            Vector2 axis = _buffer.GetMoveAxis();

            // Assert — should reflect the most recent axis input
            Assert.AreEqual(1f, axis.x, 0.01f);
            Assert.AreEqual(1f, axis.y, 0.01f);
        }

        [Test]
        public void RecordInput_MoveRightWithoutAxis_UsesDirectionalFallback()
        {
            // Arrange / Act
            _buffer.RecordInput(InputAction.MoveRight, 1.0f);

            // Assert
            Assert.AreEqual(Vector2.right, _buffer.GetMoveAxis());
        }
    }
}
