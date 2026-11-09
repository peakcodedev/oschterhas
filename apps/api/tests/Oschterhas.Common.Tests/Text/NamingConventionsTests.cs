using Oschterhas.Common.Text;

namespace Oschterhas.Common.Tests.Text;

public class NamingConventionsTests
{
    [Theory]
    [InlineData("Hare", "hare")]
    [InlineData("InstanceOrganizer", "instance_organizer")]
    [InlineData("identitySubject", "identity_subject")]
    [InlineData("CustomDomainLastCheckAt", "custom_domain_last_check_at")]
    [InlineData("PK_Hares", "pk_hares")]
    [InlineData("IX_Hares_InstanceId_Number", "ix_hares_instance_id_number")]
    [InlineData("FK_Hares_Instances_InstanceId", "fk_hares_instances_instance_id")]
    [InlineData("IPAddress", "ip_address")]
    [InlineData("Lat", "lat")]
    [InlineData("xmin", "xmin")]
    [InlineData("Version2Id", "version2_id")]
    [InlineData("", "")]
    public void ToSnakeCase_ConvertsNames(string input, string expected)
    {
        Assert.Equal(expected, NamingConventions.ToSnakeCase(input));
    }
}
