namespace MultipleProjects.Domain;

/// <summary>An amount of money in a given currency.</summary>
public sealed record Money(decimal Amount, string Currency);
