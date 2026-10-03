using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record AlertResponse(
    int Id,
    AlertEventType EventType,
    string ReferenceTable,
    int ReferenceId,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);

public record AlertCountResponse(int Count);