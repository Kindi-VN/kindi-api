using AutoMapper;
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
		CreateMap<CreatePurchaseRequestCommand, PurchaseRequest>();
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