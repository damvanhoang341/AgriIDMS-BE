
using AgriIDMS.Application.DTOs.Cart;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;
        private readonly ICartItemService _cartItemService;
        private readonly IProductVariantRepository _variantRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly INearExpiryDiscountService _nearExpiryDiscountService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CartService> _logger;

        public CartService(
            ICartRepository cartRepo,
            ICartItemService cartItemService,
            IProductVariantRepository variantRepo,
            IBoxRepository boxRepo,
            INearExpiryDiscountService nearExpiryDiscountService,
            IUnitOfWork uow,
            ILogger<CartService> logger)
        {
            _cartRepo = cartRepo;
            _cartItemService = cartItemService;
            _variantRepo = variantRepo;
            _boxRepo = boxRepo;
            _nearExpiryDiscountService = nearExpiryDiscountService;
            _uow = uow;
            _logger = logger;
        }

        /// <summary>Đơn giá /kg lưu trong giỏ — khớp <see cref="OrderService"/> khi đặt hàng (ưu đãi gần HSD / ghi đè variant).</summary>
        private async Task<decimal> ResolveCartUnitPricePerKgAsync(int productVariantId, decimal basePricePerKg)
        {
            var result = await _nearExpiryDiscountService.CalculateForVariantAsync(
                productVariantId,
                basePricePerKg,
                includeOfflineOnly: false);
            return result.FinalUnitPrice;
        }

        private const decimal CartPriceRefreshTolerance = 0.02m;

        /// <summary>Cập nhật đơn giá các dòng theo ưu đãi hiện tại (dòng cũ có thể còn giá niêm yết).</summary>
        private async Task RefreshCartItemUnitPricesAsync(Cart cart)
        {
            if (cart.Items == null || !cart.Items.Any())
                return;

            var changed = false;
            foreach (var item in cart.Items)
            {
                var basePrice = item.ProductVariant?.Price ?? 0m;
                if (basePrice <= 0)
                    continue;

                var resolved = await ResolveCartUnitPricePerKgAsync(item.ProductVariantId, basePrice);
                if (Math.Abs(item.UnitPrice - resolved) > CartPriceRefreshTolerance)
                {
                    item.UnitPrice = resolved;
                    changed = true;
                }
            }

            if (!changed)
                return;

            cart.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
        }

        public async Task<CartDto> GetMyCartAsync(string userId)
        {
            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);
            if (cart == null)
            {
                return new CartDto
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            await RefreshCartItemUnitPricesAsync(cart);

            var items = _cartItemService.GetCartItemDtos(cart);
            return new CartDto
            {
                Items = items.ToList(),
                TotalAmount = items.Sum(x => x.LineAmount),
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt
            };
        }

        public async Task AddOrUpdateItemAsync(AddCartItemRequest request, string userId)
        {
            var variant = await _variantRepo.GetProductVariantByIdAsync(request.ProductVariantId)
                ?? throw new NotFoundException("Không tìm thấy biến thể sản phẩm.");
            var unitPricePerKg = await ResolveCartUnitPricePerKgAsync(variant.Id, variant.Price);

            await _uow.ExecuteInRetryableTransactionAsync(async () =>
            {
                var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);

                var availableBoxes = await _boxRepo.GetAvailableBoxCountByVariantAndTypeAsync(
                    request.ProductVariantId,
                    request.IsPartial,
                    request.BoxWeight);

                var alreadyInCart = cart?.Items?.FirstOrDefault(i =>
                    i.ProductVariantId == request.ProductVariantId &&
                    i.IsPartial == request.IsPartial &&
                    i.BoxWeight == request.BoxWeight);
                var requestedTotal = request.Quantity + (int)(alreadyInCart?.Quantity ?? 0);

                if (requestedTotal > availableBoxes)
                    throw new InvalidBusinessRuleException(
                        $"Số lượng box yêu cầu ({requestedTotal}) vượt số box khả dụng ({availableBoxes}).");

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _cartRepo.AddAsync(cart);
                    await _uow.SaveChangesAsync();
                }

                var existingItem = cart.Items.FirstOrDefault(i =>
                    i.ProductVariantId == request.ProductVariantId &&
                    i.IsPartial == request.IsPartial &&
                    i.BoxWeight == request.BoxWeight);
                if (existingItem == null)
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductVariantId = request.ProductVariantId,
                        Quantity = request.Quantity,
                        BoxWeight = request.BoxWeight,
                        IsPartial = request.IsPartial,
                        UnitPrice = unitPricePerKg
                    });
                }
                else
                {
                    existingItem.Quantity += request.Quantity;
                    existingItem.UnitPrice = unitPricePerKg;
                }

                cart.UpdatedAt = DateTime.UtcNow;
            });

            _logger.LogInformation(
                "Cart updated for user {UserId}: variant {VariantId} qty {Qty}",
                userId, request.ProductVariantId, request.Quantity);
        }

        public async Task UpdateItemQuantityAsync(int productVariantId, UpdateCartItemRequest request, string userId)
        {
            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId)
                ?? throw new NotFoundException("Giỏ hàng trống");

            var item = cart.Items.FirstOrDefault(i =>
                    i.ProductVariantId == productVariantId &&
                    i.IsPartial == request.IsPartial &&
                    i.BoxWeight == request.BoxWeight)
                ?? throw new NotFoundException("Sản phẩm không có trong giỏ hàng");

            var availableBoxes = await _boxRepo.GetAvailableBoxCountByVariantAndTypeAsync(
                productVariantId,
                request.IsPartial,
                request.BoxWeight);
            if (request.Quantity > availableBoxes)
                throw new InvalidBusinessRuleException(
                    $"Số lượng box yêu cầu ({request.Quantity}) vượt số box khả dụng ({availableBoxes}).");

            var variant = await _variantRepo.GetProductVariantByIdAsync(productVariantId)
                ?? throw new NotFoundException("Không tìm thấy biến thể sản phẩm.");
            var unitPricePerKg = await ResolveCartUnitPricePerKgAsync(variant.Id, variant.Price);

            item.Quantity = request.Quantity;
            item.UnitPrice = unitPricePerKg;
            cart.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Cart item updated for user {UserId}: variant {VariantId} new qty {Qty}",
                userId, productVariantId, request.Quantity);
        }

        public async Task RemoveItemAsync(int productVariantId, decimal boxWeight, bool isPartial, string userId)
        {
            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId)
                ?? throw new NotFoundException("Giỏ hàng trống");

            var item = cart.Items.FirstOrDefault(i =>
                    i.ProductVariantId == productVariantId &&
                    i.IsPartial == isPartial &&
                    i.BoxWeight == boxWeight)
                ?? throw new NotFoundException("Sản phẩm không có trong giỏ hàng");

            _cartRepo.RemoveItem(item);
            cart.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Cart item removed for user {UserId}: variant {VariantId}",
                userId, productVariantId);
        }

        public async Task ClearCartAsync(string userId)
        {
            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);
            if (cart == null) return;

            await _cartRepo.ClearCartAsync(cart);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Cart cleared for user {UserId}", userId);
        }
    }
}
