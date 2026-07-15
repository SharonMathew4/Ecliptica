using System;

namespace Ecliptica.Core.Models;

public record ProjectInfo(string Id, string Name, string FilePath, DateTime CreatedAt, DateTime LastModifiedAt);
