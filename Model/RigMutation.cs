using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model;

public sealed class RigMutationErrorEnvelope
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<RigMutationError> Errors { get; set; } = [];
}

public sealed class RigMutationError
{
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
