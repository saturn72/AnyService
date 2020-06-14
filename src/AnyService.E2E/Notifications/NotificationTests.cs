using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AnyService.E2E.Notifications
{
    public class NotificationTests : BasicE2ETests
    {
        public NotificationTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task AnyServiceSupportForSignalR()
        {
            string expPayload = "this is my payload",
                payload = null;
            var serverUrl = HttpClient.BaseAddress;

            var connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/ChatHub")
                .Build();

            connection.On<string>("ReceiveMessage", (p) =>
            {
                payload = p;
            });

            await connection.StartAsync();

            int delay = 100,
            counter = 1000;

            using var hc = new HttpClient() { BaseAddress = new Uri("https://localhost:5001/") };
            await hc.PostAsJsonAsync("notify", expPayload);
            do
            {
                await Task.Delay(delay);
                counter -= 100;
            }
            while (payload == null && counter > 0);

            payload.ShouldBe(expPayload);
        }

    }
}
