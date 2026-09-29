using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Features.OfferRequests.Commands;
using Kindi.API.Application.Features.PurchaseRequests.Commands;
using Kindi.API.Domain.Entities;
using System.Reflection;

namespace Kindi.API.Application.Mappings;

public class MappingProfile : Profile
{
	public MappingProfile()
	{
		ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());

		CreateMap<CreateOfferRequestCommand, OfferRequest>();
		// Thông tin cá nhân của yêu cầu lấy từ bảng Users qua navigation User.
		CreateMap<OfferRequest, OfferRequestResponseDto>()
			.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
			.ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : string.Empty))
			.ForMember(dest => dest.Zalo, opt => opt.MapFrom(src => src.User != null ? (src.User.Zalo ?? string.Empty) : string.Empty))
			.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? UserInfo.DisplayEmail(src.User.Email, src.User.Phone) : null));
		CreateMap<CreatePurchaseRequestCommand, PurchaseRequest>();
		CreateMap<PurchaseRequest, PurchaseRequestResponseDto>()
			.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
			.ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : string.Empty))
			.ForMember(dest => dest.Zalo, opt => opt.MapFrom(src => src.User != null ? src.User.Zalo : null))
			.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? UserInfo.DisplayEmail(src.User.Email, src.User.Phone) : null));
	}

	private void ApplyMappingsFromAssembly(Assembly assembly)
	{
		var types = assembly.GetExportedTypes()
			.Where(t => t.GetInterfaces().Any(i =>
				i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
			.ToList();

		foreach (var type in types)
		{
			var instance = Activator.CreateInstance(type);
			var methodInfo = type.GetMethod("Mapping") ?? type.GetInterface("IMapFrom`1")?.GetMethod("Mapping");
			methodInfo?.Invoke(instance, new object[] { this });
		}
	}
}