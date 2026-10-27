namespace DataConverter.Core;

/// <summary>A file could not be converted because of its content; the message says what to fix.</summary>
public sealed class DataFormatException(string message) : Exception(message);
