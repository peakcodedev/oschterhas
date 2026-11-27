using Oschterhas.Core.Admin;

namespace Oschterhas.Core.Domains;

public sealed record SetCustomDomainRequest(string Domain);

/// <param name="Outcome">Result of the check; the instance contains the new status.</param>
public sealed record CustomDomainCheckDto(InstanceDetailDto Instance, DomainCheckOutcome Outcome);
