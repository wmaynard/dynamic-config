using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;

namespace Rumble.Config;

public class SectionMissingException : PlatformException
{
    public string Name { get; set; }
    public SectionMissingException(string name) : base("Section does not exist.", code: ErrorCode.MongoRecordNotFound)
    {
        Name = name;
    }
}