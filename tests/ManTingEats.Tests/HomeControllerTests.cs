using ManTingEats.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManTingEats.Tests;

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsViewResult()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance);

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}
