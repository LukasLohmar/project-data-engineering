using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using DataSystem.Database;
using DataSystem.Grpc;
using Google.Api;
using Google.Protobuf.WellKnownTypes;
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
    // create generic ServerCallContext -> https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/grpc/test-services/sample/Tests/Server/UnitTests/Helpers/TestServerCallContext.cs
    private static readonly TestServerCallContext ServerContext = TestServerCallContext.Create();

    private static readonly Dictionary<string, DateTime> DateList = new()
    {
        { "now", DateTime.UtcNow },
        { "addHour", DateTime.UtcNow.AddHours(1) },
        { "addTwoHours", DateTime.UtcNow.AddHours(2) },
        { "addDay", DateTime.UtcNow.AddDays(1) },
        { "addTwoDays", DateTime.UtcNow.AddDays(2) },
    };
    
    [TestMethod]
    [DataRow("11111111-1111-1111-1111-111111111113", "A9-61-2C-F6-BB-21", "no timestamp was provided", null, RequestResponseType.ResponseInternalError)]
    [DataRow("11111111-1111-1111-1111-111111111113", null, "no device-id was provided", "now", RequestResponseType.ResponseInternalError)]
    [DataRow("11111111-1111-1111-1111-111111111111", "A9-61-2C-F6-BB-21", null, "now", RequestResponseType.ResponseUnauthorized)]
    [DataRow("11111111-1111-1111-1111-111111111112", "A9-61-2C-F6-BB-21", null, "now", RequestResponseType.ResponseUnauthorized)]
    [DataRow("11111111-1111-1111-1111-111111111113", "A9-61-2C-F6-BB-21", null, "now", RequestResponseType.ResponseOk)]
    public async Task SaveTest(string authorizationToken, string deviceId, string errorString, string requestedDate, RequestResponseType responseType)
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

        var mockContext = new Mock<ApplicationContext>();
        
        mockContext.Setup(m => m.SensorData).Returns(mockSensorDataSet.Object);
        mockContext.Setup(m => m.Authorization).Returns(mockAuthorizationSet.Object);
        mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        
        // create new DataService
        var dataService = new DataService(mockContext.Object);

        // fails due to missing timestamp
        var newMessage = new SaveRequest
        {
            // authorized token
            AuthorizationToken = authorizationToken
        };

        // assign device id if set
        if (!string.IsNullOrEmpty(deviceId))
            newMessage.DeviceId = deviceId;
        
        // assign date if set
        if (!string.IsNullOrEmpty(requestedDate))
            newMessage.TimeStamp = Timestamp.FromDateTime(DateList[requestedDate]);
        
        var messageResult = await dataService.Save(newMessage, ServerContext);
 
        Assert.AreEqual(responseType, messageResult.ResponseState);

        if (!string.IsNullOrEmpty(errorString))
            Assert.AreEqual($"error: {errorString}", messageResult.ResponseMessage);

        // on Ok response check if AddAsync and SaveChangesAsync were actually called
        if (responseType == RequestResponseType.ResponseOk)
        {
            mockSensorDataSet.Verify(m => m.AddAsync(It.IsAny<SensorData>(), It.IsAny<CancellationToken>()), Times.Once());
            mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
    }

    [TestMethod]
    [DataRow("11111111-1111-1111-1111-111111111112", 4, 1, 1, null, 1, 25, RequestOrderBy.OrderByAscending, RequestOrderValue.OrderValueByTimestamp, null)]
    [DataRow("11111111-1111-1111-1111-111111111112", 4, 4, 1, null, 1, 25, RequestOrderBy.OrderByDescending, RequestOrderValue.OrderValueByTimestamp, null)]
    [DataRow("11111111-1111-1111-1111-111111111112", 2, 3, 2, null, 2, 2, RequestOrderBy.OrderByAscending, RequestOrderValue.OrderValueByTimestamp, null)]
    [DataRow("11111111-1111-1111-1111-111111111112", 4, 3, 1, null, 1, 5, RequestOrderBy.OrderByDescending, RequestOrderValue.OrderValueByTemperature, null)]
    [DataRow("11111111-1111-1111-1111-111111111112", 2, 4, 1, "A9612CF6BB21", 1, 5, RequestOrderBy.OrderByAscending, RequestOrderValue.OrderValueByTemperature, null)]
    [DataRow("11111111-1111-1111-1111-111111111112", 1, 2, 1, "A9612CF6BB21", 1, 5, RequestOrderBy.OrderByAscending, RequestOrderValue.OrderValueByTimestamp, "now")]
    [DataRow("11111111-1111-1111-1111-111111111112", 1, 4, 1, "A9612CF6BB21", 1, 5, RequestOrderBy.OrderByAscending, RequestOrderValue.OrderValueByTimestamp, "addTwoDays")]
    public async Task GetDataTest(string token, int expectedCount, int expectedId, int totalPages, string deviceId, int pageIndex, int pageSize, RequestOrderBy order, RequestOrderValue orderValue, string entryDate)
    {
        var authorization = new Authorization
        {
            Id = 1,
            Locked = true,
            Token = new Guid("11111111-1111-1111-1111-111111111111"),
            AuthorizedFlags = AuthorizeFlags.Write,
            CreatedAt = DateTime.Now
        };

        // setup SensorData context
        var mockSensorDataSet = new List<SensorData>
        {
            new()
            {
                Id = 1,
                TimeStamp = DateList["now"],
                DeviceId = PhysicalAddress.Parse("A9612CF6BB19".AsSpan()),
                Temperature = 30.1m,
                ProviderToken = authorization
            },
            new()
            {
                Id = 2,
                TimeStamp = DateList["addHour"],
                DeviceId = PhysicalAddress.Parse("A9612CF6BB21".AsSpan()),
                Temperature = 30.3m,
                ProviderToken = authorization
            },
            new()
            {
                Id = 3,
                TimeStamp = DateList["addTwoHours"],
                DeviceId = PhysicalAddress.Parse("A9612CF6BB19".AsSpan()),
                Temperature = 33.5m,
                ProviderToken = authorization
            },
            new()
            {
                Id = 4,
                TimeStamp = DateList["addTwoDays"],
                DeviceId = PhysicalAddress.Parse("A9612CF6BB21".AsSpan()),
                Temperature = 28.4m,
                ProviderToken = authorization
            }
        }.AsQueryable().BuildMockDbSet();

        // setup Authorization context with objects
        var mockAuthorizationSet = new List<Authorization>
        {
            authorization,
            new()
            {
                Id = 2,
                Locked = false,
                Token = new Guid("11111111-1111-1111-1111-111111111112"),
                AuthorizedFlags = AuthorizeFlags.Read,
                CreatedAt = DateTime.Now
            }
        }.AsQueryable().BuildMockDbSet();

        var mockContext = new Mock<ApplicationContext>();
        
        mockContext.Setup(m => m.SensorData).Returns(mockSensorDataSet.Object);
        mockContext.Setup(m => m.Authorization).Returns(mockAuthorizationSet.Object);

        // create new DataService
        var dataService = new DataService(mockContext.Object);
        
        var requestMessage = new GetDataRequest
        {
            AuthorizationToken = token,
            DeviceId = deviceId,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Order = order,
            OrderValue = orderValue
        };

        if (!string.IsNullOrEmpty(entryDate))
            requestMessage.EntryDate = new()
            {
                Value = Timestamp.FromDateTime(DateList[entryDate])
            };
        
        var result = await dataService.GetData(requestMessage, ServerContext);
        
        Assert.AreEqual(expectedCount, result.Items.Count);
        Assert.AreEqual(pageIndex, result.PageIndex);
        Assert.AreEqual(totalPages, result.TotalPages);
        
        if (expectedId != 0)
        {
            Assert.AreEqual(expectedId, result.Items.First().Id);
        }
    }
}