using System.Text.Json;
using System.Text.Json.Nodes;
using Kalkimo.Api.Infrastructure;
using Kalkimo.Api.Models;
using Kalkimo.Domain.Models;

namespace Kalkimo.Api.Services;

/// <summary>
/// Applies JSON Patch operations (RFC 6902) to Project objects
/// </summary>
public class JsonPatchApplier
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonPatchApplier()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Applies a list of patch operations to a project
    /// </summary>
    /// <returns>The patched project and list of old values for undo</returns>
    public (Project PatchedProject, IReadOnlyList<JsonPatchOperation> OperationsWithOldValues) ApplyPatch(
        Project project,
        IReadOnlyList<PatchOperation> operations)
    {
        // Serialize project to JsonNode for manipulation
        var jsonString = JsonSerializer.Serialize(project, _jsonOptions);
        var jsonNode = JsonNode.Parse(jsonString)
            ?? throw new JsonException("Failed to parse project JSON");

        var operationsWithOld = new List<JsonPatchOperation>();

        foreach (var op in operations)
        {
            var oldValue = ApplyOperation(jsonNode, op);
            operationsWithOld.Add(new JsonPatchOperation
            {
                Op = op.Op,
                Path = op.Path,
                Value = op.Value != null ? JsonSerializer.SerializeToElement(op.Value, _jsonOptions) : null,
                From = op.From,
                OldValue = oldValue
            });
        }

        // Deserialize back to Project
        var patchedProject = JsonSerializer.Deserialize<Project>(jsonNode.ToJsonString(), _jsonOptions)
            ?? throw new JsonException("Failed to deserialize patched project");

        return (patchedProject, operationsWithOld);
    }

    /// <summary>
    /// Applies a single operation and returns the old value
    /// </summary>
    private JsonElement? ApplyOperation(JsonNode root, PatchOperation op)
    {
        var normalizedOp = op.Op.ToLowerInvariant();

        return normalizedOp switch
        {
            "add" => ApplyAdd(root, op.Path, op.Value),
            "remove" => ApplyRemove(root, op.Path),
            "replace" => ApplyReplace(root, op.Path, op.Value),
            "move" => ApplyMove(root, op.From!, op.Path),
            "copy" => ApplyCopy(root, op.From!, op.Path),
            "test" => ApplyTest(root, op.Path, op.Value),
            _ => throw new JsonPatchException($"Unknown operation: {op.Op}")
        };
    }

    private JsonElement? ApplyAdd(JsonNode root, string path, object? value)
    {
        var (parent, key) = NavigateToParent(root, path);

        if (parent is JsonArray array)
        {
            var index = key == "-" ? array.Count : int.Parse(key);
            if (index < 0 || index > array.Count)
                throw new JsonPatchException($"Array index out of bounds: {index}");

            array.Insert(index, ConvertToJsonNode(value));
            return null; // Add has no old value
        }
        else if (parent is JsonObject obj)
        {
            JsonElement? oldValue = null;
            if (obj.ContainsKey(key))
            {
                oldValue = JsonSerializer.SerializeToElement(obj[key], _jsonOptions);
            }
            obj[key] = ConvertToJsonNode(value);
            return oldValue;
        }

        throw new JsonPatchException($"Cannot add to path: {path}");
    }

    private JsonElement? ApplyRemove(JsonNode root, string path)
    {
        var (parent, key) = NavigateToParent(root, path);
        JsonElement? oldValue;

        if (parent is JsonArray array)
        {
            var index = int.Parse(key);
            if (index < 0 || index >= array.Count)
                throw new JsonPatchException($"Array index out of bounds: {index}");

            oldValue = JsonSerializer.SerializeToElement(array[index], _jsonOptions);
            array.RemoveAt(index);
        }
        else if (parent is JsonObject obj)
        {
            if (!obj.ContainsKey(key))
                throw new JsonPatchException($"Path does not exist: {path}");

            oldValue = JsonSerializer.SerializeToElement(obj[key], _jsonOptions);
            obj.Remove(key);
        }
        else
        {
            throw new JsonPatchException($"Cannot remove from path: {path}");
        }

        return oldValue;
    }

    private JsonElement? ApplyReplace(JsonNode root, string path, object? value)
    {
        var (parent, key) = NavigateToParent(root, path);
        JsonElement? oldValue;

        if (parent is JsonArray array)
        {
            var index = int.Parse(key);
            if (index < 0 || index >= array.Count)
                throw new JsonPatchException($"Array index out of bounds: {index}");

            oldValue = JsonSerializer.SerializeToElement(array[index], _jsonOptions);
            array[index] = ConvertToJsonNode(value);
        }
        else if (parent is JsonObject obj)
        {
            if (!obj.ContainsKey(key))
                throw new JsonPatchException($"Path does not exist: {path}");

            oldValue = JsonSerializer.SerializeToElement(obj[key], _jsonOptions);
            obj[key] = ConvertToJsonNode(value);
        }
        else
        {
            throw new JsonPatchException($"Cannot replace at path: {path}");
        }

        return oldValue;
    }

    private JsonElement? ApplyMove(JsonNode root, string from, string to)
    {
        var oldValue = ApplyRemove(root, from);

        // Get the value that was removed
        var value = oldValue.HasValue
            ? JsonSerializer.Deserialize<object>(oldValue.Value.GetRawText(), _jsonOptions)
            : null;

        ApplyAdd(root, to, value);
        return oldValue;
    }

    private JsonElement? ApplyCopy(JsonNode root, string from, string to)
    {
        var sourceValue = GetValueAtPath(root, from);
        ApplyAdd(root, to, sourceValue);
        return null; // Copy doesn't modify source
    }

    private JsonElement? ApplyTest(JsonNode root, string path, object? expectedValue)
    {
        var actualValue = GetValueAtPath(root, path);
        var actualJson = JsonSerializer.Serialize(actualValue, _jsonOptions);
        var expectedJson = JsonSerializer.Serialize(expectedValue, _jsonOptions);

        if (actualJson != expectedJson)
        {
            throw new JsonPatchException($"Test failed at path {path}: expected {expectedJson}, got {actualJson}");
        }

        return null;
    }

    private object? GetValueAtPath(JsonNode root, string path)
    {
        var segments = ParsePath(path);
        JsonNode? current = root;

        foreach (var segment in segments)
        {
            if (current is JsonArray array)
            {
                var index = int.Parse(segment);
                if (index < 0 || index >= array.Count)
                    throw new JsonPatchException($"Array index out of bounds: {index}");
                current = array[index];
            }
            else if (current is JsonObject obj)
            {
                if (!obj.ContainsKey(segment))
                    throw new JsonPatchException($"Path does not exist: {path}");
                current = obj[segment];
            }
            else
            {
                throw new JsonPatchException($"Cannot navigate path: {path}");
            }
        }

        return current != null
            ? JsonSerializer.Deserialize<object>(current.ToJsonString(), _jsonOptions)
            : null;
    }

    private (JsonNode Parent, string Key) NavigateToParent(JsonNode root, string path)
    {
        var segments = ParsePath(path);

        if (segments.Length == 0)
            throw new JsonPatchException("Empty path");

        JsonNode current = root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];

            if (current is JsonArray array)
            {
                var index = int.Parse(segment);
                if (index < 0 || index >= array.Count)
                    throw new JsonPatchException($"Array index out of bounds: {index}");
                current = array[index] ?? throw new JsonPatchException($"Null value at index {index}");
            }
            else if (current is JsonObject obj)
            {
                // Try case-insensitive match
                var matchingKey = FindMatchingKey(obj, segment);
                if (matchingKey == null)
                    throw new JsonPatchException($"Path segment not found: {segment}");
                current = obj[matchingKey] ?? throw new JsonPatchException($"Null value at path segment: {segment}");
            }
            else
            {
                throw new JsonPatchException($"Cannot navigate path at segment: {segment}");
            }
        }

        var lastSegment = segments[^1];
        if (current is JsonObject parentObj)
        {
            // For objects, find the actual key (case insensitive for convenience)
            var actualKey = FindMatchingKey(parentObj, lastSegment) ?? lastSegment;
            return (current, actualKey);
        }

        return (current, lastSegment);
    }

    private static string? FindMatchingKey(JsonObject obj, string segment)
    {
        // Exact match first
        if (obj.ContainsKey(segment))
            return segment;

        // Case-insensitive match
        foreach (var prop in obj)
        {
            if (string.Equals(prop.Key, segment, StringComparison.OrdinalIgnoreCase))
                return prop.Key;
        }

        return null;
    }

    private static string[] ParsePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return [];

        // JSON Pointer format: /segment/segment/...
        if (!path.StartsWith('/'))
            throw new JsonPatchException($"Path must start with '/': {path}");

        var segments = path[1..].Split('/');

        // Unescape JSON Pointer encoding
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = segments[i]
                .Replace("~1", "/")
                .Replace("~0", "~");
        }

        return segments;
    }

    private JsonNode? ConvertToJsonNode(object? value)
    {
        if (value == null)
            return null;

        if (value is JsonElement element)
        {
            return JsonNode.Parse(element.GetRawText());
        }

        var json = JsonSerializer.Serialize(value, _jsonOptions);
        return JsonNode.Parse(json);
    }
}

/// <summary>
/// Exception thrown when a JSON Patch operation fails
/// </summary>
public class JsonPatchException : Exception
{
    public JsonPatchException(string message) : base(message) { }
}
