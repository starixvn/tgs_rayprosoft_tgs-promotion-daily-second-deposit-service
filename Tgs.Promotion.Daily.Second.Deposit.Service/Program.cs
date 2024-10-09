using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.EntityFrameworkCore;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using Tgs.Payment.Protos;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion;
using Tgs.Promotion.Daily.Second.Deposit.Core;
using Tgs.Promotion.Daily.Second.Deposit.Core.DependencyInjection;
using Tgs.Promotion.Daily.Second.Deposit.Core.MessageReply;
using Tgs.Promotion.Daily.Second.Deposit.Repository.DependencyInjection;
using Tgs.Promotion.Daily.Second.Deposit.Service.NatsSerializers;
using TGS.Core.Infrastructures.Nats;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
            fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
                "MachineName",
                () => Environment.MachineName));
        });
    });

builder.Services
    .AddKeyedSingleton<IConnectionMultiplexer>(
        "Redis",
        (sp, _) => ConnectionMultiplexer.Connect(
            builder.Configuration.GetConnectionString("Redis")!))
    .AddKeyedSingleton(
        "Redis",
        (sp, _) => sp.GetRequiredKeyedService<IConnectionMultiplexer>("Redis").GetDatabase())
    .AddPromotionDailySecondDepositCore()
    .AddDailySecondDepositRepositories(
        dbBuilder => dbBuilder
            .UseNpgsql("name=Tgs"),
        sp => sp.GetRequiredKeyedService<IDatabase>("Redis"))
    .AddPromotionClientServices((opts, _) => opts.Url =
        builder.Configuration.GetValue<Uri>("DailySecondDeposit:PromotionServiceBaseUrl")!)
    .AddPaymentServiceClient(httpBuilder => httpBuilder
        .ConfigureHttpClient(httpClient => httpClient.BaseAddress =
            builder.Configuration.GetValue<Uri>("DailySecondDeposit:PaymentServiceBaseUrl")!))
    .AddCalculateClient(httpBuilder => httpBuilder
        .ConfigureHttpClient(httpClient => httpClient.BaseAddress =
            builder.Configuration.GetValue<Uri>("DailySecondDeposit:CalculateServiceBaseUrl")!))
    .AddMemoryCache();

var instrumentationExtensionFilters = new[] { "html", "ico", "png", "jpg" };
_ = builder.Services
    .AddOpenTelemetry()
    .WithTracing(
        tracingBuilder => tracingBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddEnvironmentVariableDetector())
            .AddSource("Tgs.*")
            .AddSource("TGS.*")
            .AddSource("Valhalla.*")
            .AddAspNetCoreInstrumentation(options => options.Filter =
                httpContext => !httpContext.Request.Path.HasValue
                               || !httpContext.Request.Path.StartsWithSegments("/swagger")
                               && !httpContext.Request.Path.StartsWithSegments("/healthz")
                               && !httpContext.Request.Path.StartsWithSegments("/metrics")
                               && !httpContext.Request.Path.StartsWithSegments("/profiler/")
                               && !instrumentationExtensionFilters.Any(
                                   ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
            .AddProfileViewExporter()
            .AddOtlpExporter())
    .WithMetrics(meterBuilder => meterBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter())
    .Services
    .AddHealthChecks()
    .Services
    .AddProfileViewer();

builder.Services.AddNatsMessage(builder.Configuration.GetConnectionString("Nats")!, ProtobufSerializerRegistry.Default)
    .RequestReplyRegister<CampaignDetailRequest, CampaignDetailResponse>(typeof(DepositRuleReplyProcessor),
        "tgs.promotion.dailyseconddeposit.setting", "tgs.promotion.dailyseconddeposit.setting")
    .RequestReplyRegister(typeof(DetailReplyProcessor),
        string.Format(PromotionDetailConst.DetailSubjectTemplate, "dailyseconddeposit"),
        string.Format(PromotionDetailConst.DetailSubjectTemplate, "dailyseconddeposit"),
        NatsJsonSerializerRegistry.Default.GetDeserializer<DistributedDetailRequest>(),
        NatsJsonSerializerRegistry.Default.GetSerializer<DistributedDetailResponse>())
    .SubscribeRegister<FormEventEx>(
        ProcessorTypes.DepositFormStatusChangedHandlerType,
        "tgs.payment.deposit.status_ex",
        "tgs.promotion.daily-second-deposit.deposit");

var app = builder.Build();
{
    app.MapHealthChecks("/healthz");
    app.MapProfileViewer();
}

app.Run();
