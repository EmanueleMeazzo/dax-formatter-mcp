# DAX Formatter MCP Server

A Model Context Protocol (MCP) server that provides DAX (Data Analysis Expressions) formatting capabilities to AI assistants and other MCP clients. This server integrates with the official [DAX Formatter](https://www.daxformatter.com/) service by SQLBI to deliver professional-grade DAX code formatting.

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![MCP](https://img.shields.io/badge/MCP-2024--11--05-purple.svg)](https://modelcontextprotocol.io/)

## 🚀 Features

- **Single DAX Expression Formatting**: Format individual DAX expressions with professional standards
- **Batch Processing**: Format multiple DAX expressions in a single efficient request
- **Comprehensive Formatting Options**: Control line length, spacing, separators, and more
- **Error Handling**: Robust error handling with detailed error messages
- **Fallback Mechanisms**: Automatic fallback to individual formatting if batch processing fails
- **Database Context**: Support for database and server context (anonymized for privacy)

## 📋 Prerequisites

- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
- Internet connection (communicates with daxformatter.com)
- MCP-compatible client (Claude Desktop, Continue, or custom MCP client)

## 🛠️ Installation

### Option 1: Download Release (Recommended)

1. Download the latest release from the [Releases](https://github.com/EmanueleMeazzo/dax-formatter-mcp/releases) page
2. Extract the archive to your preferred location
3. Note the path to the executable for configuration

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/EmanueleMeazzo/dax-formatter-mcp.git
cd dax-formatter-mcp

# Build the project
dotnet build --configuration Release

# The executable will be in bin/Release/net8.0/
```

## ⚙️ Configuration

### Claude Desktop

Add the following to your Claude Desktop configuration file:

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`  
**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`  
**Linux**: `~/.config/claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "dax-formatter": {
      "command": "path/to/dax-formatter-mcp.exe",
      "args": []
    }
  }
}
```

### Continue IDE Extension

Add to your Continue configuration:

```json
{
  "mcpServers": [
    {
      "name": "dax-formatter",
      "command": "path/to/dax-formatter-mcp.exe",
      "args": []
    }
  ]
}
```

### Generic MCP Client

For any MCP client supporting the MCP 2024-11-05 protocol:

```json
{
  "servers": [
    {
      "name": "dax-formatter",
      "transport": {
        "type": "stdio",
        "command": "path/to/dax-formatter-mcp.exe",
        "args": []
      }
    }
  ]
}
```

### Using with .NET Runtime

If you prefer to use the DLL with the .NET runtime:

```json
{
  "mcpServers": {
    "dax-formatter": {
      "command": "dotnet",
      "args": ["path/to/dax-formatter-mcp.dll"]
    }
  }
}
```

## 📖 Usage

Once configured, you can use the DAX formatter through your MCP client:

### Basic Usage

```
Format this DAX expression:
CALCULATE(SUM(Sales[Amount]),FILTER(Products,Products[Category]="Electronics"))
```

### Multiple Expressions

```
Format these DAX measures:
1. [Total Sales] := SUM(Sales[Amount])
2. [Electronics Sales] := CALCULATE(SUM(Sales[Amount]),Products[Category]="Electronics")
3. [Sales YTD] := TOTALYTD(SUM(Sales[Amount]),'Calendar'[Date])
```

### With Formatting Options

```
Format this DAX with short lines and specific separators:
CALCULATE(SUM(Sales[Amount]),FILTER(Products,Products[Category]="Electronics"))

Options:
- Line length: ShortLine
- List separator: ;
- Decimal separator: ,
```

## 🔧 Available Tools

### `format_dax`

Formats a single DAX expression.

**Parameters:**
- `dax` (required): The DAX expression to format
- `options` (optional): Formatting options object

**Example:**
```json
{
  "dax": "CALCULATE(SUM(Sales[Amount]),FILTER(Products,Products[Category]=\"Electronics\"))",
  "options": {
    "maxLineLength": "LongLine",
    "skipSpaceAfterFunctionName": "BestPractice"
  }
}
```

### `format_dax_multiple`

Formats multiple DAX expressions in a single request.

**Parameters:**
- `expressions` (required): Array of DAX expressions to format
- `options` (optional): Formatting options object

**Example:**
```json
{
  "expressions": [
    "CALCULATE(SUM(Sales[Amount]),FILTER(Products,Products[Category]=\"Electronics\"))",
    "[Total Sales] := SUM(Sales[Amount])"
  ],
  "options": {
    "maxLineLength": "LongLine",
    "databaseName": "SalesDB",
    "serverName": "ProductionServer"
  }
}
```

## 🎛️ Formatting Options

All formatting options are optional and have sensible defaults:

| Option | Values | Default | Description |
|--------|--------|---------|-------------|
| `maxLineLength` | `ShortLine`, `LongLine`, `VeryLongLine` | `LongLine` | Maximum line length for formatted code |
| `skipSpaceAfterFunctionName` | `BestPractice`, `False` | `BestPractice` | Spacing after function names |
| `listSeparator` | Any character | `,` | Character used as list separator |
| `decimalSeparator` | Any character | `.` | Character used as decimal separator |
| `databaseName` | String | - | Database name for context (anonymized) |
| `serverName` | String | - | Server name for context (anonymized) |

## 📝 Example DAX Expressions

Here are some example DAX expressions you can test with:

### Basic Measures
```dax
[Total Sales] := SUM(Sales[Amount])
```

### Complex Calculations
```dax
CALCULATE(SUM(Sales[Amount]),FILTER(ALL(Products),Products[Category]="Electronics"),USERELATIONSHIP(Sales[OrderDate],'Calendar'[Date]))
```

### Time Intelligence
```dax
[Sales YTD] := TOTALYTD(SUM(Sales[Amount]),'Calendar'[Date])
```

### Table Expressions
```dax
EVALUATE FILTER(Customer, Customer[Country] = "USA")
```

## 🔍 Troubleshooting

### Common Issues

**Server fails to start:**
- Ensure .NET 8.0 runtime is installed
- Check that the executable path is correct in your configuration
- Verify you have internet connectivity (required for DAX Formatter service)

**Formatting requests fail:**
- Check your internet connection
- Verify the DAX syntax is valid
- Try with simpler expressions first

**MCP client doesn't recognize the server:**
- Restart your MCP client after adding the configuration
- Check the JSON configuration syntax
- Verify the executable path is absolute and correct

### Debug Mode

To enable debug logging, you can run the server manually to see detailed logs:

```bash
# Run the server directly to see debug output
./dax-formatter-mcp.exe
```

Then send JSON-RPC messages manually to test functionality.

## 🏗️ Architecture

The MCP server is built with:

- **.NET 9.0**: Modern, cross-platform runtime
- **DAX Formatter Client**: Official NuGet package from SQLBI
- **JSON-RPC 2.0**: Standard protocol for MCP communication
- **Async/Await**: Non-blocking I/O for better performance

### Flow Diagram

```
MCP Client → JSON-RPC → DAX Formatter MCP Server → DAX Formatter API → Formatted DAX
```

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests if applicable
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Development Setup

```bash
# Clone the repository
git clone https://github.com/EmanueleMeazzo/dax-formatter-mcp.git
cd dax-formatter-mcp

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test
```

## 🔒 Privacy & Security

- All DAX expressions are sent to the official DAX Formatter service at daxformatter.com
- Database and server names are anonymized by the DAX Formatter client
- No DAX expressions are stored locally
- No personal information is collected or transmitted

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [SQLBI](https://www.sqlbi.com/) for the excellent DAX Formatter service
- [Anthropic](https://www.anthropic.com/) for the Model Context Protocol specification
- [Microsoft](https://docs.microsoft.com/en-us/dax/) for the DAX language

## 📚 Related Resources

- [DAX Formatter Website](https://www.daxformatter.com/)
- [DAX Code Formatting Rules](https://www.sqlbi.com/articles/rules-for-dax-code-formatting/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [DAX Language Reference](https://docs.microsoft.com/en-us/dax/)
