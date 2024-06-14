using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using DataSystem.Controller;
using DataSystem.Database;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MockQueryable.Moq;
using Moq;

namespace DataSystem.Tests.Controller;

[TestClass]
[TestSubject(typeof(DataController))]
public class DataControllerTest
{
    private static readonly Mock<ApplicationContext> Context = new();
    private static DataController DataController;
    
    [ClassInitialize]
    public static async Task ClassInitialize(TestContext testContext)
    {
        var authorization = new Authorization()
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
            new ()
            {
                Id = 1,
                TimeStamp = DateTime.Now,
                DeviceId = PhysicalAddress.Parse("A9612CF6BB19".AsSpan()),
                Temperature = 30.1m,
                ProviderToken = authorization
            },
            new ()
            {
                Id = 2,
                TimeStamp = DateTime.Now.AddHours(2),
                DeviceId = PhysicalAddress.Parse("A9612CF6BB21".AsSpan()),
                Temperature = 30.3m,
                ProviderToken = authorization
            },
            new ()
            {
                Id = 3,
                TimeStamp = DateTime.Now,
                DeviceId = PhysicalAddress.Parse("A9612CF6BB19".AsSpan()),
                Temperature = 33.5m,
                ProviderToken = authorization
            },
            new ()
            {
                Id = 3,
                TimeStamp = DateTime.Now.AddHours(2),
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

        Context.Setup(m => m.SensorData).Returns(mockSensorDataSet.Object);
        Context.Setup(m => m.Authorization).Returns(mockAuthorizationSet.Object);
        
        // create new DataController
        DataController = new DataController(Context.Object);
    }
    
    [TestMethod]
    [DataRow("11111111-1111-1111-1111-111111111112", 4, null, null)]
    [DataRow("11111111-1111-1111-1111-111111111112", 2, "A9612CF6BB19", null)]
    [DataRow("11111111-1111-1111-1111-111111111112", 1, "A9612CF6BB19", 1)]
    public async Task GetLatestDataTest(string token, int expectedCount, string deviceId, int maxCount = 100)
    {
        var result = await DataController.GetLatestData(token, deviceId, maxCount);

        Assert.AreEqual(expectedCount, result.Value?.Count);
    }
}