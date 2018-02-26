namespace JoinRpg.DataModel.Extensions
{
    public static class AccommodationRequestExtensions
    {
        public static int GetAbstractRoomFreeSpace(this AccommodationRequest request)
        {
            return request.AccommodationType.Capacity -
                   request.Subjects.Count;
        }
    }
}
