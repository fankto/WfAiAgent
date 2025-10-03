#!/bin/bash

# Test Query Formulation
# This script tests the new query formulation logic

echo "=== Testing Query Formulation ==="
echo ""
echo "This test demonstrates how user queries are transformed into clean search queries."
echo ""

# Create a simple C# test program
cat > /tmp/test_query_formulation.cs << 'EOF'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// Simplified version of the query formulation logic for testing
class QueryFormulationTest
{
    private static readonly string[] NoiseTerms = new[]
    {
        "workflow+", "workflowplus", "workflow plus",
        "script", "code", "function", "command", "program",
        "in", "using", "with", "for", "the", "a", "an",
        "how to", "how do i", "i want to", "i need to", "can you",
        "help me", "please", "erstell mir", "wie kann ich"
    };
    
    private static readonly string[] CoreActionVerbs = new[]
    {
        "create", "new", "make", "initialize",
        "add", "insert", "append", "push",
        "delete", "remove", "clear", "drop",
        "update", "modify", "change", "set",
        "get", "fetch", "retrieve", "read",
        "sort", "order", "arrange",
        "search", "find", "query", "filter",
        "send", "mail", "email", "notify"
    };
    
    private static readonly string[] CoreNouns = new[]
    {
        "array", "list", "collection",
        "database", "table", "record", "row",
        "file", "document", "folder", "directory",
        "string", "text", "number", "date",
        "email", "mail", "message"
    };
    
    static string FormulateQuery(string userInput)
    {
        var normalized = userInput.ToLowerInvariant();
        
        // Remove noise terms
        foreach (var noiseTerm in NoiseTerms)
        {
            var pattern = $@"\b{Regex.Escape(noiseTerm)}\b";
            normalized = Regex.Replace(normalized, pattern, " ", RegexOptions.IgnoreCase);
        }
        
        // Tokenize
        var words = Regex.Split(normalized, @"\s+")
            .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 1)
            .ToList();
        
        // Extract concepts
        var extractedVerbs = new List<string>();
        var extractedNouns = new List<string>();
        
        foreach (var word in words)
        {
            var matchedVerb = CoreActionVerbs.FirstOrDefault(v => word.Contains(v) || v.Contains(word));
            if (matchedVerb != null && !extractedVerbs.Contains(matchedVerb))
            {
                extractedVerbs.Add(matchedVerb);
            }
            
            var matchedNoun = CoreNouns.FirstOrDefault(n => word.Contains(n) || n.Contains(word));
            if (matchedNoun != null && !extractedNouns.Contains(matchedNoun))
            {
                extractedNouns.Add(matchedNoun);
            }
        }
        
        // Build query
        var queryParts = new List<string>();
        if (extractedVerbs.Any()) queryParts.Add(extractedVerbs.First());
        queryParts.AddRange(extractedNouns.Take(2));
        
        // Fallback
        if (!queryParts.Any())
        {
            var stopWords = new[] { "that", "this", "what", "when", "where", "which", "who", "why", "how" };
            queryParts = words.Where(w => w.Length > 3 && !stopWords.Contains(w)).Take(3).ToList();
        }
        
        var result = string.Join(" ", queryParts).Trim();
        return string.IsNullOrWhiteSpace(result) ? string.Join(" ", words.Take(3)) : result;
    }
    
    static void Main()
    {
        var testCases = new[]
        {
            "erstell mir ein array mit verschiedenen gemüse",
            "create an array in Workflow+ script",
            "how do i sort a list in Workflow+?",
            "wie kann ich daten zur datenbank hinzufügen?",
            "i want to send an email using the SendMail command",
            "delete a file from the folder",
            "update database table with new records",
            "search for text in a document"
        };
        
        Console.WriteLine("User Query → Formulated Search Query");
        Console.WriteLine(new string('=', 80));
        
        foreach (var testCase in testCases)
        {
            var formulated = FormulateQuery(testCase);
            Console.WriteLine($"{testCase}");
            Console.WriteLine($"  → {formulated}");
            Console.WriteLine();
        }
    }
}
EOF

# Compile and run
echo "Compiling test..."
csc /tmp/test_query_formulation.cs -out:/tmp/test_query_formulation.exe 2>/dev/null

if [ $? -eq 0 ]; then
    echo "Running tests..."
    echo ""
    mono /tmp/test_query_formulation.exe
else
    echo "❌ Compilation failed. Trying with dotnet..."
    echo ""
    
    # Try with dotnet script instead
    cat > /tmp/test_query.csx << 'EOFCSX'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

var noiseTerms = new[]
{
    "workflow+", "workflowplus", "script", "code", "function", "command",
    "in", "using", "with", "for", "the", "a", "an",
    "how to", "i want to", "please", "erstell mir", "wie kann ich"
};

var coreVerbs = new[] { "create", "add", "delete", "update", "get", "sort", "search", "send" };
var coreNouns = new[] { "array", "list", "database", "table", "file", "email", "text" };

string FormulateQuery(string input)
{
    var normalized = input.ToLowerInvariant();
    foreach (var term in noiseTerms)
        normalized = Regex.Replace(normalized, $@"\b{Regex.Escape(term)}\b", " ", RegexOptions.IgnoreCase);
    
    var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var verbs = words.Where(w => coreVerbs.Any(v => w.Contains(v))).ToList();
    var nouns = words.Where(w => coreNouns.Any(n => w.Contains(n))).ToList();
    
    var parts = new List<string>();
    if (verbs.Any()) parts.Add(verbs.First());
    parts.AddRange(nouns.Take(2));
    
    return parts.Any() ? string.Join(" ", parts) : string.Join(" ", words.Take(2));
}

var tests = new[]
{
    "erstell mir ein array mit verschiedenen gemüse",
    "create an array in Workflow+ script",
    "how do i sort a list?",
    "wie kann ich daten zur datenbank hinzufügen?",
    "send an email using SendMail"
};

Console.WriteLine("User Query → Formulated Search Query");
Console.WriteLine(new string('=', 80));

foreach (var test in tests)
{
    Console.WriteLine($"{test}");
    Console.WriteLine($"  → {FormulateQuery(test)}");
    Console.WriteLine();
}
EOFCSX
    
    dotnet script /tmp/test_query.csx
fi

echo ""
echo "✅ Test complete!"
echo ""
echo "Notice how all queries are reduced to minimal technical concepts:"
echo "  - No product names (Workflow+)"
echo "  - No generic terms (script, code)"
echo "  - No filler words (how to, i want to)"
echo "  - Just: [action] [target]"
