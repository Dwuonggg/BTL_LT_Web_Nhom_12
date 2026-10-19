
namespace BTL_LT_Web_Nhom_12.ViewModels
{
    public class CartItemViewModel
    {
        public string Type { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; } = 1;

        public decimal LineTotal => UnitPrice * Quantity;
        public string? Note { get; set; }
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();

        public int TotalQuantity =>
            Items.Sum(x => x.Quantity);

        public decimal Subtotal =>
            Items.Sum(x => x.LineTotal);
    }
}
