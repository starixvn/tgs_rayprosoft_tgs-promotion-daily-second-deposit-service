using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.Daily.Second.Deposit.Core.MessageReply;

namespace Tgs.Promotion.Daily.Second.Deposit.UnitTest;

public class FirstDepositSettingProcessorTests
{
    [Test]
    public async Task DepositRuleReplyProcessor_處理設定值變更要求()
    {
        var sut = new DepositRuleReplyProcessor(
            NullLogger<DepositRuleReplyProcessor>.Instance);

        var request = new CampaignDetailRequest
        {
            PromotionId = Guid.NewGuid(),
            Settings = "{}"
        };

        // Act
        var actual = await sut.Handle("test", "test", null, request);

        // Assert
        Assert.That(actual, Is.Not.Null);
    }
}
