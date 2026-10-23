using System;

namespace WarehouseAnalytics
{
    public class InventoryItem
    {
        public string Sku { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Warehouse { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime LastUpdated { get; set; }

        // Giá trị tồn = Số lượng x Đơn giá
        public decimal TotalValue => Quantity * UnitPrice;
    }
}
