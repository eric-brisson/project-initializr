using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class AddTechnologyTests
{
    [Fact]
    public async Task Handle_NewTechnology_ReturnsCreated()
    {
        var catalog = new FakeTechnologyCatalog();

        var result = await AddTechnology.Handle(new("New Tech", "1.0.0"), catalog, TestContext.Current.CancellationToken);

        var created = Assert.IsType<CreatedAtRoute<ListTechnologies.TechnologyDto>>(result.Result);
        Assert.Equal(new ListTechnologies.TechnologyDto("New Tech", "1.0.0"), created.Value);
        Assert.Contains(created.Value, await catalog.GetAllAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ExistingTechnology_ReturnsConflict()
    {
        var catalog = new FakeTechnologyCatalog(new ListTechnologies.TechnologyDto("Existing", "1.0.0"));

        var result = await AddTechnology.Handle(new("Existing", "2.0.0"), catalog, TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }
}
