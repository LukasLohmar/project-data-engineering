using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataSystem.Database;
using DataSystem.Grpc;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MockQueryable.Moq;
using Moq;
using DataService = DataSystem.Service.DataService;

namespace DataSystem.Tests.Service;

[TestClass]
[TestSubject(typeof(DataService))]
public class DataServiceTest
{
    [TestMethod]
    public async Task SaveTest()
    {
        // setup SensorData context
        var mockSensorDataSet = new List<SensorData>().AsQueryable().BuildMockDbSet();

        // setup Authorization context with objects
        var mockAuthorizationSet = new List<Authorization>
        {
            // save should fail with this authorization -> token can only read, write and has none (uninitialized)
            new()
            {
                Id = 1,
                Locked = false,
                Token = new Guid("11111111-1111-1111-1111-111111111111"),
                AuthorizedFlags = AuthorizeFlags.None | AuthorizeFlags.Read | AuthorizeFlags.Delete,
                CreatedAt = DateTime.Now
            },
            // save should fail with this authorization -> token is locked (deactivated)
            new()
            {
                Id = 2,
                Locked = true,
                Token = new Guid("11111111-1111-1111-1111-111111111112"),
                AuthorizedFlags = AuthorizeFlags.Write,
                CreatedAt = DateTime.Now
            },
            // save should succeed with this authorization
            new()
            {
                Id = 3,
                Locked = false,
                Token = new Guid("11111111-1111-1111-1111-111111111113"),
                AuthorizedFlags = AuthorizeFlags.Write,
                CreatedAt = DateTime.Now
            }
        }.AsQueryable().BuildMockDbSet();

        // setup ApplicationContext
        var mockContext = new Mock<ApplicationContext>();

        mockContext.Setup(m => m.SensorData).Returns(mockSensorDataSet.Object);
        mockContext.Setup(m => m.Authorization).Returns(mockAuthorizationSet.Object);
        mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // create new DataService
        var dataService = new DataService(mockContext.Object);

        // create generic ServerCallContext -> https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/grpc/test-services/sample/Tests/Server/UnitTests/Helpers/TestServerCallContext.cs
        var serverContext = TestServerCallContext.Create();

        // fails due to missing timestamp
        var newMessage = new SaveRequest
        {
            // authorized token
            AuthorizationToken = "11111111-1111-1111-1111-111111111113",
            DeviceId = "A9-61-2C-F6-BB-21"
        };

        var messageResult = await dataService.Save(newMessage, serverContext);

        Assert.AreEqual(BasicReply.Types.ResponseValue.ResponseInternalServerError, messageResult.ResponseState);
        Assert.AreEqual("error: no timestamp was provided", messageResult.ResponseMessage);

        // fails due to missing DeviceId
        newMessage = new SaveRequest
        {
            // authorized token
            AuthorizationToken = "11111111-1111-1111-1111-111111111113",
            TimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture)
        };

        messageResult = await dataService.Save(newMessage, serverContext);

        Assert.AreEqual(BasicReply.Types.ResponseValue.ResponseInternalServerError, messageResult.ResponseState);
        Assert.AreEqual("error: no device-id was provided", messageResult.ResponseMessage);

        // fail message 1 -> fails with authorization token id: 1
        newMessage = new SaveRequest
        {
            AuthorizationToken = "11111111-1111-1111-1111-111111111111",
            TimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture),
            DeviceId = "A9-61-2C-F6-BB-21"
        };

        messageResult = await dataService.Save(newMessage, serverContext);

        Assert.AreEqual(BasicReply.Types.ResponseValue.ResponseUnauthorized, messageResult.ResponseState);

        // fail message 2 -> fails with authorization token id: 2
        newMessage = new SaveRequest
        {
            AuthorizationToken = "11111111-1111-1111-1111-111111111112",
            TimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture),
            DeviceId = "A9-61-2C-F6-BB-21"
        };

        messageResult = await dataService.Save(newMessage, serverContext);

        Assert.AreEqual(BasicReply.Types.ResponseValue.ResponseUnauthorized, messageResult.ResponseState);
        Assert.AreEqual(0, mockContext.Object.SensorData.Count());

        // fail message 3 -> fails when data was not saved -> token id: 3
        newMessage = new SaveRequest
        {
            AuthorizationToken = "11111111-1111-1111-1111-111111111113",
            TimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture),
            DeviceId = "A9-61-2C-F6-BB-21"
        };

        messageResult = await dataService.Save(newMessage, serverContext);

        Assert.AreEqual(BasicReply.Types.ResponseValue.ResponseOk, messageResult.ResponseState);
        mockSensorDataSet.Verify(m => m.AddAsync(It.IsAny<SensorData>(), It.IsAny<CancellationToken>()), Times.Once());
        mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }
}