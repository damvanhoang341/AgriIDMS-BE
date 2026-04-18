namespace AgriIDMS.Domain.Enums
{
    /// <summary>Kết quả xử lý sau khi Manager duyệt phiếu hỏng (không liên quan giảm giá).</summary>
    public enum DamageProcessingOutcome
    {
        /// <summary>Toàn bộ thùng loại khỏi tồn bán.</summary>
        CompleteDamaged = 0,

        /// <summary>Một phần khối lượng hỏng; phần còn lại vẫn là thùng Stored nếu còn &gt; 0.</summary>
        PartialDamaged = 1
    }
}
