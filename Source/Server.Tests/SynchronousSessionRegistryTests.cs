using RTServer.Managers;

namespace RTServer.Tests
{
    public class SynchronousSessionRegistryTests
    {
        [Fact]
        public void AcceptRoutesToRequesterEvenWhenBothPlayersShareATile()
        {
            SynchronousSessionRegistry sessions = new();

            Assert.True(sessions.TryRegisterRequest(requesterId: 42, hostId: 7));
            Assert.True(sessions.TryAccept(hostId: 7, out int requesterId));
            Assert.Equal(42, requesterId);
            Assert.True(sessions.TryGetPartner(7, out int hostPartner));
            Assert.Equal(42, hostPartner);
            Assert.True(sessions.TryGetPartner(42, out int guestPartner));
            Assert.Equal(7, guestPartner);
        }

        [Fact]
        public void RejectReturnsTheOriginalRequester()
        {
            SynchronousSessionRegistry sessions = new();

            Assert.True(sessions.TryRegisterRequest(requesterId: 11, hostId: 3));
            Assert.True(sessions.TryReject(hostId: 3, out int requesterId));
            Assert.Equal(11, requesterId);
            Assert.False(sessions.TryGetPartner(3, out _));
        }

        [Fact]
        public void DisconnectClearsBothSidesOfAnActiveSession()
        {
            SynchronousSessionRegistry sessions = new();
            sessions.TryRegisterRequest(requesterId: 12, hostId: 4);
            sessions.TryAccept(hostId: 4, out _);

            sessions.ClearClient(12);

            Assert.False(sessions.TryGetPartner(12, out _));
            Assert.False(sessions.TryGetPartner(4, out _));
        }

        [Fact]
        public void ClientCannotInviteItself()
        {
            SynchronousSessionRegistry sessions = new();

            Assert.False(sessions.TryRegisterRequest(requesterId: 5, hostId: 5));
        }
    }
}
