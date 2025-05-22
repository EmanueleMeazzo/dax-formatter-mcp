using System.Text.Json;
using System.Text.Json.Serialization;
using Dax.Formatter;
using Dax.Formatter.Models;
using Dax.Formatter.AnalysisServices;

namespace DaxFormatterMcp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var server = new McpServer();
        await server.RunAsync();
    }
}

public class McpServer
{
    private readonly DaxFormatterClient _formatter;

    public McpServer()
    {
        _formatter = new DaxFormatterClient();
    }

    public async Task RunAsync()
    {
        // Read from stdin and write to stdout for MCP protocol
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        
        var reader = new StreamReader(stdin);
        var writer = new StreamWriter(stdout) { AutoFlush = true };

        // Enable debug logging to stderr (won't interfere with MCP communication)
        var debug = Console.Error;

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            try
            {
                await debug.WriteLineAsync($"[DEBUG] Received: {line}");
                
                var request = JsonSerializer.Deserialize<McpRequest>(line);
                var response = await HandleRequestAsync(request);
                
                // Only send response if one was returned (notifications return null)
                if (response != null)
                {
                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    await debug.WriteLineAsync($"[DEBUG] Sending: {responseJson}");
                    await writer.WriteLineAsync(responseJson);
                }
            }
            catch (Exception ex)
            {
                await debug.WriteLineAsync($"[DEBUG] Exception: {ex}");
                
                var errorResponse = new McpResponse
                {
                    Id = null,
                    Error = new McpError
                    {
                        Code = -32603,
                        Message = "Internal error: " + ex.Message
                    }
                };
                
                var errorJson = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                await writer.WriteLineAsync(errorJson);
            }
        }
    }

    private void ApplyFormattingOptions(DaxFormatterSingleRequest request, FormattingOptions options)
    {
        if (!string.IsNullOrEmpty(options.MaxLineLength))
        {
            if (Enum.TryParse<DaxFormatterLineStyle>(options.MaxLineLength, out var lineStyle))
            {
                request.MaxLineLength = lineStyle;
            }
        }

        if (!string.IsNullOrEmpty(options.SkipSpaceAfterFunctionName))
        {
            if (Enum.TryParse<DaxFormatterSpacingStyle>(options.SkipSpaceAfterFunctionName, out var spacingStyle))
            {
                request.SkipSpaceAfterFunctionName = spacingStyle;
            }
        }

        if (!string.IsNullOrEmpty(options.ListSeparator))
        {
            request.ListSeparator = options.ListSeparator[0];
        }

        if (!string.IsNullOrEmpty(options.DecimalSeparator))
        {
            request.DecimalSeparator = options.DecimalSeparator[0];
        }

        if (!string.IsNullOrEmpty(options.DatabaseName))
        {
            request.DatabaseName = options.DatabaseName;
        }

        if (!string.IsNullOrEmpty(options.ServerName))
        {
            request.ServerName = options.ServerName;
        }
    }

    private void ApplyFormattingOptions(DaxFormatterMultipleRequest request, FormattingOptions options)
    {
        if (!string.IsNullOrEmpty(options.MaxLineLength))
        {
            if (Enum.TryParse<DaxFormatterLineStyle>(options.MaxLineLength, out var lineStyle))
            {
                request.MaxLineLength = lineStyle;
            }
        }

        if (!string.IsNullOrEmpty(options.SkipSpaceAfterFunctionName))
        {
            if (Enum.TryParse<DaxFormatterSpacingStyle>(options.SkipSpaceAfterFunctionName, out var spacingStyle))
            {
                request.SkipSpaceAfterFunctionName = spacingStyle;
            }
        }

        if (!string.IsNullOrEmpty(options.ListSeparator))
        {
            request.ListSeparator = options.ListSeparator[0];
        }

        if (!string.IsNullOrEmpty(options.DecimalSeparator))
        {
            request.DecimalSeparator = options.DecimalSeparator[0];
        }

        if (!string.IsNullOrEmpty(options.DatabaseName))
        {
            request.DatabaseName = options.DatabaseName;
        }

        if (!string.IsNullOrEmpty(options.ServerName))
        {
            request.ServerName = options.ServerName;
        }
    }

    private async Task<McpResponse?> HandleRequestAsync(McpRequest? request)
    {
        if (request == null)
        {
            return new McpResponse
            {
                Id = null,
                Error = new McpError
                {
                    Code = -32700,
                    Message = "Parse error: Invalid request"
                }
            };
        }

        // Handle notifications (no response expected)
        if (request.Method.StartsWith("notifications/"))
        {
            // Notifications don't get responses, just log them
            await Console.Error.WriteLineAsync($"[DEBUG] Received notification: {request.Method}");
            return null; // No response for notifications
        }

        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolsCallAsync(request),
            "resources/list" => HandleResourcesList(request),
            "prompts/list" => HandlePromptsList(request),
            _ => new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32601,
                    Message = $"Method not found: {request.Method}"
                }
            }
        };
    }

    private McpResponse HandleInitialize(McpRequest request)
    {
        return new McpResponse
        {
            Id = request.Id,
            Result = new InitializeResult
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability()
                },
                ServerInfo = new ServerInfo
                {
                    Name = "dax-formatter-mcp",
                    Version = "1.0.0"
                }
            }
        };
    }

    private McpResponse HandleToolsList(McpRequest request)
    {
        var tools = new List<Tool>
        {
            new Tool
            {
                Name = "format_dax",
                Description = "Format a single DAX expression according to SQL BI formatting standards",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        dax = new
                        {
                            type = "string",
                            description = "The DAX expression to format"
                        },
                        options = new
                        {
                            type = "object",
                            description = "Optional formatting options",
                            properties = new
                            {
                                maxLineLength = new
                                {
                                    type = "string",
                                    description = "Maximum line length (ShortLine, LongLine, or VeryLongLine)",
                                    @default = "LongLine"
                                },
                                skipSpaceAfterFunctionName = new
                                {
                                    type = "string", 
                                    description = "Spacing after function names (BestPractice or False)",
                                    @default = "BestPractice"
                                },
                                listSeparator = new
                                {
                                    type = "string",
                                    description = "List separator character",
                                    @default = ","
                                },
                                decimalSeparator = new
                                {
                                    type = "string",
                                    description = "Decimal separator character", 
                                    @default = "."
                                },
                                databaseName = new
                                {
                                    type = "string",
                                    description = "Database name for context (will be anonymized)"
                                },
                                serverName = new
                                {
                                    type = "string",
                                    description = "Server name for context (will be anonymized)"
                                }
                            }
                        }
                    },
                    required = new[] { "dax" }
                }
            },
            new Tool
            {
                Name = "format_dax_multiple",
                Description = "Format multiple DAX expressions in a single request",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        expressions = new
                        {
                            type = "array",
                            description = "Array of DAX expressions to format",
                            items = new
                            {
                                type = "string"
                            }
                        },
                        options = new
                        {
                            type = "object",
                            description = "Optional formatting options",
                            properties = new
                            {
                                maxLineLength = new
                                {
                                    type = "string",
                                    description = "Maximum line length (ShortLine, LongLine, or VeryLongLine)",
                                    @default = "LongLine"
                                },
                                skipSpaceAfterFunctionName = new
                                {
                                    type = "string", 
                                    description = "Spacing after function names (BestPractice or False)",
                                    @default = "BestPractice"
                                },
                                listSeparator = new
                                {
                                    type = "string",
                                    description = "List separator character",
                                    @default = ","
                                },
                                decimalSeparator = new
                                {
                                    type = "string",
                                    description = "Decimal separator character", 
                                    @default = "."
                                },
                                databaseName = new
                                {
                                    type = "string",
                                    description = "Database name for context (will be anonymized)"
                                },
                                serverName = new
                                {
                                    type = "string",
                                    description = "Server name for context (will be anonymized)"
                                }
                            }
                        }
                    },
                    required = new[] { "expressions" }
                }
            }
        };

        return new McpResponse
        {
            Id = request.Id,
            Result = new ToolsListResult
            {
                Tools = tools
            }
        };
    }

    private McpResponse HandleResourcesList(McpRequest request)
    {
        // We don't provide any resources, return empty list
        return new McpResponse
        {
            Id = request.Id,
            Result = new
            {
                resources = new object[0]
            }
        };
    }

    private McpResponse HandlePromptsList(McpRequest request)
    {
        // We don't provide any prompts, return empty list
        return new McpResponse
        {
            Id = request.Id,
            Result = new
            {
                prompts = new object[0]
            }
        };
    }

    private async Task<McpResponse> HandleToolsCallAsync(McpRequest request)
    {
        if (request.Params == null)
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32602,
                    Message = "Invalid params"
                }
            };
        }

        var callRequest = JsonSerializer.Deserialize<ToolCallRequest>(
            JsonSerializer.Serialize(request.Params));

        if (callRequest == null)
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32602,
                    Message = "Invalid tool call request"
                }
            };
        }

        return callRequest.Name switch
        {
            "format_dax" => await HandleFormatDaxAsync(request, callRequest),
            "format_dax_multiple" => await HandleFormatDaxMultipleAsync(request, callRequest),
            _ => new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32602,
                    Message = $"Unknown tool: {callRequest.Name}"
                }
            }
        };
    }

    private async Task<McpResponse> HandleFormatDaxAsync(McpRequest request, ToolCallRequest callRequest)
    {
        try
        {
            var arguments = JsonSerializer.Deserialize<FormatDaxArguments>(
                JsonSerializer.Serialize(callRequest.Arguments));

            if (arguments == null || string.IsNullOrEmpty(arguments.Dax))
            {
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32602,
                        Message = "Invalid DAX expression provided"
                    }
                };
            }

            // Try using DaxFormatterSingleRequest with options if provided
            if (arguments.Options != null)
            {
                var singleRequest = new DaxFormatterSingleRequest
                {
                    Dax = arguments.Dax
                };

                ApplyFormattingOptions(singleRequest, arguments.Options);
                var response = await _formatter.FormatAsync(singleRequest);

                return new McpResponse
                {
                    Id = request.Id,
                    Result = new ToolCallResult
                    {
                        Content = new[]
                        {
                            new TextContent
                            {
                                Type = "text",
                                Text = $"Formatted DAX:\n\n```dax\n{response.Formatted}\n```"
                            }
                        }
                    }
                };
            }
            else
            {
                // Use the simple string overload for basic formatting
                var response = await _formatter.FormatAsync(arguments.Dax);

                return new McpResponse
                {
                    Id = request.Id,
                    Result = new ToolCallResult
                    {
                        Content = new[]
                        {
                            new TextContent
                            {
                                Type = "text",
                                Text = $"Formatted DAX:\n\n```dax\n{response.Formatted}\n```"
                            }
                        }
                    }
                };
            }
        }
        catch (Exception ex)
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Error formatting DAX: {ex.Message}"
                }
            };
        }
    }

    private async Task<McpResponse> HandleFormatDaxMultipleAsync(McpRequest request, ToolCallRequest callRequest)
    {
        try
        {
            var arguments = JsonSerializer.Deserialize<FormatDaxMultipleArguments>(
                JsonSerializer.Serialize(callRequest.Arguments));

            if (arguments == null || arguments.Expressions == null || arguments.Expressions.Length == 0)
            {
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32602,
                        Message = "Invalid expressions provided"
                    }
                };
            }

            // Use the proper DaxFormatterMultipleRequest for better performance
            var multipleRequest = new DaxFormatterMultipleRequest();
            
            // Add all expressions to the request
            multipleRequest.Dax.AddRange(arguments.Expressions);

            // Apply any formatting options if provided
            if (arguments.Options != null)
            {
                ApplyFormattingOptions(multipleRequest, arguments.Options);
            }

            // Send the batch request to the DAX Formatter service
            var responses = await _formatter.FormatAsync(multipleRequest);

            // Process the responses
            var formattedExpressions = responses.Select((response, index) => 
                $"**Expression {index + 1}:**\n```dax\n{response.Formatted}\n```").ToArray();

            return new McpResponse
            {
                Id = request.Id,
                Result = new ToolCallResult
                {
                    Content = new[]
                    {
                        new TextContent
                        {
                            Type = "text",
                            Text = $"Formatted {responses.Count} DAX expressions:\n\n" + 
                                   string.Join("\n\n", formattedExpressions)
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            // Fallback to individual formatting if batch request fails
            await Console.Error.WriteLineAsync($"[DEBUG] Batch formatting failed, falling back to individual formatting: {ex.Message}");
            
            try
            {
                var arguments = JsonSerializer.Deserialize<FormatDaxMultipleArguments>(
                    JsonSerializer.Serialize(callRequest.Arguments));

                if (arguments?.Expressions == null) throw new ArgumentException("No expressions provided");

                var formattedExpressions = new List<string>();
                
                for (int i = 0; i < arguments.Expressions.Length; i++)
                {
                    try
                    {
                        var response = await _formatter.FormatAsync(arguments.Expressions[i]);
                        formattedExpressions.Add($"**Expression {i + 1}:**\n```dax\n{response.Formatted}\n```");
                    }
                    catch (Exception individualEx)
                    {
                        formattedExpressions.Add($"**Expression {i + 1} (Error):**\n```\nError: {individualEx.Message}\n```");
                    }
                }

                return new McpResponse
                {
                    Id = request.Id,
                    Result = new ToolCallResult
                    {
                        Content = new[]
                        {
                            new TextContent
                            {
                                Type = "text",
                                Text = $"Formatted {arguments.Expressions.Length} DAX expressions (fallback mode):\n\n" + 
                                       string.Join("\n\n", formattedExpressions)
                            }
                        }
                    }
                };
            }
            catch (Exception fallbackEx)
            {
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32603,
                        Message = $"Error formatting multiple DAX expressions: {fallbackEx.Message}"
                    }
                };
            }
        }
    }
}

// MCP Protocol Models
public class McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

public class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

public class InitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "";

    [JsonPropertyName("capabilities")]
    public ServerCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("serverInfo")]
    public ServerInfo ServerInfo { get; set; } = new();
}

public class ServerCapabilities
{
    [JsonPropertyName("tools")]
    public ToolsCapability? Tools { get; set; }
}

public class ToolsCapability
{
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; set; }
}

public class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}

public class ToolsListResult
{
    [JsonPropertyName("tools")]
    public List<Tool> Tools { get; set; } = new();
}

public class Tool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new();
}

public class ToolCallRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public object Arguments { get; set; } = new();
}

public class ToolCallResult
{
    [JsonPropertyName("content")]
    public TextContent[] Content { get; set; } = Array.Empty<TextContent>();
}

public class TextContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

// Tool-specific models
public class FormatDaxArguments
{
    [JsonPropertyName("dax")]
    public string Dax { get; set; } = "";

    [JsonPropertyName("options")]
    public FormattingOptions? Options { get; set; }
}

public class FormatDaxMultipleArguments
{
    [JsonPropertyName("expressions")]
    public string[] Expressions { get; set; } = Array.Empty<string>();

    [JsonPropertyName("options")]
    public FormattingOptions? Options { get; set; }
}

public class FormattingOptions
{
    [JsonPropertyName("maxLineLength")]
    public string? MaxLineLength { get; set; }

    [JsonPropertyName("skipSpaceAfterFunctionName")]
    public string? SkipSpaceAfterFunctionName { get; set; }

    [JsonPropertyName("listSeparator")]
    public string? ListSeparator { get; set; }

    [JsonPropertyName("decimalSeparator")]
    public string? DecimalSeparator { get; set; }

    [JsonPropertyName("databaseName")]
    public string? DatabaseName { get; set; }

    [JsonPropertyName("serverName")]
    public string? ServerName { get; set; }
}