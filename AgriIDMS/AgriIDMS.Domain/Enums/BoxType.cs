namespace AgriIDMS.Domain.Enums
{
    /// <summary>Loại bao bì / đóng gói (khác với <see cref="Box.IsPartial"/> = box đầy hay lẻ).</summary>
    public enum BoxType
    {
        /// <summary>Chưa xác định hoặc dữ liệu cũ.</summary>
        Unknown = 0,
        /// <summary>Thùng xốp.</summary>
        StyrofoamBox = 1,
        /// <summary>Thùng carton.</summary>
        Carton = 2,
        /// <summary>Bao lưới.</summary>
        MeshBag = 3,
        /// <summary>Sọt.</summary>
        Crate = 4
    }
}
