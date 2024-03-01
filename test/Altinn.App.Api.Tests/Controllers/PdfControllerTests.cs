using Altinn.App.Api.Controllers;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Infrastructure.Clients.Pdf;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Profile;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using IAppResources = Altinn.App.Core.Internal.App.IAppResources;

namespace Altinn.App.Api.Tests.Controllers
{
    public class PdfControllerTests
    {
        private readonly string org = "org";
        private readonly string app = "app";
        private readonly Guid instanceId = new Guid("e11e3e0b-a45c-48fb-a968-8d4ddf868c80");
        private readonly int partyId = 12345;
        private readonly string taskId = "Task_1";

        private readonly Mock<IAppResources> _appResources = new();
        private readonly Mock<IDataClient> _dataClient = new();
        private readonly Mock<IProfileClient> _profile = new();
        private readonly IOptions<PlatformSettings> _platformSettingsOptions = Microsoft.Extensions.Options.Options.Create<PlatformSettings>(new() { });
        private readonly Mock<IInstanceClient> _instanceClient = new();
        private readonly Mock<IPdfFormatter> _pdfFormatter = new();
        private readonly Mock<IAppModel> _appModel = new();
        private readonly Mock<IUserTokenProvider> _userTokenProvider = new();

        private readonly IOptions<PdfGeneratorSettings> _pdfGeneratorSettingsOptions = Microsoft.Extensions.Options.Options.Create<PdfGeneratorSettings>(new() { });


        public PdfControllerTests()
        {
            _instanceClient
                .Setup(a => a.GetInstance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Instance()
                {
                    Org = org,
                    AppId = $"{org}/{app}",
                    Id = $"{partyId}/{instanceId}",
                    Process = new ProcessState()
                    {
                        CurrentTask = new ProcessElementInfo() { ElementId = taskId, },
                    }
                }));
        }

        [Fact]
        public async Task Request_In_Prod_Should_Be_Blocked()
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(a => a.EnvironmentName).Returns("Production");

            IOptions<GeneralSettings> generalSettingsOptions =
                Microsoft.Extensions.Options.Options.Create<GeneralSettings>(new() { HostName = "org.apps.altinn.no" });

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext!.Request!.Query["lang"]).Returns("nb");

            var handler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handler.Object);

            var pdfGeneratorClient = new PdfGeneratorClient(httpClient, _pdfGeneratorSettingsOptions, _platformSettingsOptions, _userTokenProvider.Object, httpContextAccessor.Object);
            var pdfService = new PdfService(_appResources.Object, _dataClient.Object, httpContextAccessor.Object, _profile.Object, pdfGeneratorClient, _pdfGeneratorSettingsOptions, generalSettingsOptions);
            var pdfController = new PdfController(_instanceClient.Object, _pdfFormatter.Object, _appResources.Object, _appModel.Object, _dataClient.Object, env.Object, pdfService);

            var result = await pdfController.GetPdfPreview(org, app, partyId, instanceId);

            result.Should().BeOfType(typeof(NotFoundResult));
            handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task Request_In_Dev_Should_Generate()
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(a => a.EnvironmentName).Returns("Development");

            IOptions<GeneralSettings> generalSettingsOptions =
                Microsoft.Extensions.Options.Options.Create<GeneralSettings>(new() { HostName = "local.altinn.cloud" });

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext!.Request!.Query["lang"]).Returns("nb");
            string? frontendVersion = null;
            httpContextAccessor.Setup(x => x.HttpContext!.Request!.Cookies.TryGetValue("frontendVersion", out frontendVersion)).Returns(false);

            var handler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handler.Object);

            var pdfGeneratorClient = new PdfGeneratorClient(httpClient, _pdfGeneratorSettingsOptions, _platformSettingsOptions, _userTokenProvider.Object, httpContextAccessor.Object);
            var pdfService = new PdfService(_appResources.Object, _dataClient.Object, httpContextAccessor.Object, _profile.Object, pdfGeneratorClient, _pdfGeneratorSettingsOptions, generalSettingsOptions);
            var pdfController = new PdfController(_instanceClient.Object, _pdfFormatter.Object, _appResources.Object, _appModel.Object, _dataClient.Object, env.Object, pdfService);

            string? requestBody = null;
            using (var mockResponse = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent("PDF") })
            {
                handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .Callback<HttpRequestMessage, CancellationToken>((m, c) => requestBody = m.Content!.ReadAsStringAsync().Result)
                    .ReturnsAsync(mockResponse);

                var result = await pdfController.GetPdfPreview(org, app, partyId, instanceId);
                result.Should().BeOfType(typeof(FileStreamResult));
            }

            requestBody.Should().Contain(@"url"":""http://local.altinn.cloud/org/app/#/instance/12345/e11e3e0b-a45c-48fb-a968-8d4ddf868c80?pdf=1");
            requestBody.Should().NotContain(@"name"":""frontendVersion");
        }

        [Fact]
        public async Task Request_In_Dev_Should_Include_Frontend_Version()
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(a => a.EnvironmentName).Returns("Development");

            IOptions<GeneralSettings> generalSettingsOptions =
                Microsoft.Extensions.Options.Options.Create<GeneralSettings>(new() { HostName = "local.altinn.cloud" });

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext!.Request!.Query["lang"]).Returns("nb");
            string? frontendVersion = "https://altinncdn.no/toolkits/altinn-app-frontend/3/";
            httpContextAccessor.Setup(x => x.HttpContext!.Request!.Cookies.TryGetValue("frontendVersion", out frontendVersion)).Returns(true);

            var handler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handler.Object);

            var pdfGeneratorClient = new PdfGeneratorClient(httpClient, _pdfGeneratorSettingsOptions, _platformSettingsOptions, _userTokenProvider.Object, httpContextAccessor.Object);
            var pdfService = new PdfService(_appResources.Object, _dataClient.Object, httpContextAccessor.Object, _profile.Object, pdfGeneratorClient, _pdfGeneratorSettingsOptions, generalSettingsOptions);
            var pdfController = new PdfController(_instanceClient.Object, _pdfFormatter.Object, _appResources.Object, _appModel.Object, _dataClient.Object, env.Object, pdfService);

            string? requestBody = null;
            using (var mockResponse = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent("PDF") })
            {
                handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .Callback<HttpRequestMessage, CancellationToken>((m, c) => requestBody = m.Content!.ReadAsStringAsync().Result)
                    .ReturnsAsync(mockResponse);

                var result = await pdfController.GetPdfPreview(org, app, partyId, instanceId);
                result.Should().BeOfType(typeof(FileStreamResult));
            }

            requestBody.Should().Contain(@"url"":""http://local.altinn.cloud/org/app/#/instance/12345/e11e3e0b-a45c-48fb-a968-8d4ddf868c80?pdf=1");
            requestBody.Should().Contain(@"name"":""frontendVersion"",""value"":""https://altinncdn.no/toolkits/altinn-app-frontend/3/""");
        }

        [Fact]
        public async Task Request_In_TT02_Should_Ignore_Frontend_Version()
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(a => a.EnvironmentName).Returns("Staging");

            IOptions<GeneralSettings> generalSettingsOptions =
                Microsoft.Extensions.Options.Options.Create<GeneralSettings>(new() { HostName = "org.apps.tt02.altinn.no" });

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext!.Request!.Query["lang"]).Returns("nb");
            string? frontendVersion = "https://altinncdn.no/toolkits/altinn-app-frontend/3/";
            httpContextAccessor.Setup(x => x.HttpContext!.Request!.Cookies.TryGetValue("frontendVersion", out frontendVersion)).Returns(true);

            var handler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handler.Object);

            var pdfGeneratorClient = new PdfGeneratorClient(httpClient, _pdfGeneratorSettingsOptions, _platformSettingsOptions, _userTokenProvider.Object, httpContextAccessor.Object);
            var pdfService = new PdfService(_appResources.Object, _dataClient.Object, httpContextAccessor.Object, _profile.Object, pdfGeneratorClient, _pdfGeneratorSettingsOptions, generalSettingsOptions);
            var pdfController = new PdfController(_instanceClient.Object, _pdfFormatter.Object, _appResources.Object, _appModel.Object, _dataClient.Object, env.Object, pdfService);

            string? requestBody = null;
            using (var mockResponse = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent("PDF") })
            {
                handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .Callback<HttpRequestMessage, CancellationToken>((m, c) => requestBody = m.Content!.ReadAsStringAsync().Result)
                    .ReturnsAsync(mockResponse);

                var result = await pdfController.GetPdfPreview(org, app, partyId, instanceId);
                result.Should().BeOfType(typeof(FileStreamResult));
            }

            requestBody.Should().Contain(@"url"":""http://org.apps.tt02.altinn.no/org/app/#/instance/12345/e11e3e0b-a45c-48fb-a968-8d4ddf868c80?pdf=1");
            requestBody.Should().NotContain(@"name"":""frontendVersion");
        }
    }
}
