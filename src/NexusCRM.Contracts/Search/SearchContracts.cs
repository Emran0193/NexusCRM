namespace NexusCRM.Contracts.Search;

public sealed record SearchHitDto(
    string EntityType,
    Guid Id,
    string Title,
    string? Subtitle,
    string Href);

public sealed record GlobalSearchResponse(
    string Query,
    IReadOnlyList<SearchHitDto> Items);
