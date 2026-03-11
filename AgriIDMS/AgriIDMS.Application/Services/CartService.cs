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
        private readonly IProductVariantRepository _variantRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CartService> _logger;

        public CartService(
            ICartRepository cartRepo,
            IProductVariantRepository variantRepo,
            IBoxRepository boxRepo,
            IUnitOfWork uow,
            ILogger<CartService> logger)
        {
            _cartRepo = cartRepo;
            _variantRepo = variantRepo;
            _boxRepo = boxRepo;
            _uow = uow;
            _logger = logger;
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

            var items = cart.Items.Select(i => new CartItemDto
            {
                ProductVariantId = i.ProductVariantId,
                ProductName = i.ProductVariant?.Product?.Name ?? string.Empty,
                Grade = i.ProductVariant?.Grade.ToString() ?? string.Empty,
                Quantity = (int)i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            return new CartDto
            {
                Items = items,
                TotalAmount = items.Sum(x => x.LineAmount),
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt
            };
        }

        public async Task AddOrUpdateItemAsync(AddCartItemRequest request, string userId)
        {
            var variant = await _variantRepo.GetProductVariantByIdAsync(request.ProductVariantId);

            await _uow.BeginTransactionAsync();
            try
            {
                var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);

                var availableBoxes = await _boxRepo.GetAvailableBoxCountByVariantIdAsync(request.ProductVariantId);
                var alreadyInCart = cart?.Items?.FirstOrDefault(i => i.ProductVariantId == request.ProductVariantId);
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

                var existingItem = cart.Items.FirstOrDefault(i => i.ProductVariantId == request.ProductVariantId);
                if (existingItem == null)
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductVariantId = request.ProductVariantId,
                        Quantity = request.Quantity,
                        UnitPrice = variant.Price
                    });
                }
                else
                {
                    existingItem.Quantity += request.Quantity;
                    existingItem.UnitPrice = variant.Price;
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _uow.CommitAsync();

                _logger.LogInformation(
                    "Cart updated for user {UserId}: variant {VariantId} qty {Qty}",
                    userId, request.ProductVariantId, request.Quantity);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateItemQuantityAsync(int productVariantId, UpdateCartItemRequest request, string userId)
        {
            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId)
                ?? throw new NotFoundException("Giỏ hàng trống");

            var item = cart.Items.FirstOrDefault(i => i.ProductVariantId == productVariantId)
                ?? throw new NotFoundException("Sản phẩm không có trong giỏ hàng");

            var availableBoxes = await _boxRepo.GetAvailableBoxCountByVariantIdAsync(productVariantId);
            if (request.Quantity > availableBoxes)
                throw new InvalidBusinessRuleException(
                    $"Số lượng box yêu cầu ({request.Quantity}) vượt số box khả dụng ({availableBoxes}).");

            var variant = await _variantRepo.GetProductVariantByIdAsync(productVariantId);

            item.Quantity = request.Quantity;
            item.UnitPrice = variant.Price;
            cart.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Cart item updated for user {UserId}: variant {VariantId} new qty {Qty}",
                userId, productVariantId, request.Quantity);
        }

        public async Task RemoveItemAsync(int productVariantId, string userId)
        {
            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId)
                ?? throw new NotFoundException("Giỏ hàng trống");

            var item = cart.Items.FirstOrDefault(i => i.ProductVariantId == productVariantId)
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
