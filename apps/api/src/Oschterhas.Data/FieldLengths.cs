namespace Oschterhas.Data;

/// <summary>Maximum text lengths, shared by the database schema and input validation.</summary>
public static class FieldLengths
{
    public const int Slug = 40;
    public const int InstanceName = 100;
    public const int InstanceDescription = 2000;
    public const int HostName = 253;
    public const int Token = 64;
    public const int HareName = 100;
    public const int HareDescription = 500;
    public const int SourceWaypointId = 100;
    public const int Nickname = 40;
    public const int Email = 254;
    public const int DisplayName = 200;
    public const int IdentityIssuer = 500;
    public const int IdentitySubject = 255;
    public const int Locale = 10;
    public const int EnumValue = 20;
}
