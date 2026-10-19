using System.ComponentModel.DataAnnotations;

namespace BTL_LT_Web_Nhom_12.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        [StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(
            @"^0\d{9}$",
            ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [StringLength(300, MinimumLength = 10,
            ErrorMessage = "Địa chỉ phải có từ 10 đến 300 ký tự")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Note { get; set; }

        // Mã Voucher Customer nhập
        [StringLength(50, ErrorMessage =
            "Mã ưu đãi không được quá 50 ký tự")]
        public string? VoucherCode { get; set; }

        // Danh sách món trong giỏ hàng
        public List<CartItemViewModel> Items { get; set; } = new();

        // Tổng tiền chưa giảm
        public decimal Subtotal =>
            Items.Sum(x => x.UnitPrice * x.Quantity);

        // Số tiền được giảm
        public decimal DiscountAmount { get; set; }

        // Phí giao hàng tạm thời
        public decimal DeliveryFee { get; set; } = 0m;

        // Tổng tiền sau Voucher
        public decimal TotalAmount =>
            Math.Max(0m,
                Subtotal - DiscountAmount + DeliveryFee);
    }
}