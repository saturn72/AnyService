using AnyService.Security;
using AnyService.Services.Security;

namespace AnyService.Tests.Services.Security
{
    public class PermissionFuncsTests
    {
        [Fact]
        public void AllFuncs()
        {
            string post = "post",
            get = "get",
            put = "put",
            delete = "delete";
            var tc = new EntityConfigRecord { PermissionRecord = new PermissionRecord(post, get, put, delete) };

            PermissionFuncs.GetByHttpMethod(post)(tc).ShouldBe(post);
            PermissionFuncs.GetByHttpMethod(get)(tc).ShouldBe(get);
            PermissionFuncs.GetByHttpMethod(put)(tc).ShouldBe(put);
            PermissionFuncs.GetByHttpMethod(delete)(tc).ShouldBe(delete);
        }
        [Fact]
        public void ReturnsNullOnKeyNotExists()
        {
             PermissionFuncs.GetByHttpMethod("not-existst").ShouldBeNull();
        }
    }
}
