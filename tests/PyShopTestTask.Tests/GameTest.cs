using Xunit;
using Task1;

namespace PyShopTestTask.Tests
{
    public class GameTest
    {
        private static readonly Game _testGame = new Game(new GameStamp[]
        {
            new GameStamp(0, 0, 0),
            new GameStamp(10, 0, 0),
            new GameStamp(20, 0, 1),
            new GameStamp(30, 0, 1),
            new GameStamp(40, 1, 1),
            new GameStamp(50, 1, 1),
            new GameStamp(60, 1, 2),
            new GameStamp(70, 2, 2),
        });

        private static IEnumerable<object[]> DataForCorrectArguments()
        {
            yield return new object[] { _testGame, 0, new Score(0, 0) };
            yield return new object[] { _testGame, 19, new Score(0, 0) };
            yield return new object[] { _testGame, 20, new Score(0, 1) };
            yield return new object[] { _testGame, 30, new Score(0, 1) };
            yield return new object[] { _testGame, 59, new Score(1, 1) };
            yield return new object[] { _testGame, 69, new Score(1, 2) };
            yield return new object[] { _testGame, 70, new Score(2, 2) };

        }

        [Theory]
        [MemberData(nameof(DataForCorrectArguments))]
        public void GetScore_WhenCorretArguments_ReturnsCorrectValues(Game game, int offset, Score expected)
        {
            var actual = game.getScore(offset);

            Assert.Equal(expected, actual);
        }

        private static IEnumerable<object[]> DataForNullOrEmpty()
        {
            yield return new object[] { null };
            yield return new object[] { new GameStamp[] { } };
        }

        [Theory]
        [MemberData(nameof(DataForNullOrEmpty))]
        public void GetScore_WhenGameStampsAreNullOreEmpty_ThrowsException(GameStamp[] gameStamps)
        {
            // Arrange
            Game cut = new Game(gameStamps);

            // Act & Assert
            Assert.Throws<Exception>(() => cut.getScore(100));
        }

        private static IEnumerable<object[]> DataForOutOfRange()
        {
            yield return new object[] { new GameStamp[] { new GameStamp(0, 0, 0),
                new GameStamp(70, 1, 2) }, -1 };
            yield return new object[] { new GameStamp[] { new GameStamp(0, 0, 0),
                new GameStamp(70, 1, 2) }, 71 };
        }

        [Theory]
        [MemberData(nameof(DataForOutOfRange))]
        public void GetScore_WhenWrongOffset_ThrowsOutOfRangeException(GameStamp[] gameStamps, int offset)
        {
            // Arrange
            Game cut = new Game(gameStamps);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => cut.getScore(offset));
        }
    }
}
