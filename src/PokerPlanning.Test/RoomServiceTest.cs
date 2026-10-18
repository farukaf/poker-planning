using PokerPlanning.Services;

namespace PokerPlanning.Test;

public class RoomServiceTest
{
    [Fact]
    public void Create_ShouldAddRoom()
    {
        // Arrange
        var service = new RoomService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomService>.Instance);
        var cardValues = new string[] { "1", "2", "3" };

        // Act
        var roomId = service.Create(cardValues);
        var room = service.GetRoom(roomId);

        // Assert
        Assert.NotNull(room);
        Assert.Equal(cardValues, room.CardValues);
    }

    [Fact]
    public void GetRoom_ShouldReturnNull_WhenRoomDoesNotExist()
    {
        // Arrange
        var service = new RoomService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomService>.Instance);

        // Act
        var room = service.GetRoom(Guid.NewGuid());

        // Assert
        Assert.Null(room);
    }

    [Fact]
    public async Task CleanRooms_ShouldRemoveOldRooms()
    {
        // Arrange
        var service = new RoomService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomService>.Instance);
        var oldRoomId = service.Create(new string[] { "1", "2", "3" });
        service.Rooms[oldRoomId].UpdateAt = DateTime.UtcNow.AddHours(-1);
        var newRoomId = service.Create(new string[] { "4", "5", "6" });

        // Act
        var removedCount = await service.CleanRooms(DateTime.UtcNow.AddMinutes(-1));

        // Assert
        Assert.Equal(1, removedCount);
        Assert.Single(service.Rooms);
        Assert.Null(service.GetRoom(oldRoomId));
        Assert.NotNull(service.GetRoom(newRoomId));
    }
}
