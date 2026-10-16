using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Payments.Models;

/// <summary>Represents optional filters used to list payments.</summary>
public sealed record PaymentFilter(long? MemberId, DateOnly? From, DateOnly? To, PaymentMethod? Method);
