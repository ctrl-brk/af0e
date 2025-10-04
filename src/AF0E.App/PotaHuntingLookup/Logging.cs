﻿using System.Net;
using Microsoft.Extensions.Logging;

namespace PotaHuntingLookup;

internal static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Lookup started for {startDate} - {endDate}")]
    public static partial void LogAppStarted(this ILogger logger, string startDate, string endDate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{callSign} {parkNum} {parkName}")]
    public static partial void LogQso(this ILogger logger, string callSign, string parkNum, string parkName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Adding {parkNum} - {parkName}")]
    public static partial void LogNewPark(this ILogger logger, string parkNum, string parkName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {count} QSOs")]
    public static partial void LogProcessing(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Removing {match} from {comment} for {call} ({id})")]
    public static partial void LogStripedComment(this ILogger logger, string match, string comment, string call, int id);

    [LoggerMessage(Level = LogLevel.Information, Message = "No new QSOs detected")]
    public static partial void LogNoNewQsos(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No QSOs were returned by the pota api")]
    public static partial void LogNoQsos(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Park {parkNum} not found")]
    public static partial void LogParkNotFound(this ILogger logger, string parkNum);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Calls {calls} not found")]
    public static partial void LogCallsNotFound(this ILogger logger, string calls);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Calls {calls} have band mismatch")]
    public static partial void LogCallsBandMismatch(this ILogger logger, string calls);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid HTTP status code: {statusCode}")]
    public static partial void LogHttpError(this ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Can not deserialize response: {url}")]
    public static partial void LogJsonError(this ILogger logger, string url);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error {statusCode}. {errorMessage}, {requestMessage}")]
    public static partial void LogAppError(this ILogger logger, HttpStatusCode statusCode, string? errorMessage, HttpRequestMessage? requestMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogException(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "{message}")]
    public static partial void LogInformation(this ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{message}")]
    public static partial void LogWarning(this ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "{message}")]
    public static partial void LogError(this ILogger logger, string message);
}
