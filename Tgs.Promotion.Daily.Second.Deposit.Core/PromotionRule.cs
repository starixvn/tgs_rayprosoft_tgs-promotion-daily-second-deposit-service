using System.Text.Json.Serialization;

namespace Tgs.Promotion.Daily.Second.Deposit.Core;
public record PromotionRule(
	[property: JsonPropertyName("compaignDetail")] DepositRewardSetting CampaignDetail);
