using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyNoSqlAccessor.AzureTable.Tests
{
    public class BrokenType
    {
        public Uri Base { get; set; }
        public string YourName { get; set; }
        public byte Unsupported { get; set; }
        public string ETag { get; set; }
    }

    public class TestDepartment
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public string Name { get; set; }

        public Guid ManagerId { get; set; }

        public string ETag { get; set; }
    }

    public class TestEmployee
    {
        public Guid Id { get; set; }

        public int DepartmentId { get; set; }

        public string Name { get; set; }

        public DateTimeOffset OnboardDate { get; set; }

        public string ETag { get; set; }
    }

    public class TestEmployeeConverter : IEntityConverter<TestEmployee>
    {
        public TestEmployee Deserialize(IDictionary<string, EntityProperty> values)
        {
            TestEmployee e = new TestEmployee();
            e.DepartmentId = values["DepartmentId"].Int32Value.Value;
            e.Name = values["Name"].StringValue;
            e.OnboardDate = values["OnboardDate"].DateTimeOffsetValue.Value;
            e.Id = values["Id"].GuidValue.Value;
            return e;
        }

        public IDictionary<string, EntityProperty> Serialize(TestEmployee entity)
        {
            IDictionary<string, EntityProperty> e = new Dictionary<string, EntityProperty>();
            e["Name"] = new EntityProperty(entity.Name);
            e["DepartmentId"] = new EntityProperty(entity.DepartmentId);
            e["OnboardDate"] = new EntityProperty(entity.OnboardDate);
            e["Id"] = new EntityProperty(entity.Id);
            return e;
        }
    }

}
