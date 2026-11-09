using System.Text;

namespace Oschterhas.Common.Text;

public static class NamingConventions
{
    /// <summary>
    /// Converts PascalCase or camelCase to snake_case, e.g. <c>InstanceOrganizer</c> → <c>instance_organizer</c>,
    /// <c>PK_Hares</c> → <c>pk_hares</c>. Consecutive capitals stay together (<c>XmlId</c> → <c>xml_id</c>, <c>IPAddress</c> → <c>ip_address</c>).
    /// </summary>
    public static string ToSnakeCase(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0)
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                var previous = i > 0 ? name[i - 1] : '\0';
                var next = i + 1 < name.Length ? name[i + 1] : '\0';
                var startsWord = i > 0
                    && previous != '_'
                    && (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && char.IsLower(next)));
                if (startsWord)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
