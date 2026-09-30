using System.ComponentModel.DataAnnotations;

namespace ManTingEats.Models.Validation;

/// <summary>金額需為整數（台幣不收小數）；出單以 F0 列印，若允許小數會造成畫面與出單金額不一致。</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class WholeAmountAttribute : ValidationAttribute
{
    public WholeAmountAttribute() : base("金額需為整數")
    {
    }

    public override bool IsValid(object? value) =>
        value is not decimal amount || amount == decimal.Truncate(amount);
}
