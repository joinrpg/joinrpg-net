using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models.Schedules
{
    public enum AppointmentErrorType
    {
        NotLocated,

        [Display(Name = "Пересечение с другими мероприятиями")]
        Intersection,
    }

    public struct Rect
    {
        public int Left;
        public int Top;
        public int Width;
        public int Height;
    }

    public class AppointmentBaseViewModel
    {
        public string DisplayName { get; set; }
        public int ProjectId { get; set; }
        public int CharacterId { get; set; }
        public IReadOnlyCollection<User> Users { get; set; }
        public JoinHtmlString Description { get; set; }
    }

    public class AppointmentViewModel : AppointmentBaseViewModel
    {
        public int RoomIndex { get; set; }
        public int RoomCount { get; set; }
        public int TimeSlotIndex { get; set; }
        public int TimeSlotsCount { get; set; }
        public bool AllRooms { get; set; }
        public bool ErrorMode { get; set; }
        public AppointmentErrorType? ErrorType { get; set; }

        public IReadOnlyCollection<TableHeaderViewModel> Rooms { get; set; } = Array.Empty<TableHeaderViewModel>();
        public IReadOnlyCollection<TableHeaderViewModel> Slots { get; set; } = Array.Empty<TableHeaderViewModel>();

        private readonly Lazy<Rect> _bounds;

        public int Left => _bounds.Value.Left;
        public int Top => _bounds.Value.Top;
        public int Width => _bounds.Value.Width;
        public int Height => _bounds.Value.Height;

        public bool HasMasterAccess { get; set; }

        public AppointmentViewModel(Func<Rect> getBounds) => _bounds = new Lazy<Rect>(getBounds);
    }
}
