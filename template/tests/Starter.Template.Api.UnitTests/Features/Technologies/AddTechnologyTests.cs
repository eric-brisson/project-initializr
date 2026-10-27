using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class AddTechnologyTests
{
    [Fact]
    public void Handle_NewTechnology_ReturnsCreated()
    {
        var catalog = new TechnologyCatalog();

        var result = AddTechnology.Handle(new("New Tech", "1.0.0"), catalog);

        var created = Assert.IsType<CreatedAtRoute<ListTechnologies.TechnologyDto>>(result.Result);
        Assert.Equal(new ListTechnologies.TechnologyDto("New Tech", "1.0.0"), created.Value);
        Assert.Contains(created.Value, catalog.GetAll());
    }

    [Fact]
    public void Handle_ExistingTechnology_ReturnsConflict()
    {
        var catalog = new TechnologyCatalog();

        var result = AddTechnology.Handle(new(".net", "1.0.0"), catalog);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }
}
