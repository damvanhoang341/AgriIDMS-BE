using System;

namespace AgriIDMS.Domain
{
    /// <summary>
    /// Quy tắc xuất kho lạnh: box phải nằm trong kho lạnh đủ thời gian (theo Warehouse.MinColdStorageHours, mặc định 48h) mới được xuất.
    /// </summary>
    public static class ColdStorageExportRule
    {
        /// <summary>
        /// Kiểm tra box đã đủ thời gian lưu lạnh chưa.
        /// </summary>
        /// <param name="placedInColdAt">Thời điểm box được đặt vào slot kho lạnh (Box.PlacedInColdAt).</param>
        /// <param name="minColdStorageHours">Số giờ tối thiểu (Warehouse.MinColdStorageHours, ví dụ 48).</param>
        /// <returns>True nếu đủ điều kiện xuất; false nếu chưa có dữ liệu hoặc chưa đủ giờ.</returns>
        public static bool CanExportFromCold(DateTime? placedInColdAt, decimal minColdStorageHours)
        {
            if (!placedInColdAt.HasValue || minColdStorageHours <= 0)
                return false;
            var hoursElapsed = (DateTime.UtcNow - placedInColdAt.Value).TotalHours;
            return hoursElapsed >= (double)minColdStorageHours;
        }

        /// <summary>
        /// Lấy thông báo lỗi khi box chưa đủ thời gian lưu lạnh (dùng cho InvalidBusinessRuleException).
        /// </summary>
        public static string GetNotEligibleMessage(string boxCode, decimal minHours, DateTime? placedInColdAt)
        {
            if (!placedInColdAt.HasValue)
                return $"Box {boxCode} đang ở kho lạnh nhưng chưa có thời điểm vào kho lạnh. Không được xuất.";
            var hoursElapsed = (DateTime.UtcNow - placedInColdAt.Value).TotalHours;
            return $"Box {boxCode} chưa đủ thời gian lưu lạnh: cần tối thiểu {minHours} giờ, đã {hoursElapsed:F1} giờ.";
        }
    }
}
