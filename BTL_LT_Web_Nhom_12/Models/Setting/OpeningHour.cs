using System.ComponentModel.DataAnnotations;

namespace BTL_LT_Web_Nhom_12.Models.Setting
{
    public class OpeningHour
    {
        [Key]
        public int OpeningHourId { get; set; }

        [Range(0, 6)]
        public int DayOfWeek { get; set; }

        public TimeSpan OpenTime { get; set; }

        public TimeSpan CloseTime { get; set; }

        public bool IsClosed { get; set; } = false;
    }
}
