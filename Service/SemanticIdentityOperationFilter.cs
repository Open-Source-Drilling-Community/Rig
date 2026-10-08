using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.Drilling.Rig.Semantics;

namespace OSDC.Drilling.Rig.Service;

public sealed class SemanticIdentityOperationFilter:IOperationFilter
{
    public void Apply(OpenApiOperation operation,OperationFilterContext context)
    {
        if(context.MethodInfo.DeclaringType!=typeof(Controllers.RigController) || context.MethodInfo.Name!="GetRigById")return;
        var identity=ProviderSemantics.Metadata(Concepts.ResourceIdentifier);identity["resourceType"]=Concepts.Rig;
        foreach(var parameter in operation.Parameters.Where(p=>p.Name=="id"))
            parameter.Schema.Extensions[SemanticMetadata.ExtensionName]=OpenApiAnyFactory.CreateFromJson(identity.ToJsonString());
    }
}
