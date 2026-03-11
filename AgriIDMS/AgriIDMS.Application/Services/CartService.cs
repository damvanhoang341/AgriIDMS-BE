using AgriIDMS.Application.DTOs.Cart;
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
                ProductName = i.ProductVariant?.Product.Name ?? string.Empty,
                Grade = i.ProductVariant?.Grade.ToString() ?? string.Empty,
                Quantity = i.Quantity,
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
            if (request.Quantity <= 0)
                throw new InvalidBusinessRuleException("Quantity phải lớn hơn 0");

            var variant = await _variantRepo.GetProductVariantByIdAsync(request.ProductVariantId);

            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);

            var available = await _boxRepo.GetAvailableQuantityByVariantIdAsync(request.ProductVariantId);
            var alreadyInCart = cart?.Items?.FirstOrDefault(i => i.ProductVariantId == request.ProductVariantId);
            var requestedTotal = request.Quantity + (alreadyInCart?.Quantity ?? 0);

            if (requestedTotal > available)
                throw new InvalidBusinessRuleException(
                    $"Số lượng yêu cầu ({requestedTotal}) vượt tồn kho khả dụng ({available}).");

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
            await _cartRepo.UpdateAsync(cart);
            await _uow.SaveChangesAsync();
        }

        public async Task ClearCartAsync(string userId)
        {
            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);
            if (cart == null) return;

            await _cartRepo.ClearCartAsync(cart);
            await _uow.SaveChangesAsync();
        }
    }
}

