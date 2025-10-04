# Testing Search Improvements

## Quick Verification

### 1. Check Compilation

```bash
cd AiAgent
dotnet build
```

Expected: No errors

### 2. Run Integration Tests (if available)

```bash
dotnet test --filter "SearchKnowledge"
```

### 3. Manual Testing with Agent

```bash
# Ensure AiSearch service is running
cd AiSearch/src/Service
dotnet run &

# Run the agent
cd ../../AiAgent/src/Agent
export OPENAI_API_KEY='your-key'
dotnet run
```

### Test Queries

**Query 1: Simple (should skip assessment)**
```
User: "create array"
Expected: Fast response, ≤3 results, no assessment log
```

**Query 2: Complex (should trigger assessment)**
```
User: "How do I sort a list of vegetables alphabetically?"
Expected: 
- Log: "Assessing relevance of X candidates using LLM"
- Log: "LLM selected Y of X candidates as relevant"
- Returns only relevant commands (ArraySort, ArrayNew)
```

**Query 3: Multilingual (tests expansion)**
```
User: "neue liste erstellen und sortieren"
Expected: Finds English commands via query expansion
```

## Observing Parallel Execution

Look for these log patterns:

**Before (sequential):**
```
[Info] Searching documentation for: sort list
[Info] Generating hypothetical document for: sort list
[Info] HyDE generated: Use ArraySort command...
[Info] Expanding query: sort list
[Info] Expanded to 3 variations
[Info] Advanced search returned 5 unique results (from 12 total hits)
```

**After (parallel):**
```
[Info] Searching documentation for: sort list
[Debug] Technique 'original' returned 5 results
[Debug] Technique 'hyde' returned 5 results
[Debug] Technique 'expansion' returned 8 results
[Info] Advanced search returned 8 unique candidates (from 18 total hits)
[Info] Assessing relevance of 8 candidates using LLM
[Info] LLM selected 2 of 8 candidates as relevant. Reasoning: ArraySort...
```

## Performance Comparison

### Measure Latency

Add timing to your test:

```csharp
var stopwatch = Stopwatch.StartNew();
var response = await orchestrator.ProcessQueryAsync("How do I sort a list?");
stopwatch.Stop();
Console.WriteLine($"Search took: {stopwatch.ElapsedMilliseconds}ms");
```

**Expected improvements:**
- Old: 3000-4000ms
- New: 2000-3000ms (with assessment)
- New: 1500-2000ms (without assessment, <4 candidates)

## Verifying LLM Assessment Quality

### Test Case 1: Filtering Irrelevant Results

**Query:** "sort array"

**Expected candidates (before assessment):**
1. ArraySort (relevant ✓)
2. ArrayNew (relevant ✓)
3. ArrayFilter (not relevant ✗)
4. ArrayReverse (not relevant ✗)
5. StringSort (not relevant ✗)

**Expected after assessment:**
- Only ArraySort and ArrayNew returned
- Log shows reasoning

### Test Case 2: Keeping All When Relevant

**Query:** "array operations"

**Expected candidates:**
1. ArrayNew (relevant ✓)
2. ArrayAdd (relevant ✓)
3. ArraySort (relevant ✓)
4. ArrayFilter (relevant ✓)

**Expected after assessment:**
- All 4 returned (all are relevant)
- Log shows reasoning

## Debugging Tips

### Enable Verbose Logging

In `appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

### Check for Fallback Scenarios

Look for these warning logs:
- "LLM assessment returned no results, falling back to score-based ranking"
- "Failed to parse LLM assessment response"
- "Advanced search failed, falling back to basic search"

These indicate graceful degradation is working.

### Verify Parallel Execution

The techniques should complete in overlapping time windows. Check timestamps:

```
[14:23:45.123] Searching documentation for: sort list
[14:23:45.124] Technique 'original' started
[14:23:45.125] Technique 'hyde' started
[14:23:45.126] Technique 'expansion' started
[14:23:46.500] Technique 'original' returned 5 results
[14:23:46.800] Technique 'hyde' returned 5 results
[14:23:47.200] Technique 'expansion' returned 8 results
```

If timestamps are sequential (not overlapping), parallelization isn't working.

## Common Issues

### Issue 1: "Object reference not set" in AssessRelevanceAsync

**Cause:** `_chatService` is null

**Fix:** Ensure AgentOrchestrator passes chat service:
```csharp
new SearchKnowledgeTool(queryEnhancer, _chatService)
```

### Issue 2: Assessment always skipped

**Cause:** Not enough candidates (≤3)

**Solution:** This is expected behavior. Try more complex queries.

### Issue 3: Slow performance

**Cause:** Parallel execution not working

**Check:** 
1. Verify `Task.WhenAll` is used
2. Check for `await` inside loops (should be avoided)
3. Ensure no synchronous blocking calls

### Issue 4: LLM returns invalid JSON

**Cause:** LLM response format varies

**Solution:** Already handled with JSON extraction logic:
```csharp
var jsonStart = content.IndexOf('{');
var jsonEnd = content.LastIndexOf('}');
```

Falls back gracefully if parsing fails.

## Success Criteria

✅ **Compilation:** No errors or warnings

✅ **Parallel execution:** Techniques complete in overlapping time windows

✅ **LLM assessment:** Triggered for >3 candidates, logs reasoning

✅ **Quality:** Returns fewer but more relevant results

✅ **Performance:** 30-50% faster than sequential version

✅ **Fallback:** Gracefully handles failures at each level

✅ **Backward compatibility:** Works with null services (basic search)

## Next Steps

After verification:

1. **Run full test suite:** `dotnet test`
2. **Update documentation:** Add to main README
3. **Monitor in production:** Track latency and quality metrics
4. **Gather feedback:** Ask users if results are more relevant
5. **Tune parameters:** Adjust weights (1.0, 0.8, 0.7) if needed

## Rollback Plan

If issues arise, revert by:

```bash
git checkout HEAD~1 -- AiAgent/src/Agent/Tools/SearchKnowledgeTool.cs
git checkout HEAD~1 -- AiAgent/src/Agent/Orchestration/AgentOrchestrator.cs
```

Or disable features:
```csharp
// Disable LLM assessment
new SearchKnowledgeTool(queryEnhancer, null)

// Disable all advanced features
new SearchKnowledgeTool(null, null)
```
