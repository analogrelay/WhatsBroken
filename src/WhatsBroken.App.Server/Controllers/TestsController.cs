using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WhatsBroken.App.Server.Controllers
{
    [ApiController]
    [Route("api/tests")]
    public class TestsController: Controller
    {
        [HttpGet("values")]
        public async IAsyncEnumerable<string> GetValuesAsync()
        {
            yield return "foo";
            await Task.Yield();
            yield return "bar";
            await Task.Yield();
            yield return "baz";
        }
    }
}
