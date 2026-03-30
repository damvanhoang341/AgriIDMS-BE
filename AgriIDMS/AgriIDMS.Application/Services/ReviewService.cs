using AgriIDMS.Application.DTOs.Review;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class ReviewService : IReviewService
    {
        private const int ReviewWindowStartDays = 3;
        private const int ReviewWindowEndDays = 7;

        private readonly IReviewRepository _reviewRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(
            IReviewRepository reviewRepository,
            IUnitOfWork unitOfWork)
        {
            _reviewRepository = reviewRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ReviewResponseDto> CreateReviewAsync(CreateReviewRequest request, string customerId)
        {
            await ValidateReviewEligibility(request.OrderDetailId, customerId);

            var orderDetail = await _reviewRepository.GetOrderDetailForReviewAsync(request.OrderDetailId)
                ?? throw new NotFoundException("Order detail không tồn tại");

            var review = new Review
            {
                OrderDetailId = orderDetail.Id,
                ProductVariantId = orderDetail.ProductVariantId,
                CustomerId = customerId,
                Rating = request.Rating,
                Freshness = request.Freshness,
                Packaging = request.Packaging,
                Comment = request.Comment?.Trim(),
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            return new ReviewResponseDto
            {
                Id = review.Id,
                OrderDetailId = review.OrderDetailId,
                ProductVariantId = review.ProductVariantId,
                CustomerId = review.CustomerId,
                Rating = review.Rating,
                Freshness = review.Freshness,
                Packaging = review.Packaging,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }

        public async Task<bool> IsReviewableAsync(int orderDetailId, string customerId)
        {
            try
            {
                await ValidateReviewEligibility(orderDetailId, customerId);
                return true;
            }
            catch (DomainException)
            {
                return false;
            }
            catch (ForbiddenException)
            {
                return false;
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        public async Task<ApprovedReviewListResponseDto> GetApprovedReviewsByProductVariantAsync(int productVariantId, int skip, int take)
        {
            var normalizedSkip = skip < 0 ? 0 : skip;
            var normalizedTake = take <= 0 ? 10 : Math.Min(take, 100);

            var reviews = await _reviewRepository.GetApprovedByProductVariantAsync(productVariantId, normalizedSkip, normalizedTake);
            var items = reviews.Select(r => new ApprovedReviewItemDto
            {
                Id = r.Id,
                ProductVariantId = r.ProductVariantId,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer?.FullName,
                Rating = r.Rating,
                Freshness = r.Freshness,
                Packaging = r.Packaging,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();

            return new ApprovedReviewListResponseDto
            {
                ProductVariantId = productVariantId,
                Skip = normalizedSkip,
                Take = normalizedTake,
                Count = items.Count,
                Items = items
            };
        }

        public async Task ValidateReviewEligibility(int orderDetailId, string customerId)
        {
            var orderDetail = await _reviewRepository.GetOrderDetailForReviewAsync(orderDetailId)
                ?? throw new NotFoundException("Order detail không tồn tại");

            var order = orderDetail.Order ?? throw new NotFoundException("Order không tồn tại");

            if (order.UserId != customerId)
                throw new ForbiddenException("Bạn không có quyền review sản phẩm của đơn hàng này");

            if (order.Status != OrderStatus.Delivered)
                throw new InvalidBusinessRuleException("Chỉ được review khi đơn hàng ở trạng thái Delivered");

            if (orderDetail.Review != null)
                throw new InvalidBusinessRuleException("Mỗi OrderDetail chỉ được review 1 lần");

            if (!order.DeliveredAt.HasValue)
                throw new InvalidBusinessRuleException("Đơn chưa có mốc DeliveredAt để tính thời gian review");

            var daysAfterDelivered = (DateTime.UtcNow - order.DeliveredAt.Value).TotalDays;
            if (daysAfterDelivered < ReviewWindowStartDays || daysAfterDelivered > ReviewWindowEndDays)
                throw new InvalidBusinessRuleException(
                    $"Chỉ được review trong khoảng {ReviewWindowStartDays}-{ReviewWindowEndDays} ngày sau khi giao hàng");

            var latestComplaintStatus = await _reviewRepository.GetLatestComplaintStatusAsync(order.Id, orderDetail.Id);
            if (latestComplaintStatus.HasValue)
            {
                if (latestComplaintStatus.Value != ComplaintStatus.Verified)
                    throw new InvalidBusinessRuleException("Đơn có khiếu nại và chưa Verified, không thể review");
            }

            if (await _reviewRepository.HasNonResolvedComplaintAsync(order.Id, orderDetail.Id))
                throw new InvalidBusinessRuleException("Đơn có khiếu nại chưa Verified, không thể review");
        }
    }
}
