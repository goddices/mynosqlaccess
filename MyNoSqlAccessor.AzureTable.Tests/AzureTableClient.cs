using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyNoSqlAccessor.AzureTable.Extensions;

namespace MyNoSqlAccessor.AzureTable.Tests
{
    [TestClass]
    public class AzureTableClient
    {
        private ServiceCollection services;
        private AzureTableClientFactory factory;
        private INoSqlTable<TestEmployee> emptable;
        private INoSqlTable<TestDepartment> deptab;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public async Task Init()
        {
            CancellationToken token = default;
            services = new ServiceCollection();
            services.AddIndexManager();
            services.AddScoped<IEntityConverter<TestEmployee>, TestEmployeeConverter>();
            services.AddScoped<IEntityConverter<TestDepartment>, AutoEntityConverter<TestDepartment>>();
            services.AddScoped<IEntityConverter<BrokenType>, AutoEntityConverter<BrokenType>>();

            services.AddIndex<TestEmployee>("default", index => index
                .ForPartition(p => p.Add(e => e.DepartmentId))
                .ForRowKey(r => r.Id()));
            services.AddIndex<TestEmployee>("name", index => index
               .ForPartition(p => p.Add(e => e.DepartmentId))
               .ForRowKey(r => r.Add(x => x.Name)));


            services.AddIndex<TestDepartment>("default", index => index
                .ForPartition(p => p.Add(e => e.CompanyId))
                .ForRowKey(r => r.Add(e => e.Id)));

            services.AddIndex<TestDepartment>("name", index => index
                .ForPartition(p => p.Add(e => e.CompanyId))
                .ForRowKey(r => r.Const("namerow").Add(e => e.Name)));

            services.AddIndex<BrokenType>("default", index => index
                .ForPartition(p => p.Add(e => e.Unsupported))
                .ForRowKey(r => r.Add(e => e.YourName)));

            var sp = services.BuildServiceProvider();

            // any one of *.runsettings which contains ConnectionString is required. 
            var connstr = this.TestContext.Properties["ConnectionString"].ToString().Trim();

            factory = new AzureTableClientFactory(connstr, sp);

            emptable = factory.CreateClient<TestEmployee>();
            await emptable.CreateTableIfNotExistsAsync(token);

            deptab = factory.CreateClient<TestDepartment>();
            await deptab.CreateTableIfNotExistsAsync(token);

        }

        [TestCleanup]
        public async Task Cleanup()
        {
            CancellationToken token = default;
            await deptab.DeleteTableIfExistsAsync(token);
            await emptable.DeleteTableIfExistsAsync(token);
        }

        [TestMethod]
        public async Task FullTest()
        {
            CancellationToken token = default;

            // test insert
            var newId = Guid.NewGuid();
            var insertedEntity = await emptable.InsertEntityIndexAsync(new TestEmployee()
            {
                DepartmentId = 1,
                Id = newId,
                Name = "testpersion",
                OnboardDate = DateTimeOffset.Now
            }, "default", token);

            Assert.AreEqual(newId, insertedEntity.Id);

            // test duplicate insert which throws 409 Conflict
            var conflict = await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
                await emptable.InsertEntityIndexAsync(new TestEmployee()
                {
                    DepartmentId = 1,
                    Id = newId,
                    Name = "testpersion",
                    OnboardDate = DateTimeOffset.Now
                }, "default", token)
            );
            Assert.AreEqual(nameof(HttpStatusCode.Conflict), conflict.Message);


            // test get null
            var getEntity = await emptable.GetEntityIndexAsync(new TestEmployee { Id = Guid.NewGuid(), DepartmentId = 1 }, "default", token);
            Assert.IsNull(getEntity);

            // test get
            getEntity = await emptable.GetEntityIndexAsync(new TestEmployee { Id = newId, DepartmentId = 1 }, "default", token);
            Assert.AreEqual(newId, getEntity.Id);

            // test replace
            insertedEntity.Name = Guid.NewGuid().ToString();
            var replaced = await emptable.ReplaceEntityIndexAsync(insertedEntity, "default", token);
            Assert.AreEqual(insertedEntity.Name, replaced.Name);

            // test dirty etag
            getEntity.Name = "another name";
            var http412 = await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
            {
                var replaced2 = await emptable.ReplaceEntityIndexAsync(insertedEntity, "default", token);
            });
            Assert.AreEqual(nameof(HttpStatusCode.PreconditionFailed), http412.Message);

            // test delete 
            await emptable.DeleteEntityIndexAsync(new TestEmployee { Id = newId, DepartmentId = 1 }, "default", token);
            getEntity = await emptable.GetEntityIndexAsync(new TestEmployee { Id = newId, DepartmentId = 1 }, "default", token);
            Console.WriteLine("expect delete and get null {0}", getEntity == null);

            Random rand = new Random();
            var newDepart = await deptab.InsertEntityIndexAsync(new TestDepartment
            {
                CompanyId = 11,
                Id = rand.Next(100000),
                ManagerId = Guid.NewGuid(),
                Name = "testde"
            }, "default", token);

            // BrokenType
            Assert.ThrowsException<NotSupportedException>(() => factory.CreateClient<BrokenType>());

            foreach (var _ in Enumerable.Range(1, 3))
            {
                await deptab.InsertEntityIndexAsync(
                    new TestDepartment
                    {
                        CompanyId = 1,
                        Id = rand.Next(100),
                        Name = "1111ÖÐÎÄ" + Guid.NewGuid().ToString("N"),
                        ManagerId = Guid.NewGuid()
                    }, "name", token);

                await deptab.InsertEntityIndexAsync(
                    new TestDepartment
                    {
                        CompanyId = 1,
                        Id = rand.Next(100),
                        Name = "RPA" + Guid.NewGuid().ToString("N"),
                        ManagerId = Guid.NewGuid()
                    }, "name", token);
            }


            // query for department name start with '1', like '1abc','1234567'
            var list2 = await deptab.QueryEntitiesIndexByPrefixAsync("name", new TestDepartment
            {
                CompanyId = 1,
                Name = "1"
            }, 10, token);

            Assert.IsTrue(list2.Any());


            await emptable.InsertEntityIndexAsync(new TestEmployee
            {
                DepartmentId = 1,
                Id = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                Name = "aaa",
                OnboardDate = DateTimeOffset.Now
            }, "default", token);
            await emptable.InsertEntityIndexAsync(new TestEmployee
            {
                DepartmentId = 1,
                Id = Guid.Parse("01000000-0000-0000-0000-000000000000"),
                Name = "aaa",
                OnboardDate = DateTimeOffset.Now
            }, "default", token);
            await emptable.InsertEntityIndexAsync(new TestEmployee
            {
                DepartmentId = 1,
                Id = Guid.Parse("20000000-0000-0000-0000-000000000000"),
                Name = "aaa",
                OnboardDate = DateTimeOffset.Now
            }, "default", token);

            await emptable.InsertEntityIndexAsync(new TestEmployee
            {
                DepartmentId = 1,
                Id = Guid.Parse("40000000-0000-0000-0000-000000000000"),
                Name = "aaa",
                OnboardDate = DateTimeOffset.Now
            }, "default", token);

            await emptable.InsertEntityIndexAsync(new TestEmployee
            {
                DepartmentId = 1,
                Id = Guid.Parse("40000000-0000-0000-0000-000000000000"),
                Name = "aaa",
                OnboardDate = DateTimeOffset.Now
            }, "name", token);
            await emptable.InsertEntityIndexAsync(new TestEmployee
            {
                DepartmentId = 1,
                Id = Guid.Parse("40000000-0000-0000-0000-000000000000"),
                Name = "aaabbbb",
                OnboardDate = DateTimeOffset.Now
            }, "name", token);
            await emptable.InsertEntityIndexAsync(new TestEmployee
            {
                DepartmentId = 1,
                Id = Guid.Parse("40000000-0000-0000-0000-000000000000"),
                Name = "ccccc",
                OnboardDate = DateTimeOffset.Now
            }, "name", token);
            await emptable.InsertEntityIndexAsync(new TestEmployee
            {
                DepartmentId = 1,
                Id = Guid.Parse("40000000-0000-0000-0000-000000000000"),
                Name = "ÎûÎûÎûÎû",
                OnboardDate = DateTimeOffset.Now
            }, "name", token);



            var list = await emptable.QueryEntitiesIndexByRangeAsync(
                "default",
                new TestEmployee { DepartmentId = 1, Id = Guid.Parse("00000000-0000-0000-0000-000000000000") },
                true,
                new TestEmployee { DepartmentId = 1, Id = Guid.Parse("3abad168-6bfc-4b0a-9c95-2960fee61833") },
                true,
                300,
                token);

            Assert.AreEqual(3, list.Count());

            list = await emptable.QueryEntitiesIndexByPrefixAsync(
                "name",
                new TestEmployee { DepartmentId = 1, Name = "aaa" },
                200,
                token);
            Assert.AreEqual(2, list.Count());

        }
    }
}
